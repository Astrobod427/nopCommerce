"""Staging helpers: admin session on the local nopCommerce staging (127.0.0.1:5010)."""
import pathlib, re, requests

BASE = "http://127.0.0.1:5010"
EMAIL = "staging-admin@local.test"
PASSWORD = (pathlib.Path(__file__).parent.parent / ".staging-admin-password").read_text().strip()


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
