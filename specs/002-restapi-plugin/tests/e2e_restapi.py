"""End-to-end test of the Misc.RestApi plugin on the local staging (spec 002, T008/T009).

Run: python3 -I tools/e2e_restapi.py   (from ~/projects/webshops/staging)
Creates throw-away customers stg-api-*@local.test, an order and an API client on the STAGING copy only.
"""
import html, re, sys, time
import requests

sys.path.insert(0, __file__.rsplit("/", 1)[0])
import nopadmin  # noqa: E402

API = nopadmin.BASE + "/api/v1"
results = []


def check(name, ok, detail=""):
    results.append((name, bool(ok)))
    print(("OK   " if ok else "FAIL ") + name + (f"  — {detail}" if detail and not ok else ""))
    return ok


def call(method, path, token=None, **kw):
    headers = kw.pop("headers", {})
    if token:
        headers["Authorization"] = "Bearer " + token
    r = requests.request(method, API + path, headers=headers, timeout=60, **kw)
    try:
        body = r.json()
    except ValueError:
        body = r.text[:300]
    return r.status_code, body


def g(d, *keys):
    """Read a JSON field whatever its casing (camelCase or PascalCase)"""
    for k in keys:
        for cand in (k, k[0].upper() + k[1:], k[0].lower() + k[1:]):
            if isinstance(d, dict) and cand in d:
                return d[cand]
    return None


stamp = time.strftime("%m%d%H%M%S")
pwd = "Stg-Api-" + stamp + "!"

# --- anonymous / guest -------------------------------------------------------
st, _ = call("GET", "/me")
check("sans jeton -> 401", st == 401, st)
st, _ = call("GET", "/me", token="nimportequoi")
check("jeton bidon -> 401", st == 401, st)

st, guest = call("POST", "/auth/guest")
check("session invitée", st == 200 and g(guest, "accessToken"), (st, guest))
gtok = g(guest, "accessToken")

st, store = call("GET", "/store", gtok)
check("GET store", st == 200, (st, store))

st, prods = call("GET", "/products", gtok, params={"pageSize": 50})
items = g(prods, "items") or []
check("GET products (catalogue)", st == 200 and items, (st, str(prods)[:200]))
product = next((p for p in items if (g(g(p, "price"), "value") or 0) > 0 and g(p, "canBePurchased")), None)
if not product:
    sys.exit("aucun produit à prix > 0 dans le catalogue")
pid = g(product, "id")
st, pdet = call("GET", f"/products/{pid}", gtok)
check(f"GET products/{pid}", st == 200, (st, pdet))
print(f"     produit testé : {pid} {g(pdet, 'name')!r} prix API = {g(g(pdet, 'price'), 'formatted')}")

# E5: same price as the web store, for the same anonymous visitor
slug = g(pdet, "seName", "slug")
if slug:
    page = requests.get(f"{nopadmin.BASE}/{slug}", timeout=60).text
    m = re.search(r'class="price-value-\d+"[^>]*>\s*([^<]+?)\s*<', page)
    web = html.unescape(m.group(1)).strip().replace("\u00a0", "\u202f") if m else None
    api = g(g(pdet, "price"), "formatted")
    check("prix API = prix boutique web", web and api and web == api, f"web={web!r} api={api!r}")
else:
    check("prix API = prix boutique web", False, "pas de seName dans la fiche produit")

st, _ = call("POST", "/cart/items", gtok, json={"productId": pid, "quantity": 1})
check("panier invité : ajout", st in (200, 201), (st, _))


# --- two registered customers ------------------------------------------------
def register(tag, token=None):
    email = f"stg-api-{tag}-{stamp}@local.test"
    st, body = call("POST", "/auth/register", token, json={"email": email, "password": pwd, "firstName": "Test", "lastName": tag.upper()})
    check(f"inscription client {tag}", st == 201 and g(body, "status") == "standard", (st, body))
    return email, g(g(body, "token"), "accessToken")


