#!/usr/bin/env python3
import argparse
import http.server
import mimetypes
import os


class Handler(http.server.SimpleHTTPRequestHandler):
    def end_headers(self):
        if self.path.split("?", 1)[0].endswith(".br"):
            self.send_header("Content-Encoding", "br")
        self.send_header("Cache-Control", "no-store")
        super().end_headers()

    def guess_type(self, path):
        if path.endswith(".br"):
            path = path[:-3]
        if path.endswith(".wasm"):
            return "application/wasm"
        return mimetypes.guess_type(path)[0] or "application/octet-stream"


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description="Serve a Unity WebGL build with Brotli headers.")
    parser.add_argument("directory", nargs="?", default="Build/WebGL")
    parser.add_argument("--port", type=int, default=4173)
    args = parser.parse_args()
    os.chdir(args.directory)
    http.server.ThreadingHTTPServer(("127.0.0.1", args.port), Handler).serve_forever()
