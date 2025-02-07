import http.server
import socketserver
import webbrowser
import os
from threading import Timer

PORT = 8000
BUILD_DIRECTORY = os.path.dirname(os.path.abspath(__file__))  # Gets absolute path of script location

def open_browser():
    webbrowser.open(f'http://localhost:{PORT}/index.html')

def main():
    print(f"Current working directory: {os.getcwd()}")
    print(f"Script directory: {BUILD_DIRECTORY}")
    
    os.chdir(BUILD_DIRECTORY)
    print(f"Changed to directory: {os.getcwd()}")
    print(f"Files in directory: {os.listdir('.')}")
    
    Handler = http.server.SimpleHTTPRequestHandler
    Handler.extensions_map.update({
        '.wasm': 'application/wasm',
    })

    with socketserver.TCPServer(("", PORT), Handler) as httpd:
        print(f"Serving at http://localhost:{PORT}")
        print("To stop the server, press Ctrl+C")
        Timer(1.5, open_browser).start()
        try:
            httpd.serve_forever()
        except KeyboardInterrupt:
            print("\nServer stopped by user")

if __name__ == "__main__":
    main()
