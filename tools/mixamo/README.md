# tools/mixamo — Mixamo Animation Downloader

Batch downloads humanoid animations from Mixamo for Crowd Defense enemies.

## Setup

```bash
pip3 install playwright requests
python3 -m playwright install chromium
```

Both are already installed on this machine.

## Auth — extract bearer token from Chrome (recommended, 30s)

The Mixamo API uses an Adobe IMS OAuth bearer token. Since you are already
logged in to Mixamo in Chrome, the fastest way is to copy the token directly:

1. Open https://www.mixamo.com in your Chrome
2. Open DevTools (Cmd+Opt+I) → Console tab
3. Run: `copy(localStorage.access_token)`
4. Paste into `tools/mixamo/.token` (one line, no quotes):
   ```bash
   pbpaste > tools/mixamo/.token
   ```
5. Run the script:
   ```bash
   python3 tools/mixamo/download_anims.py
   ```

The token is JWT-formatted; it typically expires after 24h. If you get HTTP 401,
re-extract the token using the same steps above.

Alternative: set `MIXAMO_TOKEN` env var with the token value.

## Auth — automated Playwright login (fallback)

If `.token` / `MIXAMO_TOKEN` / `.session.json` are all absent, the script opens
a fresh Chromium window for Adobe login. The script captures the OAuth token
automatically and saves it to `.session.json` for reuse.

## Options

| Flag | Description |
|---|---|
| `--dry-run` | List what would be downloaded, no actual fetching |
| `--batch-size N` | Max downloads this run (default: 50) |
| `--resume` | Skip already-downloaded anims (default: enabled) |
| `--no-resume` | Re-fetch even if file exists |
| `--reset-progress` | Clear `.progress.json` and start fresh |

## Batch strategy

Mixamo soft daily limit: ~50 downloads.
Total: 15 enemies × 4 anims = 60 anims.

- **Batch 1** (today): 50 downloads — 12 enemies × 4 + first 2 boss anims
- **Batch 2** (tomorrow): remaining 10 downloads

Progress tracked in `tools/mixamo/.progress.json`.

## Output

FBX files: `Assets/Animations/Mixamo/{enemy_key}_{anim}.fbx`

| Animation key | Mixamo query | Unity usage |
|---|---|---|
| `walking` | "Walking" | Idle-moving state |
| `running` | "Running" | Fast-moving state |
| `attack` | "Sword And Shield Attack" | Combat state |
| `dying` | "Dying" | Death state |

## Session expiry / re-login

If you see `HTTP 401` or "OAuth token expired":

```bash
# In Chrome devtools: copy(localStorage.access_token)
pbpaste > tools/mixamo/.token
python3 tools/mixamo/download_anims.py --resume
```

## API endpoints used (reverse-engineered)

Base: `https://www.mixamo.com/api/v1/*` (NOT `api.mixamo.com` — deprecated)

| Endpoint | Method | Purpose |
|---|---|---|
| `/products?type=Character&query=X` | GET | Find character ID by name |
| `/products?type=Motion,MotionPack&query=X` | GET | Find motion ID by name |
| `/products/{motion_id}?character_id={char_id}` | GET | Fetch motion gms_hash |
| `/animations/stream` | POST | Retarget motion to character |
| `/animations/export` | POST | Request FBX generation |
| `/characters/{char_id}/monitor` | GET | Poll until job complete |
| S3 presigned URL (`job_result`) | GET | Download FBX (or ZIP-wrapped FBX) |

Headers: `x-api-key: mixamo2` + `Authorization: Bearer <token>`

## Enemy list

See `enemies_humanoid.txt` — 15 humanoid keys.

Excluded (not Mixamo-Humanoid compatible):
- `mob_frog`, `mob_cactoro`, `mob_armabee`, `mob_pigeon` — non-humanoid
- `mob_cyberpunk_flying`, dragon, kraken — flyers / non-humanoid bosses

## Unity import notes

- Import setting: Rig → Animation Type → Humanoid
- Avatar: Create From This Model
- AnimationController already present in `Assets/Animations/Controllers/`
- `AnimationController.cs` in `Assets/Scripts/Visual/` applies clips at runtime

## Troubleshooting

| Error | Cause | Fix |
|---|---|---|
| `HTTP 401: Oauth token is not valid` | Token expired | Re-copy token from Chrome devtools |
| `Failed to resolve api.mixamo.com` | Old script using deprecated host | Script already uses `www.mixamo.com/api/v1` |
| Playwright SingletonLock | Chrome running same profile | Script uses temp profile |
| 503 on S3 URL | FBX not yet generated | Script polls monitor endpoint until ready |
