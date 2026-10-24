"""Loopback-only dual-stack nonce peer for the isolated Windows guest."""
import argparse
import json
from pathlib import Path
import socket
import socketserver
import threading
import time

parser = argparse.ArgumentParser()
parser.add_argument('--output', type=Path, required=True)
parser.add_argument('--tcp4', type=int, default=31941)
parser.add_argument('--udp4', type=int, default=31942)
parser.add_argument('--tcp6', type=int, default=31943)
parser.add_argument('--udp6', type=int, default=31944)
args = parser.parse_args()
args.output.mkdir(parents=True, exist_ok=True)
stop = args.output / 'stop-peer'
if stop.exists():
    raise SystemExit('Remove the previous stop-peer marker before starting a new peer.')


class Tcp(socketserver.BaseRequestHandler):
    def handle(self):
        # Keep pre-existing negative-control streams alive during a cold guest run.
        self.request.settimeout(300)
        try:
            while True:
                data = self.request.recv(65536)
                if not data:
                    break
                self.request.sendall(data)
        except (TimeoutError, ConnectionError, OSError):
            pass


class Udp(socketserver.BaseRequestHandler):
    def handle(self):
        data, sock = self.request
        sock.sendto(data, self.client_address)


servers = []
try:
    for family, address, tcp, udp in [
        (socket.AF_INET, '127.0.0.1', args.tcp4, args.udp4),
        (socket.AF_INET6, '::1', args.tcp6, args.udp6),
    ]:
        for base, handler, port in [
            (socketserver.ThreadingTCPServer, Tcp, tcp),
            (socketserver.ThreadingUDPServer, Udp, udp),
        ]:
            server_type = type('EchoServer', (base,), {
                'address_family': family, 'allow_reuse_address': True,
                'daemon_threads': True,
            })
            server = server_type((address, port), handler)
            servers.append(server)
            threading.Thread(target=server.serve_forever, daemon=True).start()
    (args.output / 'peer-ready.json').write_text(json.dumps({
        'ready': True, 'scope': 'host loopback only',
        'ports': [args.tcp4, args.udp4, args.tcp6, args.udp6],
        'time': time.time(),
    }, indent=2), encoding='utf-8')
    print('Dual-stack echo peer ready on loopback only', flush=True)
    while not stop.exists():
        time.sleep(0.5)
finally:
    for server in servers:
        server.shutdown()
        server.server_close()
