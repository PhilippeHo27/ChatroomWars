import os
from http.server import SimpleHTTPRequestHandler, HTTPServer

class BrotliHandler(SimpleHTTPRequestHandler):
    def do_GET(self):
        # Check if the request is for a Brotli-compressed file
        if self.path.endswith(".br"):
            file_path = self.translate_path(self.path)
            if os.path.exists(file_path):
                self.send_response(200)
                self.send_header("Content-Encoding", "br")
                self.send_header("Content-Type", "application/octet-stream")
                self.end_headers()
                with open(file_path, "rb") as file:
                    self.wfile.write(file.read())
            else:
                self.send_error(404, "File not found")
        else:
            # Default behavior for other files
            super().do_GET()

if __name__ == "__main__":
    PORT = 8000
    server = HTTPServer(("localhost", PORT), BrotliHandler)
    print(f"Serving on http://localhost:{PORT}")
    server.serve_forever()