email_a, tok_a = register("a", gtok)  # the guest becomes customer A, cart kept
st, _ = call("GET", "/me", gtok)
check("ancien jeton invité révoqué après inscription", st == 401, st)
st, cart = call("GET", "/cart", tok_a)
check("panier conservé à l'inscription", st == 200 and len(g(cart, "items") or []) == 1, (st, str(cart)[:200]))

st, _ = call("POST", "/auth/login", json={"usernameOrEmail": email_a, "password": "mauvais"})
check("connexion mauvais mot de passe -> 401", st == 401, st)
st, login = call("POST", "/auth/login", json={"usernameOrEmail": email_a, "password": pwd})
check("connexion client A", st == 200, (st, login))
tok_a = g(login, "accessToken") or tok_a

email_b, tok_b = register("b")

# address + checkout for A
st, countries = call("GET", "/countries", tok_a)
ch = next((c for c in (countries if isinstance(countries, list) else g(countries, "items") or []) if g(c, "twoLetterIsoCode") == "CH"), None)
check("GET countries (Suisse présente)", st == 200 and ch, (st, str(countries)[:200]))
st, states = call("GET", f"/countries/{g(ch, 'id')}/states", tok_a)
states_l = states if isinstance(states, list) else g(states, "items") or []
fr = next((x for x in states_l if g(x, "abbreviation") == "FR"), states_l[0] if states_l else None)
addr = {"stateProvinceId": g(fr, "id") if fr else None, "firstName": "Test", "lastName": "A", "email": email_a, "countryId": g(ch, "id") if ch else None,
        "city": "Fribourg", "address1": "Rue du Test 1", "zipPostalCode": "1700", "phoneNumber": "0260000000"}
st, a = call("POST", "/me/addresses", tok_a, json=addr)
check("ajout adresse A", st in (200, 201) and g(a, "id"), (st, a))
addr_id = g(a, "id")

st, opts = call("GET", "/checkout/shipping-options", tok_a, params={"addressId": addr_id})
opts_l = opts if isinstance(opts, list) else g(opts, "items") or []
check("options de livraison", st == 200, (st, str(opts)[:300]))
st, pays = call("GET", "/checkout/payment-methods", tok_a)
pays_l = pays if isinstance(pays, list) else g(pays, "items") or []
check("moyens de paiement", st == 200 and pays_l, (st, str(pays)[:300]))
pay = next((p for p in pays_l if g(p, "supportedByApi")), None)
print(f"     paiements : {[(g(p, 'systemName'), g(p, 'supportedByApi')) for p in pays_l]}")
print(f"     livraisons : {[g(o, 'id') for o in opts_l]}")

order_id = None
if pay:
    st, attrs = call("GET", "/checkout/attributes", tok_a)
    attrs_l = attrs if isinstance(attrs, list) else g(attrs, "items") or []
    sel = [{"attributeId": g(x, "id"), "valueIds": [g((g(x, "values") or [{}])[0], "id")]} for x in attrs_l if g(x, "isRequired") and g(x, "values")]
    print(f"     attributs obligatoires : {[g(x, 'name') for x in attrs_l if g(x, 'isRequired')]}")
    req = {"attributes": sel, "billingAddressId": addr_id, "shippingAddressId": addr_id,
           "shippingOptionId": g(opts_l[0], "id") if opts_l else None, "paymentMethodSystemName": g(pay, "systemName")}
    st, order = call("POST", "/checkout", tok_a, json=req)
    check("commande passée par A", st in (200, 201) and g(order, "orderId"), (st, order))
    order_id = g(order, "orderId")
    print(f"     commande {order_id} total {g(order, 'orderTotalFormatted')}")
else:
    check("commande passée par A", False, "aucun moyen de paiement utilisable par l'API")

