import http.server
import socketserver
import os
import json

PORT = 22005
PROJECT_DIR = os.getcwd()
IMAGE_PATH = os.path.join(PROJECT_DIR, "captures", "screenshot_latest.png")
HEALTH_PATH = os.path.join(PROJECT_DIR, "metadata", "vibe_health.json")
AUDIT_PATH = os.path.join(PROJECT_DIR, "logs", "vibe_audit.jsonl")

class MonitorHandler(http.server.SimpleHTTPRequestHandler):
    def do_GET(self):
        if self.path == "/":
            self.send_response(200)
            self.send_header('Content-type', 'text/html')
            self.end_headers()
            
            health_data = "{}"
            if os.path.exists(HEALTH_PATH):
                with open(HEALTH_PATH, "r") as f: health_data = f.read()
            
            audit_lines = []
            if os.path.exists(AUDIT_PATH):
                with open(AUDIT_PATH, "r") as f:
                    audit_lines = f.readlines()[-5:]
            
            html = f"""
            <!DOCTYPE html>
            <html>
            <head>
                <title>Unity Vibe Monitor</title>
                <meta http-equiv="refresh" content="2">
                <style>
                    body {{ background: #0f0f0f; color: #00ff00; font-family: 'Courier New', monospace; padding: 20px; }}
                    .container {{ display: flex; gap: 20px; }}
                    .view {{ flex: 2; }}
                    .sidebar {{ flex: 1; background: #1a1a1a; padding: 15px; border-radius: 8px; border: 1px solid #333; }}
                    img {{ width: 100%; border: 1px solid #00ff00; box-shadow: 0 0 10px rgba(0,255,0,0.2); }}
                    h1, h2 {{ color: #00ff00; text-transform: uppercase; letter-spacing: 2px; }}
                    .status-box {{ padding: 10px; margin-bottom: 10px; border: 1px solid #444; }}
                    .audit-line {{ font-size: 11px; color: #aaa; margin-bottom: 5px; border-bottom: 1px solid #222; }}
                    .ready {{ color: #00ff00; }}
                    .busy {{ color: #ffff00; }}
                    .error {{ color: #ff0000; }}
                </style>
            </head>
            <body>
                <h1>Unity Vibe Monitor [v16.h]</h1>
                <div class="container">
                    <div class="view">
                        <img src="/image?t={os.path.getmtime(IMAGE_PATH) if os.path.exists(IMAGE_PATH) else 0}" alt="Waiting for AI capture..." />
                    </div>
                    <div class="sidebar">
                        <h2>System Pulse</h2>
                        <div class="status-box">
                            <pre id="health">{health_data}</pre>
                        </div>
                        <h2>Audit Trail</h2>
                        <div id="audit">
                            {"".join([f'<div class="audit-line">{l}</div>' for l in audit_lines])}
                        </div>
                    </div>
                </div>
            </body>
            </html>
            """
            self.wfile.write(html.encode())
        elif self.path.startswith("/image"):
            if os.path.exists(IMAGE_PATH):
                self.send_response(200)
                self.send_header('Content-type', 'image/png')
                self.send_header('Cache-Control', 'no-store, no-cache, must-revalidate')
                self.end_headers()
                with open(IMAGE_PATH, "rb") as f:
                    self.wfile.write(f.read())
            else:
                self.send_response(404)
                self.end_headers()

with socketserver.TCPServer(("", PORT), MonitorHandler) as httpd:
    print(f"🚀 Monitor serving at http://localhost:{PORT}")
    httpd.serve_forever()
