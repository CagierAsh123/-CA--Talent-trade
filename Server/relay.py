#!/usr/bin/env python3
"""
Talent Trade Relay Server
轻量 HTTP 消息中转，兼容 mod 的 TalentTradeTransport 协议。

用法:
    python3 relay.py                    # 默认 0.0.0.0:8080
    python3 relay.py --port 9090        # 自定义端口

API:
    POST /v1/raw    发送消息
        Headers: X-Room, X-Sender, X-Event-Id
        Body: 纯文本消息内容
        返回: {"ok":true,"id":123}

    GET /v1/raw     拉取消息
        Params: room, after_id, limit, since_ms
        返回: 第一行 lastId，后续每行 id\tbase64(message)
"""

import base64
import json
import threading
import time
import argparse
from http.server import HTTPServer, BaseHTTPRequestHandler
from urllib.parse import urlparse, parse_qs
from collections import defaultdict

# --- 内存存储 ---

lock = threading.Lock()
rooms = defaultdict(list)   # room_name -> [(id, timestamp_ms, sender, event_id, raw_message)]
id_counter = [0]            # 全局自增 ID

# 消息保留时间（秒），超过的自动清理
MAX_AGE_SECONDS = 3600  # 1 小时

def store_message(room, sender, event_id, message):
    with lock:
        id_counter[0] += 1
        msg_id = id_counter[0]
        ts_ms = int(time.time() * 1000)
        rooms[room].append((msg_id, ts_ms, sender, event_id, message))
        return msg_id

def fetch_messages(room, after_id=0, limit=200, since_ms=0):
    with lock:
        msgs = rooms.get(room, [])
        result = []
        for msg_id, ts_ms, sender, event_id, message in msgs:
            if msg_id <= after_id:
                continue
            if since_ms > 0 and ts_ms < since_ms:
                continue
            result.append((msg_id, message))
            if len(result) >= limit:
                break
        last_id = result[-1][0] if result else after_id
        return last_id, result

def cleanup_old_messages():
    """清理过期消息"""
    cutoff_ms = int((time.time() - MAX_AGE_SECONDS) * 1000)
    with lock:
        for room in list(rooms.keys()):
            msgs = rooms[room]
            rooms[room] = [m for m in msgs if m[1] >= cutoff_ms]
            if not rooms[room]:
                del rooms[room]


class RelayHandler(BaseHTTPRequestHandler):

    def log_message(self, format, *args):
        # 简化日志
        pass

    def do_POST(self):
        parsed = urlparse(self.path)
        if parsed.path != "/v1/raw":
            self._send(404, "not found")
            return

        room = self.headers.get("X-Room", "default")
        sender = self.headers.get("X-Sender", "unknown")
        event_id = self.headers.get("X-Event-Id", "")

        content_length = int(self.headers.get("Content-Length", 0))
        body = self.rfile.read(content_length).decode("utf-8") if content_length > 0 else ""

        if not body:
            self._send(400, '{"ok":false,"error":"empty body"}')
            return

        msg_id = store_message(room, sender, event_id, body)
        self._send(200, json.dumps({"ok": True, "id": msg_id}))

    def do_GET(self):
        parsed = urlparse(self.path)
        if parsed.path != "/v1/raw":
            self._send(404, "not found")
            return

        params = parse_qs(parsed.query)
        room = params.get("room", ["default"])[0]
        after_id = int(params.get("after_id", ["0"])[0])
        limit = int(params.get("limit", ["200"])[0])
        since_ms = int(params.get("since_ms", ["0"])[0])

        if limit > 1000:
            limit = 1000

        last_id, messages = fetch_messages(room, after_id, limit, since_ms)

        # 格式：第一行 lastId，后续每行 id\tbase64(message)
        lines = [str(last_id)]
        for msg_id, message in messages:
            b64 = base64.b64encode(message.encode("utf-8")).decode("ascii")
            lines.append("{}\t{}".format(msg_id, b64))

        self._send(200, "\n".join(lines))

    def do_OPTIONS(self):
        self.send_response(200)
        self._cors_headers()
        self.end_headers()

    def _send(self, code, body):
        self.send_response(code)
        self.send_header("Content-Type", "text/plain; charset=utf-8")
        self._cors_headers()
        self.end_headers()
        self.wfile.write(body.encode("utf-8"))

    def _cors_headers(self):
        self.send_header("Access-Control-Allow-Origin", "*")
        self.send_header("Access-Control-Allow-Headers", "*")
        self.send_header("Access-Control-Allow-Methods", "GET, POST, OPTIONS")


def cleanup_loop():
    """后台线程，定期清理过期消息"""
    while True:
        time.sleep(300)  # 每 5 分钟清理一次
        cleanup_old_messages()


def main():
    parser = argparse.ArgumentParser(description="Talent Trade Relay Server")
    parser.add_argument("--host", default="0.0.0.0", help="绑定地址")
    parser.add_argument("--port", type=int, default=8080, help="端口号")
    args = parser.parse_args()

    # 启动清理线程
    t = threading.Thread(target=cleanup_loop, daemon=True)
    t.start()

    server = HTTPServer((args.host, args.port), RelayHandler)
    print("Relay server running on {}:{}".format(args.host, args.port))
    try:
        server.serve_forever()
    except KeyboardInterrupt:
        print("\nShutting down.")
        server.server_close()


if __name__ == "__main__":
    main()
