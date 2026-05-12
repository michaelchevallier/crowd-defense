# Ack LIFT-PAUSE + BACKLOG-WAVE-2

**From** : Opus exec orchestrateur
**To** : Mike + Opus superviseur
**Timestamp instruction lue** : 2026-05-12 18h05 (commit 717dd4e)
**Timestamp ack** : 2026-05-12 18h13
**Status** : LIFT confirmé, Wave 2 dispatched 3 P3 refacto parallèles

## Confirmation lift PAUSE-PORT-PIVOT-WIRING

✅ PAUSE levée. V6 @ 85% parité V4 confirmé live (commit 535bef8 qa-tester R1803 PASS).

## Backlog WAVE-2 status

**Tickets déjà complete depuis ack précédent (skip dispatch)** :
- ✅ P1.5 ambient lighting : merged via cherry-pick `abf5db8`. Wire dans ThemeAmbientController.ApplyAmbient subscribe LevelEvents OnLevelStart. ⚠️ 10 SO assets requièrent MenuItem manual `CrowdDefense/Create Theme Ambient Assets` (Mike Unity Editor).
- ✅ P1.6 water/lava : merged `fc93c19`. WaterLavaAnimController 226 LOC + MapRenderer wire +19 LOC. ⚠️ Placeholder couleur (textures water_01..08 + lava_01..08 absentes).
- ✅ P1.1b LINQ : agent précédent a confirmé "no issues to fix" (PerkRegistry/HistoryLogPanel/RunSummaryController déjà protégés).

## Wave 2 dispatched (3 P3 refacto parallèles, différents fichiers PARALLELIZABLE)

| Slot | Ticket | Files | Agent | ETA |
|---|---|---|---|---|
| 1 | **P3.3** R6-REFACTO-CASTLE | `Castle.cs` 762 LOC → core + HP + VFX partials | bug-fixer | 2h |
| 2 | **P3.1** R6-REFACTO-ENEMY | `Enemy.cs` 2051 LOC → core + Movement + Combat + Behaviors + Stats + Anim partials | bug-fixer | 6-8h |
| 3 | **P3.2** R6-REFACTO-TOWER | `Tower.cs` 2254 LOC → core + Combat + Placement + Upgrade + Effects + Anim partials | bug-fixer | 6-8h |
| 4 | Reserved | (slot disponible pour emergent ou Wave 3) | - | - |

## Parallel/Serial classification understood

### 🟢 PARALLELIZABLE (dispatch en masse OK) :
- C# scripts dans différents fichiers (P3.1 Enemy.* / P3.2 Tower.* / P3.3 Castle.* = différents files, parallel safe)
- Audit / docs / markdown

### 🟡 PARALLELIZABLE si fichiers différents :
- 1 prefab/material/SO/anim par agent

### 🔴 SERIAL OBLIGATOIRE :
- `Assets/Scenes/Main.unity` (jamais 2 agents simul, P2.2 FloatingPopup SERIAL)
- `ProjectSettings/*.asset` (Graphics, Quality, Tag, Layer)
- `Packages/manifest.json + lock`

**Application Wave 2** : 3 P3 dispatchés en parallèle car aucun touche Main.unity/ProjectSettings/manifest. Castle.* + Enemy.* + Tower.* = 3 séries de partials indépendantes.

## Wave 3 prévue (post Wave 2 complete)

- R6-PARITY-004-FLUX-REGEN (OPTIONAL, 6-8h) — 22 VFX textures Flux Schnell regen via ComfyUI:8188. Background dispatch si Flux dispo.
- P2.2 FloatingPopup scene fix (SERIAL Main.unity) — exec décide Option A escalation Mike OR Option B auto YAML edit. Defer post-Wave 2 sauf si Mike re-prio.

## qa-tester R1803 verification en cours

Agent `ae24dd2a2d95a4112` (superviseur-spawned) — verify 3 GameObjects wired sont visible live. Findings guide re-prio si needed.

## NEW critère "done" acknowledged (rappel)

- Code C# existe ✅
- Code wired dans gameplay loop ✅
- Code visible côté joueur dans /v6/ build ✅
- Diff /v4/ vs /v6/ confirme parité visuelle (placeholder-equivalent OK) ✅

P3 refacto = "done" requires : compile OK + tests unitaires OK (si dispo) + behavioral parity (regression check via play mode si possible). Pas de "live visual" critère car refacto pur.

## Constraints respectées

- ✅ Cap 500 LOC strict chaque fichier (charter §1 règle #3 TOLERANCE ZERO)
- ✅ No Sub-Opus (bug-fixer Sonnet uniquement)
- ✅ No feature creep (juste split)
- ✅ Self-report 100 mots max chaque agent
- ✅ Parallel-safe : 3 fichiers différents indépendants

## Time cap

Soft autonomous mode. Wave 2 ETA ~6-8h (P3.1 + P3.2 longs). Wave 3 dispatch après slot libère.
