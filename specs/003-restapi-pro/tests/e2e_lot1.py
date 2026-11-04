"""End-to-end test of spec 003 lot 1 (security) on the staging (nopcommerce.atrobod.cloud).

Run: python3 -I e2e_lot1.py   (from this folder, on vps2: uses docker exec on nop-stg-db / nop-stg-web)
Creates throw-away customers stg-l1-*@local.test and API clients on the STAGING copy only.
Restarts nop-stg-web once (second factor set up directly in the database). Leaves the plugin with its
default protection settings (300 requests / 10 sign-ins per minute, no CORS origin).
"""
import base64, hashlib, hmac, re, struct, subprocess, sys, time
import requests

sys.path.insert(0, __file__.rsplit("/", 1)[0])
import nopadmin  # noqa: E402

API = nopadmin.BASE + "/api/v1"
FRONT = "https://front.example.test"
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
    return r.status_code, body, r.headers


def g(d, *keys):
    for k in keys:
        for cand in (k, k[0].upper() + k[1:], k[0].lower() + k[1:]):
            if isinstance(d, dict) and cand in d:
                return d[cand]
    return None


def sql(query):
    out = subprocess.run(["docker", "exec", "nop-stg-db", "psql", "-U", "nopcommerce", "-d", "nopcommerce", "-tAc", query],
                         capture_output=True, text=True, check=True)
    return out.stdout.strip()


def wait_api():
    for _ in range(120):
        try:
            if requests.get(API + "/store", timeout=10).status_code in (401, 429):
                return True
        except requests.RequestException:
            pass
        time.sleep(3)
    return False


admin = nopadmin.login()


def configure(**values):
    page = admin.get(nopadmin.BASE + "/Admin/RestApiAdmin/Configure").text
    fields = nopadmin.form_fields(page, "Configure")
    fields.update({k: str(v) for k, v in values.items()})
    r = nopadmin.post_form(admin, "/Admin/RestApiAdmin/Configure", fields)
    return r


