# Blender Setup Notes — Crowd Defense ASSET-GEN axis

**Date** : 2026-05-11
**Author** : SO-ASSET-GEN (Opus 4.7 orchestrator)

## TL;DR état actuel

- Blender 5.1.1 installé `/Applications/Blender.app` (via brew cask, OK)
- Blender MCP addon disponible `~/tools/blender-mcp/addon.py` (111 KB, OK)
- Blender MCP server config présent `~/.claude.json` → `mcpServers.blender = uvx blender-mcp` (OK)
- **MCP /mcp Failed to reconnect** : cause identifiée — port 9876 occupé par autre process Python (`/tmp/capture_server.py`), bloque le bind du MCP addon Blender.
- Re-export 11 .gltf → .glb peut tourner **sans MCP**, via `blender --background --python ...` direct.

## Diagnostic Blender MCP

### Symptômes Mike (2026-05-11)
> "/mcp Failed to reconnect to blender"

### Cause racine
Port `9876/tcp` (default Blender MCP addon listener) **déjà occupé par un autre service Python** :

```bash
$ lsof -i :9876
COMMAND   PID USER   FD   TYPE             DEVICE SIZE/OFF NODE NAME
Python  61776 mike    3u  IPv4 0xd3678b70298f035c  0t0    TCP localhost:9876 (LISTEN)

$ ps -p 61776 -o command=
/Library/Frameworks/Python.framework/Versions/3.11/Resources/Python.app/Contents/MacOS/Python /tmp/capture_server.py
```

Ce `/tmp/capture_server.py` est un service de screenshot capture (Image.fromstring HTTP server) sans rapport avec le Blender MCP. Probablement spawned par une session Claude Code antérieure de Mike.

Tant que ce process tient 9876, le Blender MCP addon ne peut pas écouter et `/mcp` reconnect échoue.

### Cause secondaire potentielle
Addon Blender MCP peut ne pas être activé dans Preferences. À vérifier manuellement par Mike.

## Steps fix (manuels Mike)

### 1. Libérer le port 9876
```bash
kill 61776   # ou : kill $(lsof -ti :9876)
# Verify :
lsof -i :9876   # doit retourner vide
```

### 2. Vérifier que Blender MCP addon est activé

Dans Blender :
1. `Edit > Preferences > Add-ons`
2. Chercher "BlenderMCP" dans la liste
3. Si pas listé : `Install...` → sélectionner `~/tools/blender-mcp/addon.py` → enable
4. Si listé : cocher la checkbox pour activer

### 3. Connecter à Claude
1. Dans Blender : presser `N` pour ouvrir la sidebar de la 3D viewport
2. Onglet "BlenderMCP" doit apparaître
3. Click "Connect to Claude" → l'addon démarre son socket server sur 9876

### 4. Tester depuis Claude Code
```
/mcp
# blender doit apparaître "✓ Connected"
```

## Re-export 11 .gltf → .glb (sans MCP)

Le script `reexport_gltf_to_glb.py` tourne en headless Blender, pas besoin du MCP. Usage :

```bash
# Single file :
/Applications/Blender.app/Contents/MacOS/Blender --background \
  --python tools/blender/reexport_gltf_to_glb.py -- Assets/Models/Enemies/goblin.gltf

# Batch les 11 :
for name in goblin knight knightgolden mob_cyberpunk_2legs mob_cyberpunk_character \
            mob_cyberpunk_flying mob_cyberpunk_large pirate soldier wizard zombie; do
  /Applications/Blender.app/Contents/MacOS/Blender --background \
    --python tools/blender/reexport_gltf_to_glb.py -- "Assets/Models/Enemies/${name}.gltf"
done
```

Output : 11 nouveaux `.glb` dans `Assets/Models/Enemies/`. Les `.gltf` originaux peuvent être supprimés une fois la re-import Unity validée (rebuild AssetRegistry → check 60 entries).

## Note diagnostic vs brief initial

Le brief mentionnait "refs textures manquantes" comme cause probable des skips. Inspection JSON révèle :
- Tous les buffers sont déjà **embed base64** dans la .gltf (data:application/octet-stream;base64,...)
- **0 images externes** déclarées dans `"images": []`
- **0 extensions requises** (`extensionsRequired: []`)

La vraie cause des skips est donc côté **UnityGLTF parser** : il rejette ces .gltf pour une raison interne (probablement `KHR_*` extension support ou animations structure spécifique exportée par Blender Khronos I/O v1.6.16). Re-export Blender→.glb régénère un fichier propre que UnityGLTF accepte.
