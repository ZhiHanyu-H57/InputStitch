"""Local static preview with the same clean routes as Cloudflare Pages."""
import argparse
from pathlib import Path
from http.server import SimpleHTTPRequestHandler, ThreadingHTTPServer
root=Path(__file__).resolve().parents[2]/'website'
args=argparse.ArgumentParser()
args.add_argument('--port',type=int,default=0)
options=args.parse_args()
class Handler(SimpleHTTPRequestHandler):
    extensions_map={**SimpleHTTPRequestHandler.extensions_map,'.svg':'image/svg+xml','.webp':'image/webp','.js':'text/javascript','.css':'text/css'}
    def __init__(self,*args,**kwargs): super().__init__(*args,directory=str(root),**kwargs)
    def clean_route(self):
        path=self.path.split('?',1)[0]
        if path in ('/download','/guide','/404'): self.path=self.path.replace(path,path+'.html',1)
    def do_GET(self): self.clean_route(); super().do_GET()
    def do_HEAD(self): self.clean_route(); super().do_HEAD()
    def end_headers(self):
        self.send_header('Content-Security-Policy',"default-src 'self'; img-src 'self' data:; script-src 'self'; connect-src 'self' https://api.github.com; style-src 'self' 'unsafe-inline'; base-uri 'self'; form-action 'none'; frame-ancestors 'none'")
        super().end_headers()
    def log_message(self,*args): pass
server=ThreadingHTTPServer(('127.0.0.1',options.port),Handler)
print('Preview: http://127.0.0.1:'+str(server.server_port),flush=True)
server.serve_forever()