def totp(key_bytes, at=None):
    counter = int((at or time.time()) // 30)
    digest = hmac.new(key_bytes, struct.pack(">Q", counter), hashlib.sha1).digest()
    offset = digest[-1] & 0x0F
    code = (struct.unpack(">I", digest[offset:offset + 4])[0] & 0x7FFFFFFF) % 1_000_000
    return f"{code:06d}"


stamp = time.strftime("%m%d%H%M%S")
pwd = "Stg-L1-" + stamp + "!"

# --- non-regression of spec 002 with the sign-in limit off ------------------------------------
r = configure(RequestsPerMinute=300, AuthRequestsPerMinute=0, CorsAllowedOrigins=FRONT)
check("réglages enregistrés (admin)", r.status_code in (302, 303), r.status_code)
suite = subprocess.run([sys.executable, "-I", str(__import__("pathlib").Path(__file__).parents[2] / "002-restapi-plugin/tests/e2e_restapi.py")],
                       capture_output=True, text=True, timeout=900)
last = suite.stdout.strip().splitlines()[-1] if suite.stdout.strip() else suite.stderr[-300:]
check("non-régression spec 002 (" + last + ")", suite.returncode == 0 and "FAIL" not in suite.stdout, suite.stdout[-800:])

# --- S3 security headers ------------------------------------------------------------------------
st, _, h = call("GET", "/store")
check("en-têtes : nosniff + no-store", h.get("X-Content-Type-Options") == "nosniff" and "no-store" in h.get("Cache-Control", ""), dict(h))
check("pas de cookie posé par l'API", "Set-Cookie" not in h, h.get("Set-Cookie"))

# --- S2 CORS ------------------------------------------------------------------------------------
r = requests.options(API + "/products", headers={"Origin": FRONT, "Access-Control-Request-Method": "GET",
                                                  "Access-Control-Request-Headers": "authorization"}, timeout=30)
check("CORS preflight site autorisé -> 204 + autorisation", r.status_code == 204 and r.headers.get("Access-Control-Allow-Origin") == FRONT
      and "Authorization" in r.headers.get("Access-Control-Allow-Headers", ""), (r.status_code, dict(r.headers)))
r = requests.options(API + "/products", headers={"Origin": "https://pirate.example.test", "Access-Control-Request-Method": "GET"}, timeout=30)
check("CORS preflight site inconnu -> pas d'autorisation", "Access-Control-Allow-Origin" not in r.headers, dict(r.headers))
st, _, h = call("POST", "/auth/guest", headers={"Origin": FRONT})
check("CORS requête réelle site autorisé", h.get("Access-Control-Allow-Origin") == FRONT and "Retry-After" in h.get("Access-Control-Expose-Headers", ""), dict(h))
r = configure(CorsAllowedOrigins="pas-une-adresse\nhttps://ok.example.test")
check("admin refuse une origine invalide", r.status_code == 200 and "pas-une-adresse" in r.text, r.status_code)
page = admin.get(nopadmin.BASE + "/Admin/RestApiAdmin/Configure").text
check("origine invalide non enregistrée", nopadmin.form_fields(page, "Configure")["CorsAllowedOrigins"].strip() == FRONT,
      nopadmin.form_fields(page, "Configure")["CorsAllowedOrigins"])

# --- S4 activity log: failed sign-in ---------------------------------------------------------
st, guest, _ = call("POST", "/auth/guest")
gtok = g(guest, "accessToken")
email = f"stg-l1-{stamp}@local.test"
st, reg, _ = call("POST", "/auth/register", gtok, json={"email": email, "password": pwd, "firstName": "L1", "lastName": "Test"})
check("inscription client de test", st == 201, (st, reg))
cust_id = sql(f"select \"Id\" from \"Customer\" where \"Email\"='{email}'")
call("POST", "/auth/login", json={"usernameOrEmail": email, "password": "mauvais"})
n = sql(f"select count(*) from \"ActivityLog\" l join \"ActivityLogType\" t on t.\"Id\"=l.\"ActivityLogTypeId\" "
        f"where t.\"SystemKeyword\"='RestApi.SignInFailed' and l.\"CustomerId\"={cust_id}")
check("échec de connexion inscrit au journal (sur le compte visé)", n.isdigit() and int(n) >= 1, n)

# --- S5 IP restriction + S4 back-office writes ------------------------------------------------
def save_client(action, data):
    r = nopadmin.post_form(admin, action, data, get_path="/Admin/RestApiAdmin/CreateClient" if "Create" in action else None)
    return r


r = save_client("/Admin/RestApiAdmin/CreateClient", {"Name": f"l1-ip-{stamp}", "CustomerEmail": nopadmin.EMAIL, "Active": "true",
                                                     "AllowedIpAddresses": "pas-une-ip"})
check("admin refuse une IP invalide", r.status_code == 200 and "pas-une-ip" in r.text and "<code>" not in r.text, r.status_code)

r = save_client("/Admin/RestApiAdmin/CreateClient", {"Name": f"l1-ip-{stamp}", "CustomerEmail": nopadmin.EMAIL, "Active": "true",
                                                     "AllowedIpAddresses": "203.0.113.0/24"})
codes = re.findall(r"<code>([^<]+)</code>", r.text)
cid, secret = (codes[0], codes[1]) if len(codes) >= 2 else (None, None)
client_db_id = sql(f"select \"Id\" from \"ApiClient\" where \"ClientId\"='{cid}'") if cid else ""
check("client API restreint créé", cid and client_db_id, codes)
st, body, _ = call("POST", "/auth/token", json={"clientId": cid, "clientSecret": secret})
check("client hors de ses IP -> 403 ip_not_allowed", st == 403 and g(g(body, "error"), "code") == "ip_not_allowed", (st, body))
n = sql("select count(*) from \"ActivityLog\" l join \"ActivityLogType\" t on t.\"Id\"=l.\"ActivityLogTypeId\" "
        f"where t.\"SystemKeyword\"='RestApi.RequestRefused' and l.\"Comment\" like '%l1-ip-{stamp}%'")
check("refus d'IP inscrit au journal", n.isdigit() and int(n) >= 1, n)

# the IP the store sees for this machine (proxy-aware), read from a token it just issued
my_ip = sql("select \"IpAddress\" from \"ApiToken\" order by \"Id\" desc limit 1").replace("::ffff:", "")
edit_page = admin.get(f"{nopadmin.BASE}/Admin/RestApiAdmin/EditClient/{client_db_id}").text
fields = nopadmin.form_fields(edit_page, "EditClient")
fields["AllowedIpAddresses"] = f"203.0.113.0/24\n{my_ip}"
r = nopadmin.post_form(admin, "/Admin/RestApiAdmin/EditClient", fields, get_path=f"/Admin/RestApiAdmin/EditClient/{client_db_id}")
check(f"IP {my_ip} ajoutée au client", r.status_code in (302, 303), r.status_code)
st, body, _ = call("POST", "/auth/token", json={"clientId": cid, "clientSecret": secret})
bo = g(body, "accessToken")
check("client depuis une IP autorisée -> jeton", st == 200 and bo, (st, body))

st, cat, _ = call("POST", "/admin/categories", bo, json={"name": f"L1 test {stamp}", "published": False})
cat_id = g(cat, "id")
check("écriture back-office (création catégorie)", st in (200, 201) and cat_id, (st, cat))
if cat_id:
    st, _, _ = call("DELETE", f"/admin/categories/{cat_id}", bo)
    check("écriture back-office (suppression catégorie)", st in (200, 204), st)
n = sql("select count(*) from \"ActivityLog\" l join \"ActivityLogType\" t on t.\"Id\"=l.\"ActivityLogTypeId\" "
        f"where t.\"SystemKeyword\"='RestApi.BackOfficeWrite' and l.\"Comment\" like '%l1-ip-{stamp}%/admin/categories%'")
check("écritures back-office inscrites au journal (2)", n.isdigit() and int(n) >= 2, n)
st, _, _ = call("GET", "/admin/categories", bo)
n2 = sql("select count(*) from \"ActivityLog\" l join \"ActivityLogType\" t on t.\"Id\"=l.\"ActivityLogTypeId\" "
         f"where t.\"SystemKeyword\"='RestApi.BackOfficeWrite' and l.\"Comment\" like '%l1-ip-{stamp}%'")
check("lecture back-office non journalisée", n2 == n, (n, n2))

fields = nopadmin.form_fields(admin.get(f"{nopadmin.BASE}/Admin/RestApiAdmin/EditClient/{client_db_id}").text, "EditClient")
fields["AllowedIpAddresses"] = "203.0.113.0/24"
nopadmin.post_form(admin, "/Admin/RestApiAdmin/EditClient", fields, get_path=f"/Admin/RestApiAdmin/EditClient/{client_db_id}")
st, _, _ = call("GET", "/admin/categories", bo)
check("liste d'IP modifiée -> sessions du client fermées (401)", st == 401, st)

# --- S6 second factor (Google Authenticator) ---------------------------------------------------
secret_key = "JBSWY3DPEHPK3PXP"
sql("update \"Setting\" set \"Value\"='MultiFactorAuth.GoogleAuthenticator' "
    "where \"Name\"='multifactorauthenticationsettings.activeauthenticationmethodsystemnames'")
sql(f"delete from \"GoogleAuthenticatorRecord\" where \"Customer\"='{email}'; "
    f"insert into \"GoogleAuthenticatorRecord\" (\"Customer\",\"SecretKey\") values ('{email}','{secret_key}'); "
    "insert into \"GenericAttribute\" (\"KeyGroup\",\"Key\",\"Value\",\"EntityId\",\"StoreId\",\"CreatedOrUpdatedDateUTC\") "
    f"values ('Customer','SelectedMultiFactorAuthProvider','MultiFactorAuth.GoogleAuthenticator',{cust_id},0,now())")
subprocess.run(["docker", "restart", "nop-stg-web"], capture_output=True, check=True)
check("staging redémarré (double facteur activé)", wait_api())
admin = nopadmin.login()

st, ch, _ = call("POST", "/auth/login", json={"usernameOrEmail": email, "password": pwd})
mfa = g(ch, "mfaToken")
check("connexion avec double facteur -> défi (pas de session)", st == 200 and g(ch, "mfaRequired") and mfa and not g(ch, "accessToken"), (st, ch))
st, _, _ = call("GET", "/me", mfa)
check("jeton d'attente refusé comme session -> 401", st == 401, st)
st, body, _ = call("POST", "/auth/mfa", json={"mfaToken": mfa, "code": "000000"})
check("mauvais code -> 401 invalid_code", st == 401 and g(g(body, "error"), "code") == "invalid_code", (st, body))

session = None
for key in (secret_key.encode(), base64.b32decode(secret_key)):
    st, body, _ = call("POST", "/auth/mfa", json={"mfaToken": mfa, "code": totp(key)})
    if st == 200:
        session = g(body, "accessToken")
        break
check("bon code -> session", session, (st, body))
st, me, _ = call("GET", "/me", session)
check("session après double facteur utilisable", st == 200 and g(me, "email") == email, (st, me))
st, _, _ = call("POST", "/auth/mfa", json={"mfaToken": mfa, "code": "000000"})
check("jeton d'attente à usage unique", st == 401, st)

st, ch, _ = call("POST", "/auth/login", json={"usernameOrEmail": email, "password": pwd})
mfa2 = g(ch, "mfaToken")
codes_left = []
for _ in range(5):
    st, body, _ = call("POST", "/auth/mfa", json={"mfaToken": mfa2, "code": "000000"})
    codes_left.append(g(g(body, "error"), "code"))
st, body, _ = call("POST", "/auth/mfa", json={"mfaToken": mfa2, "code": totp(secret_key.encode())})
check("5 mauvais codes -> jeton d'attente détruit", codes_left[-1] == "invalid_mfa_token" and st == 401, (codes_left, st))

# --- S1 rate limits (last: they block this machine for up to a minute) ------------------------
configure(RequestsPerMinute=300, AuthRequestsPerMinute=3)
statuses = [call("POST", "/auth/login", json={"usernameOrEmail": "personne@local.test", "password": "x"})[0] for _ in range(4)]
st, body, h = call("POST", "/auth/guest")
check("limite connexions : 4e tentative -> 429", statuses[:3] == [401, 401, 401] and statuses[3] == 429, statuses)
check("429 avec Retry-After et code rate_limited", st == 429 and h.get("Retry-After", "").isdigit() and g(g(body, "error"), "code") == "rate_limited", (st, dict(h), body))
st, _, _ = call("GET", "/store")
check("limite connexions n'empêche pas la navigation", st == 401, st)

configure(RequestsPerMinute=5, AuthRequestsPerMinute=0)
statuses = [call("GET", "/store")[0] for _ in range(6)]
check("limite générale : 6e requête -> 429", statuses[-1] == 429 and 429 not in statuses[:5], statuses)

configure(RequestsPerMinute=300, AuthRequestsPerMinute=10, CorsAllowedOrigins="")
st, _, _ = call("POST", "/auth/guest")
check("réglages par défaut remis, l'API répond", st == 200, st)

ok = sum(1 for _, passed in results if passed)
print(f"\n{ok}/{len(results)} OK")
sys.exit(0 if ok == len(results) else 1)
