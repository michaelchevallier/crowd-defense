# ComfyUI MCP Setup Notes — Crowd Defense ASSET-GEN axis

**Date** : 2026-05-11
**Author** : SO-ASSET-GEN

## TL;DR

- ComfyUI server **listening on `http://127.0.0.1:8188`** (PID 35226, OK)
- MCP server config **présent** dans `~/.claude.json` (entry `comfy`, OK)
- Tools exposed via MCP : `mcp__comfy__generate_image(prompt)` + `mcp__comfy__generate_prompt(topic)`
- Workflow file référencé : `/Users/mike/flux-local/workflows/flux_schnell_basic.json` (Flux Schnell basic, prompt node 6, output node 9, output mode file)

## Verification (Opus 2026-05-11)

```bash
$ lsof -i :8188 | head -3
COMMAND   PID USER   FD   TYPE             DEVICE SIZE/OFF NODE NAME
Python  35226 mike    8u  IPv4 0xf7052833b7d395ca  0t0    TCP localhost:8188 (LISTEN)

$ cat ~/.claude.json | jq '.mcpServers.comfy'
{
  "command": "uvx",
  "args": ["--with", "mcp<1.6", "comfy-mcp-server"],
  "env": {
    "COMFY_URL": "http://127.0.0.1:8188",
    "COMFY_WORKFLOW_JSON_FILE": "/Users/mike/flux-local/workflows/flux_schnell_basic.json",
    "PROMPT_NODE_ID": "6",
    "OUTPUT_NODE_ID": "9",
    "OUTPUT_MODE": "file"
  }
}
```

Both prerequisites pass. From a Claude Code session, after `/mcp restart` or fresh launch, the tool `mcp__comfy__generate_image` should be available via `ToolSearch`.

## Test invoke (pour valider end-to-end)

Une fois `/mcp` actif côté Claude Code :

```
ToolSearch select:mcp__comfy__generate_image
mcp__comfy__generate_image(prompt="low-poly stylized goblin enemy, cell shading, vibrant colors, isolated on transparent background, game-ready 3D texture")
```

Output attendu : path local à un .png généré par Flux Schnell, ~1024×1024, sous `~/flux-local/output/` ou similaire selon workflow.

## Limitations connues

- **2 tools seulement** : pas de batch, pas de LoRA selection. Pour workflows avancés (multiple LoRAs, ControlNet, batch 4-up), tomber back sur `python3 /Users/mike/Work/milan project/tools/gen_textures.py` qui pilote ComfyUI:8188 directement via REST.
- **Workflow figé** : un seul JSON workflow lu via env var. Pour switcher de workflow → modifier `COMFY_WORKFLOW_JSON_FILE` dans `~/.claude.json` + restart Claude Code.
- **Output mode file** : retourne un path local. Pour récupérer l'image binary, utiliser `Read tool` sur le path retourné.

## Caveats

- ComfyUI doit rester running (PID 35226 actuellement). Si reboot Mac → relancer ComfyUI server avant Claude Code.
- Le workflow `flux_schnell_basic.json` doit exister au path indiqué (non vérifié par Opus, à valider par Mike — si manquant, blocker).

## Voir aussi

- Memory : `~/.claude/projects/-Users-mike-Work-crowd-defense/memory/reference_flux_local.md` (pipeline Flux + `gen_textures.py`)
- Status backlog : `STATUS.md` section "2. ComfyUI MCP — T1 SETUP DONE"
- Source MCP repo : https://github.com/lalanikarim/comfy-mcp-server
