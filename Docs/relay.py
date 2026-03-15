#!/usr/bin/env python3
"""
Talent Trade Relay Server (Secured)
API Key 认证 + IP 速率限制 + 消息体大小限制 + HMAC 消息签名
"""
import base64,json,threading,time,hashlib,hmac
from http.server import HTTPServer,BaseHTTPRequestHandler
from urllib.parse import urlparse,parse_qs
from collections import defaultdict

# ========== 安全配置 ==========
API_KEY = "tt-9f3a7c2e1b4d6058e7a2c9d4f1b3e5a8"  # mod 里要配一样的
SIGNING_KEY = "tt-sign-b7e2f4a19c3d5068d1e4a7b2c8f0e3d6"  # HMAC 签名密钥
MAX_BODY_BYTES = 512 * 1024       # 单条消息最大 512KB
RATE_LIMIT_PER_IP = 60            # 每个 IP 每分钟最多 60 次 POST
RATE_WINDOW_SECONDS = 60
# ==============================

lock = threading.Lock()
rooms = defaultdict(list)
id_counter = [0]

# 速率限制: ip -> [(timestamp), ...]
rate_lock = threading.Lock()
rate_buckets = defaultdict(list)

def check_rate_limit(ip):
    now = time.time()
    cutoff = now - RATE_WINDOW_SECONDS
    with rate_lock:
        bucket = rate_buckets[ip]
        # 清理过期记录
        rate_buckets[ip] = [t for t in bucket if t > cutoff]
        if len(rate_buckets[ip]) >= RATE_LIMIT_PER_IP:
            return False
        rate_buckets[ip].append(now)
        return True

def cleanup_rate_buckets():
    while True:
        time.sleep(120)
        now = time.time()
        cutoff = now - RATE_WINDOW_SECONDS * 2
        with rate_lock:
            for ip in list(rate_buckets.keys()):
                rate_buckets[ip] = [t for t in rate_buckets[ip] if t > cutoff]
                if not rate_buckets[ip]:
                    del rate_buckets[ip]

def store(room, sender, eid, msg):
    with lock:
        id_counter[0] += 1
        mid = id_counter[0]
        ts = int(time.time() * 1000)
        rooms[room].append((mid, ts, sender, eid, msg))
        return mid

def fetch(room, after=0, limit=200, since=0):
    with lock:
        msgs = rooms.get(room, [])
        r = []
        for mid, ts, s, e, m in msgs:
            if mid <= after: continue
            if since > 0 and ts < since: continue
            r.append((mid, m))
            if len(r) >= limit: break
        lid = r[-1][0] if r else after
        return lid, r

def cleanup():
    while True:
        time.sleep(300)
        cut = int((time.time() - 3600) * 1000)
        with lock:
            for rm in list(rooms.keys()):
                rooms[rm] = [m for m in rooms[rm] if m[1] >= cut]
                if not rooms[rm]: del rooms[rm]

class H(BaseHTTPRequestHandler):
    def log_message(self, *a): pass

    def _check_auth(self):
        key = self.headers.get("X-Api-Key", "")
        if key != API_KEY:
            self._s(403, '{"error":"forbidden"}')
            return False
        return True

    def _verify_signature(self, body_bytes):
        sig = self.headers.get("X-Signature", "")
        if not sig:
            self._s(403, '{"error":"missing signature"}')
            return False
        expected = hmac.new(SIGNING_KEY.encode("utf-8"), body_bytes, hashlib.sha256).hexdigest()
        if not hmac.compare_digest(sig, expected):
            self._s(403, '{"error":"invalid signature"}')
            return False
        return True

    def _get_client_ip(self):
        # 支持反代 X-Forwarded-For
        xff = self.headers.get("X-Forwarded-For")
        if xff:
            return xff.split(",")[0].strip()
        return self.client_address[0]

    def do_POST(self):
        if not self._check_auth(): return
        p = urlparse(self.path)
        if p.path != "/v1/raw": self._s(404, "not found"); return

        # 速率限制
        ip = self._get_client_ip()
        if not check_rate_limit(ip):
            self._s(429, '{"error":"rate limited"}')
            return

        cl = int(self.headers.get("Content-Length", 0))
        # 消息体大小限制
        if cl > MAX_BODY_BYTES:
            self._s(413, '{"error":"payload too large"}')
            return

        body_bytes = self.rfile.read(cl) if cl > 0 else b""
        if not body_bytes: self._s(400, "empty"); return

        # HMAC 签名验证
        if not self._verify_signature(body_bytes): return

        body = body_bytes.decode("utf-8")
        rm = self.headers.get("X-Room", "default")
        sn = self.headers.get("X-Sender", "unknown")
        ei = self.headers.get("X-Event-Id", "")
        mid = store(rm, sn, ei, body)
        self._s(200, json.dumps({"ok": True, "id": mid}))

    def do_GET(self):
        if not self._check_auth(): return
        p = urlparse(self.path)
        if p.path != "/v1/raw": self._s(404, "not found"); return
        q = parse_qs(p.query)
        rm = q.get("room", ["default"])[0]
        ai = int(q.get("after_id", ["0"])[0])
        li = min(int(q.get("limit", ["200"])[0]), 1000)
        si = int(q.get("since_ms", ["0"])[0])
        lid, msgs = fetch(rm, ai, li, si)
        lines = [str(lid)]
        for mid, m in msgs:
            b = base64.b64encode(m.encode("utf-8")).decode("ascii")
            lines.append("{}\t{}".format(mid, b))
        self._s(200, "\n".join(lines))

    def do_OPTIONS(self):
        self.send_response(200); self._ch(); self.end_headers()

    def _s(self, c, b):
        self.send_response(c)
        self.send_header("Content-Type", "text/plain; charset=utf-8")
        self._ch(); self.end_headers()
        self.wfile.write(b.encode("utf-8"))

    def _ch(self):
        self.send_header("Access-Control-Allow-Origin", "*")
        self.send_header("Access-Control-Allow-Headers", "*")
        self.send_header("Access-Control-Allow-Methods", "GET,POST,OPTIONS")

if __name__ == "__main__":
    threading.Thread(target=cleanup, daemon=True).start()
    threading.Thread(target=cleanup_rate_buckets, daemon=True).start()
    s = HTTPServer(("0.0.0.0", 8080), H)
    print("Relay running on :8080 (secured)")
    s.serve_forever()
