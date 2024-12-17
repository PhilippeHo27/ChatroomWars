import http.server
import socketserver
import webbrowser
import os
from threading import Timer

# Configuration
PORT = 8000
BUILD_DIRECTORY = "."  # Current directory where the script is located

def open_browser():
    webbrowser.open(f'http://localhost:{PORT}/index.html')

def main():
    # Change to the build directory
    os.chdir(BUILD_DIRECTORY)
    
    # Create the server
    Handler = http.server.SimpleHTTPRequestHandler
    Handler.extensions_map.update({
        '.wasm': 'application/wasm',
    })

    # Configure and start the server
    with socketserver.TCPServer(("", PORT), Handler) as httpd:
        print(f"Serving at http://localhost:{PORT}")
        print("To stop the server, press Ctrl+C")
        
        # Open browser after a short delay
        Timer(1.5, open_browser).start()
        
        try:
            httpd.serve_forever()
        except KeyboardInterrupt:
            print("\nServer stopped by user")

if __name__ == "__main__":
    main()
