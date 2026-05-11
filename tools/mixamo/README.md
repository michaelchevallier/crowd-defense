# tools/mixamo — Mixamo Animation Downloader

Downloads humanoid animations from Mixamo for Crowd Defense enemies.

## Setup

```bash
pip3 install playwright requests
python3 -m playwright install chromium
```

Both are already installed on this machine.

## First run (Adobe login required)

```bash
cd /Users/mike/Work/crowd-defense
python3 tools/mixamo/download_anims.py
```

A Chromium browser window opens. Log in to [mixamo.com](https://mixamo.com) with your Adobe account.
After login the script detects the session automatically and continues.
Session cookies are saved to `tools/mixamo/.session.json` for reuse.

## Subsequent runs (headless, no browser)

```bash
python3 tools/mixamo/download_anims.py --resume
```

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

If you see `HTTP 401` or the script hangs at login:

```bash
rm tools/mixamo/.session.json
python3 tools/mixamo/download_anims.py
```

A new Chromium window will open for fresh login.

## Enemy list

See `enemies_humanoid.txt` — 15 humanoid keys.

Excluded (not Mixamo-Humanoid compatible):
- `mob_frog`, `mob_cactoro`, `mob_armabee`, `mob_pigeon` — non-humanoid creatures
- `mob_cyberpunk_flying`, dragon, kraken — flyers / non-humanoid bosses

## Unity import notes

- Import setting: Rig → Animation Type → Humanoid
- Avatar: Create From This Model
- AnimationController already present in `Assets/Animations/Controllers/`
- `AnimationController.cs` in `Assets/Scripts/Visual/` applies clips at runtime
