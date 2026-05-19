#!/usr/bin/env bash
# webgl-serve.sh — Serve Unity WebGL build locally with Brotli support
# Usage: ./tools/webgl-serve.sh [port]
# Requires: Python 3 (for fallback) OR npx serve (recommended for .unityweb Brotli files)

set -euo pipefail
REPO_ROOT="$(cd "$(dirname "$0")/.." && pwd)"
WEBGL_DIR="$REPO_ROOT/Builds/WebGL"
PORT="${1:-8000}"

if [ ! -f "$WEBGL_DIR/index.html" ]; then
  echo "ERROR: $WEBGL_DIR/index.html not found — build WebGL first in Unity."
  exit 1
fi

cd "$WEBGL_DIR"

# Unity WebGL with Brotli (.unityweb) needs:
#   Content-Encoding: br  for .unityweb files
#   Correct MIME types:  .wasm.unityweb → application/wasm
#                        .js.unityweb   → application/javascript
#                        .data.unityweb → application/octet-stream
#
# npx serve handles this via serve.json headers config (see below).
# Python http.server does NOT set Content-Encoding:br → browser decompression fails.

if command -v npx &>/dev/null; then
  # Write a temporary serve.json for correct headers
  cat > /tmp/webgl-serve.json <<'JSON'
{
  "headers": [
    {
      "source": "**/*.unityweb",
      "headers": [
        { "key": "Content-Encoding", "value": "br" }
      ]
    },
    {
      "source": "**/*.wasm.unityweb",
      "headers": [
        { "key": "Content-Type",     "value": "application/wasm" },
        { "key": "Content-Encoding", "value": "br" }
      ]
    },
    {
      "source": "**/*.js.unityweb",
      "headers": [
        { "key": "Content-Type",     "value": "application/javascript" },
        { "key": "Content-Encoding", "value": "br" }
      ]
    },
    {
      "source": "**/*.data.unityweb",
      "headers": [
        { "key": "Content-Type",     "value": "application/octet-stream" },
        { "key": "Content-Encoding", "value": "br" }
      ]
    }
  ]
}
JSON
  echo "Serving $WEBGL_DIR on http://localhost:$PORT (npx serve + Brotli headers)"
  echo "Open: http://localhost:$PORT"
  npx serve@latest -p "$PORT" --config /tmp/webgl-serve.json .
else
  echo "WARNING: npx not found — falling back to Python http.server."
  echo "Brotli (.unityweb) files will likely fail to decompress in browser."
  echo "Install Node.js for proper Brotli support: https://nodejs.org"
  echo ""
  echo "Serving $WEBGL_DIR on http://localhost:$PORT"
  echo "Open: http://localhost:$PORT"
  python3 -m http.server "$PORT"
fi
