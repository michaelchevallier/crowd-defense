# tools/mixamo — Mixamo Animation Downloader

Batch downloads humanoid animations from Mixamo for Crowd Defense enemies.

## Setup

```bash
pip3 install playwright requests
python3 -m playwright install chromium
```

Already installed on this machine.

## Auth — copy token from Chrome (recommended, 30s)

The Mixamo API uses an Adobe IMS OAuth bearer token, stored in browser
localStorage as `access_token` after login. The fastest way to set up:

1. Open https://www.mixamo.com in Chrome (already logged in)
2. DevTools (Cmd+Opt+I) → Console
3. Run: `copy(localStorage.access_token)`
4. Paste into the token file:
   ```bash
   pbpaste > tools/mixamo/.token
   ```
5. Run the downloader:
   ```bash
   python3 tools/mixamo/download_anims.py
   ```

Token typically expires after 24h. On HTTP 401, repeat steps 2-4.

Alternative: set `MIXAMO_TOKEN` env var with the token value.

## Auth — Playwright fallback

If no `.token`, `MIXAMO_TOKEN`, or `.session.json` is present, the script
opens a fresh Chromium window for Adobe login and captures the token
automatically via network request interception.

## Options

| Flag | Description |
|---|---|
| `--dry-run` | List what would be downloaded, no fetching |
| `--batch-size N` | Max downloads this run (default: 50) |
| `--resume` | Skip already-done anims (default: enabled) |
| `--no-resume` | Re-fetch even if file exists |
| `--reset-progress` | Clear `.progress.json` and restart |

## Batch strategy

Mixamo soft daily limit: ~50 downloads.
Total: 15 enemies × 4 anims = 60 anims.

- **Batch 1** (today): 50 downloads
- **Batch 2** (tomorrow): remaining 10 downloads — `--resume`

Progress tracked in `tools/mixamo/.progress.json`.

## Output

`Assets/Animations/Mixamo/{enemy_key}_{anim}.fbx`

| Animation key | Mixamo query | Unity usage |
|---|---|---|
| `walking` | "Walking" | Idle-moving state |
| `running` | "Running" | Fast-moving state |
| `attack` | "Sword And Shield Attack" | Combat state |
| `dying` | "Dying" | Death state |

## API endpoints (reverse-engineered, 2026)

Base: `https://www.mixamo.com/api/v1/*` (NOT `api.mixamo.com` — deprecated)

| Endpoint | Method | Purpose |
|---|---|---|
| `/products?type=Character&query=X` | GET | Find character by name |
| `/products?type=Motion,MotionPack&query=X` | GET | Find motion by name |
| `/products/{motion_id}?character_id={char}` | GET | Fetch motion gms_hash |
| `/animations/stream` | POST | Retarget motion to character |
| `/animations/export` | POST | Request FBX generation |
| `/characters/{char_id}/monitor` | GET | Poll until job complete |
| S3 presigned URL (`job_result`) | GET | Download FBX |

Headers: `x-api-key: mixamo2` + `Authorization: Bearer <token>`

## Enemy list

See `enemies_humanoid.txt` — 15 humanoid keys.

Excluded (not Mixamo-Humanoid compatible):
- `mob_frog`, `mob_cactoro`, `mob_armabee`, `mob_pigeon` — non-humanoid
- `mob_cyberpunk_flying`, dragon, kraken — flyers / non-humanoid bosses

## Unity import notes

- Rig → Animation Type → Humanoid
- Avatar → Create From This Model
- `AnimationController.cs` in `Assets/Scripts/Visual/` applies clips at runtime

## Troubleshooting

| Error | Cause | Fix |
|---|---|---|
| `HTTP 401: Oauth token is not valid` | Token expired | Re-copy token from Chrome devtools |
| `Failed to resolve api.mixamo.com` | Old code | Script uses `www.mixamo.com/api/v1` |
| 503 on S3 URL | FBX not yet generated | Script polls `monitor` until ready |
| Playwright SingletonLock | Chrome same profile | Script uses temp profile |
