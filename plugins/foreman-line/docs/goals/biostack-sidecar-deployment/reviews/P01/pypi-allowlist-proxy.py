"""P01 Round-07 Audit Recovery 02 — PyPI-JSON-only CONNECT allowlist proxy.

Stdlib only. Listens on 127.0.0.1 (ephemeral port, printed as PORT=<n>).
Permits exactly CONNECT pypi.org:443, tunneled byte-blind (TLS preserved).
Every other destination or method gets 403 + DENIED log line and a closed
connection. All decisions append to the log file given as argv[1].

Known limitation (disclosed in the Recovery 01/02 grants): a CONNECT tunnel
cannot enforce URL paths inside TLS. Path safety rests on the authorized
command line itself: pip-audit 2.9.0 defaults to the PyPI vulnerability
service (JSON metadata only) and --disable-pip --no-deps removes its internal
venv-bootstrap/resolver paths, so no artifact/index fetch code path is active.
Per-tunnel byte counters are logged; JSON metadata is KB-scale, so any
artifact-scale transfer would be visible here as a tripwire.
"""

import socket
import sys
import threading
import time

ALLOW_HOST = "pypi.org"
ALLOW_PORT = 443
BUF = 65536

log_path = sys.argv[1]
log_lock = threading.Lock()


def log(msg):
    line = "%s %s" % (time.strftime("%Y-%m-%dT%H:%M:%S"), msg)
    with log_lock:
        with open(log_path, "a", encoding="utf-8") as fh:
            fh.write(line + "\n")
    print(line, flush=True)


def relay(src, dst, counter):
    try:
        while True:
            data = src.recv(BUF)
            if not data:
                break
            counter[0] += len(data)
            dst.sendall(data)
    except OSError:
        pass


def handle(conn, addr):
    try:
        conn.settimeout(30)
        head = b""
        while b"\r\n\r\n" not in head and len(head) < 65536:
            chunk = conn.recv(4096)
            if not chunk:
                break
            head += chunk
        try:
            request_line = head.split(b"\r\n", 1)[0].decode("latin-1")
        except Exception:
            request_line = "<undecodable>"
        parts = request_line.split()
        if len(parts) >= 2 and parts[0].upper() == "CONNECT":
            target = parts[1]
            host, _, port = target.partition(":")
            if host.lower() == ALLOW_HOST and port == str(ALLOW_PORT):
                try:
                    up = socket.create_connection((ALLOW_HOST, ALLOW_PORT), timeout=30)
                except OSError as exc:
                    log("DENY-UPSTREAM-CONNECT %s error=%r" % (target, exc))
                    conn.sendall(b"HTTP/1.1 502 Bad Gateway\r\n\r\n")
                    return
                log("ALLOW-CONNECT %s from=%s" % (target, addr[0]))
                conn.sendall(b"HTTP/1.1 200 Connection Established\r\n\r\n")
                c2s = [0]
                s2c = [0]
                t1 = threading.Thread(target=relay, args=(conn, up, c2s), daemon=True)
                t2 = threading.Thread(target=relay, args=(up, conn, s2c), daemon=True)
                t1.start()
                t2.start()
                t1.join()
                t2.join()
                try:
                    up.close()
                except OSError:
                    pass
                log("CLOSE-CONNECT %s bytes_c2s=%d bytes_s2c=%d" % (target, c2s[0], s2c[0]))
                return
            log("DENY-CONNECT target=%s from=%s" % (target, addr[0]))
            conn.sendall(b"HTTP/1.1 403 Forbidden\r\n\r\n")
            return
        log("DENY-METHOD line=%r from=%s" % (request_line[:200], addr[0]))
        try:
            conn.sendall(b"HTTP/1.1 403 Forbidden\r\n\r\n")
        except OSError:
            pass
    finally:
        try:
            conn.close()
        except OSError:
            pass


def main():
    srv = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
    srv.setsockopt(socket.SOL_SOCKET, socket.SO_REUSEADDR, 1)
    srv.bind(("127.0.0.1", 0))
    srv.listen(64)
    port = srv.getsockname()[1]
    print("PORT=%d" % port, flush=True)
    log("LISTEN 127.0.0.1:%d allow=CONNECT %s:%d" % (port, ALLOW_HOST, ALLOW_PORT))
    while True:
        conn, addr = srv.accept()
        threading.Thread(target=handle, args=(conn, addr), daemon=True).start()


if __name__ == "__main__":
    main()
