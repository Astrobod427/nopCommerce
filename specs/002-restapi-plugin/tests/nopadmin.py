"""Staging helpers: admin session on the local nopCommerce staging (127.0.0.1:5010)."""
import os, pathlib, re, requests

BASE = os.environ.get("STAGING_BASE", "https://nopcommerce.atrobod.cloud")
EMAIL = "staging-admin@local.test"
PASSWORD = pathlib.Path(os.environ.get("STAGING_ADMIN_PASSWORD_FILE",
                                     pathlib.Path.home() / "projects/webshops/staging/.staging-admin-password")).read_text().strip()


def token(html):
    m = re.search(r'name="__RequestVerificationToken" type="hidden" value="([^"]+)"', html)
    return m.group(1) if m else None


def login():
    s = requests.Session()
    r = s.get(f"{BASE}/login")
    r = s.post(f"{BASE}/login", data={"Email": EMAIL, "Username": EMAIL, "Password": PASSWORD,
                                      "__RequestVerificationToken": token(r.text)}, allow_redirects=False)
    if r.status_code not in (302, 303):
        raise SystemExit(f"login failed: HTTP {r.status_code}")
    return s


def post_form(s, path, data, get_path=None):
    page = s.get(f"{BASE}{get_path or path}")
    data = dict(data, __RequestVerificationToken=token(page.text))
    return s.post(f"{BASE}{path}", data=data, allow_redirects=False)


def form_fields(html_text, form_action):
    """Current values of a form (inputs, checkboxes, textareas, selects) found by its action"""
    import html as _html
    from html.parser import HTMLParser

    class P(HTMLParser):
        def __init__(self):
            super().__init__()
            self.inside, self.fields, self.textarea = False, {}, None

        def handle_starttag(self, tag, attrs):
            a = dict(attrs)
            if tag == "form":
                self.inside = form_action.lower() in (a.get("action") or "").lower()
            if not self.inside or not a.get("name"):
                return
            if tag == "input":
                kind = (a.get("type") or "text").lower()
                if kind == "checkbox":
                    if "checked" in a:
                        self.fields[a["name"]] = "true"
                    else:
                        self.fields.setdefault(a["name"], "false")
                elif kind not in ("submit", "button"):
                    if a["name"] in self.fields and kind == "hidden":
                        return  # hidden "false" after a checked checkbox
                    self.fields[a["name"]] = a.get("value") or ""
            elif tag == "textarea":
                self.textarea = a["name"]
                self.fields[a["name"]] = ""

        def handle_data(self, data):
            if self.textarea:
                self.fields[self.textarea] += data

        def handle_endtag(self, tag):
            if tag == "textarea":
                self.textarea = None
            if tag == "form":
                self.inside = False

    p = P()
    p.feed(html_text)
    return {k: _html.unescape(v) for k, v in p.fields.items() if k != "__RequestVerificationToken"}
