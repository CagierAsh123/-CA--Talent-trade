#!/usr/bin/env python3
import base64,json,threading,time
from http.server import HTTPServer,BaseHTTPRequestHandler
from urllib.parse import urlparse,parse_qs
from collections import defaultdict
lock=threading.Lock()
rooms=defaultdict(list)
id_counter=[0]
def store(room,sender,eid,msg):
    with lock:
        id_counter[0]+=1
        mid=id_counter[0]
        ts=int(time.time()*1000)
        rooms[room].append((mid,ts,sender,eid,msg))
        return mid
def fetch(room,after=0,limit=200,since=0):
    with lock:
        msgs=rooms.get(room,[])
        r=[]
        for mid,ts,s,e,m in msgs:
            if mid<=after:continue
            if since>0 and ts<since:continue
            r.append((mid,m))
            if len(r)>=limit:break
        lid=r[-1][0] if r else after
        return lid,r
def cleanup():
    while True:
        time.sleep(300)
        cut=int((time.time()-3600)*1000)
        with lock:
            for rm in list(rooms.keys()):
                rooms[rm]=[m for m in rooms[rm] if m[1]>=cut]
                if not rooms[rm]:del rooms[rm]
class H(BaseHTTPRequestHandler):
    def log_message(self,*a):pass
    def do_POST(self):
        p=urlparse(self.path)
        if p.path!="/v1/raw":self._s(404,"not found");return
        rm=self.headers.get("X-Room","default")
        sn=self.headers.get("X-Sender","unknown")
        ei=self.headers.get("X-Event-Id","")
        cl=int(self.headers.get("Content-Length",0))
        body=self.rfile.read(cl).decode("utf-8") if cl>0 else ""
        if not body:self._s(400,"empty");return
        mid=store(rm,sn,ei,body)
        self._s(200,json.dumps({"ok":True,"id":mid}))
    def do_GET(self):
        p=urlparse(self.path)
        if p.path!="/v1/raw":self._s(404,"not found");return
        q=parse_qs(p.query)
        rm=q.get("room",["default"])[0]
        ai=int(q.get("after_id",["0"])[0])
        li=min(int(q.get("limit",["200"])[0]),1000)
        si=int(q.get("since_ms",["0"])[0])
        lid,msgs=fetch(rm,ai,li,si)
        lines=[str(lid)]
        for mid,m in msgs:
            b=base64.b64encode(m.encode("utf-8")).decode("ascii")
            lines.append("{}\t{}".format(mid,b))
        self._s(200,"\n".join(lines))
    def do_OPTIONS(self):
        self.send_response(200);self._ch();self.end_headers()
    def _s(self,c,b):
        self.send_response(c)
        self.send_header("Content-Type","text/plain; charset=utf-8")
        self._ch();self.end_headers()
        self.wfile.write(b.encode("utf-8"))
    def _ch(self):
        self.send_header("Access-Control-Allow-Origin","*")
        self.send_header("Access-Control-Allow-Headers","*")
        self.send_header("Access-Control-Allow-Methods","GET,POST,OPTIONS")
if __name__=="__main__":
    threading.Thread(target=cleanup,daemon=True).start()
    s=HTTPServer(("0.0.0.0",80),H)
    print("Relay running on :80")
    s.serve_forever()
