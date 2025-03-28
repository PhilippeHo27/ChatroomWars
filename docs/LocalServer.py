import http.server
import socketserver
import webbrowser
import os
from threading import Timer
import sys

PORT = 8000

def open_browser():
    webbrowser.open(f'http://localhost:{PORT}/index.html')

def main():
    # Get the directory where the script is located
    if getattr(sys, 'frozen', False):
        # For executable
        script_dir = os.path.dirname(sys.executable)
    else:
        # For Python script
        script_dir = os.path.dirname(os.path.abspath(__file__))
    
    print(f"Script directory: {script_dir}")
    print(f"Current working directory before change: {os.getcwd()}")
    
    # Change to script directory
    os.chdir(script_dir)
    print(f"Changed to directory: {os.getcwd()}")
    
    # List files to verify content
    files = os.listdir('.')
    print(f"Files in directory: {files}")
    
    # Check for required files
    required_files = ['index.html', 'game-loader.js', 'styles.css']
    missing_files = [file for file in required_files if file not in files]
    
    if missing_files:
        print(f"WARNING: Missing required files: {missing_files}")
        response = input("Continue anyway? (y/n): ")
        if response.lower() != 'y':
            print("Server startup cancelled")
            return
    
    # Set up the HTTP server
    Handler = http.server.SimpleHTTPRequestHandler
    
    # Add MIME types
    Handler.extensions_map.update({
        '.wasm': 'application/wasm',
        '.js': 'application/javascript',
        '.css': 'text/css',
        '.html': 'text/html',
        '.png': 'image/png',
        '.jpg': 'image/jpeg',
        '.svg': 'image/svg+xml',
        '.data': 'application/octet-stream',
        '.mem': 'application/octet-stream',
        '.json': 'application/json',
        '': 'application/octet-stream',  # Default type
    })

    socketserver.TCPServer.allow_reuse_address = True  # Helps with "address already in use" errors
    
    print(f"Starting server at http://localhost:{PORT}")
    print("To stop the server, press Ctrl+C")

    try:
        with socketserver.TCPServer(("", PORT), Handler) as httpd:
            Timer(1.5, open_browser).start()
            httpd.serve_forever()
    except OSError as e:
        if e.errno == 98:  # Address already in use
            print(f"ERROR: Port {PORT} is already in use. Try closing other applications or changing the port.")
        else:
            print(f"ERROR: {e}")
    except KeyboardInterrupt:
        print("\nServer stopped by user")

if __name__ == "__main__":
    main()