if order_id:
    st, mine = call("GET", f"/me/orders/{order_id}", tok_a)
    check("A voit sa commande", st == 200, (st, mine))
    st, other = call("GET", f"/me/orders/{order_id}", tok_b)
    check("B ne voit PAS la commande de A -> 404", st == 404, (st, other))
    st, lst = call("GET", "/me/orders", tok_b)
    check("liste des commandes de B vide", st == 200 and not (g(lst, "items") or []), (st, str(lst)[:200]))
st, _ = call("PUT", f"/me/addresses/{addr_id}", tok_b, json=addr)
check("B ne peut PAS modifier l'adresse de A -> 404", st == 404, st)

# storefront token on back-office endpoints
st, _ = call("GET", "/admin/orders", tok_a)
check("jeton boutique sur back-office -> 403", st == 403, st)

# --- back office ---------------------------------------------------------------
s = nopadmin.login()


def create_client(name, customer_email):
    r = nopadmin.post_form(s, "/Admin/RestApiAdmin/CreateClient",
                           {"Name": name, "CustomerEmail": customer_email, "Active": "true"})
    codes = re.findall(r"<code>([^<]+)</code>", r.text)
    return r.status_code, codes


st, codes = create_client(f"e2e-admin-{stamp}", nopadmin.EMAIL)
check("création client API (admin)", st == 200 and len(codes) >= 2, (st, codes))
cid, secret = (codes[0], codes[1]) if len(codes) >= 2 else (None, None)
# the page shows the client id and the one-time secret; try both orders
bo = None
for c_id, c_sec in ((cid, secret),):
    st, tk = call("POST", "/auth/token", json={"clientId": c_id, "clientSecret": c_sec})
    if st == 200:
        bo = g(tk, "accessToken")
        break
check("jeton back-office (client credentials)", bo, (st, tk))
st, _ = call("POST", "/auth/token", json={"clientId": cid, "clientSecret": "faux"})
check("mauvais secret -> 401", st == 401, st)

if bo:
    st, _ = call("GET", "/me", bo)
    check("jeton back-office sur boutique -> 403", st == 403, st)
    st, orders = call("GET", "/admin/orders", bo, params={"pageSize": 5})
    check("GET admin/orders", st == 200 and g(orders, "items") is not None, (st, str(orders)[:200]))
    if order_id:
        st, o = call("GET", f"/admin/orders/{order_id}", bo)
        check("admin voit la commande de A", st == 200, (st, str(o)[:200]))
    st, custs = call("GET", "/admin/customers", bo, params={"email": email_a})
    check("GET admin/customers", st == 200, (st, str(custs)[:200]))
    st, cats = call("GET", "/admin/categories", bo)
    check("GET admin/categories", st == 200, (st, str(cats)[:200]))
    st, rr = call("GET", "/admin/return-requests", bo)
    check("GET admin/return-requests", st == 200, (st, str(rr)[:200]))
    st, _ = call("POST", "/auth/logout", bo)
    check("déconnexion back-office", st == 204, st)
    st, _ = call("GET", "/admin/orders", bo)
    check("jeton révoqué -> 401", st == 401, st)

# a client bound to a plain customer (no back-office permission) must be refused
st, codes = create_client(f"e2e-noperm-{stamp}", email_b)
ok = False
if len(codes) >= 2:
    for c_id, c_sec in ((codes[0], codes[1]),):
        st, _ = call("POST", "/auth/token", json={"clientId": c_id, "clientSecret": c_sec})
        if st != 401:
            ok = st == 403
            break
check("client lié à un client sans droit -> 403", ok, (st, codes))

st, _ = call("POST", "/auth/logout", tok_a)
check("déconnexion boutique", st == 204, st)
st, _ = call("GET", "/me", tok_a)
check("jeton boutique révoqué -> 401", st == 401, st)

fails = [n for n, ok in results if not ok]
print(f"\n{len(results) - len(fails)}/{len(results)} OK")
sys.exit(1 if fails else 0)
