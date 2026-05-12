# TODO Final Sweep — N36 (2026-05-13 02h)

## Summary

Total TODO/FIXME/XXX/HACK found: **2**
Resolved: 0 | Deleted: 1 | Deferred: 1

---

## Table

| file:line | TODO text | action |
|-----------|-----------|--------|
| `Assets/Scripts/UI/WorldMapController.cs:119` | `TODO: separate UIDocument with WorldMap.uxml would decouple logic from main scene.` | **DELETED** — speculative architecture note inside a null guard. Guard is self-explanatory; comment added no signal. |
| `Assets/Scripts/Visual/WaterLavaAnimController.cs:26` | `TODO (C1-VISUAL-004): Replace colour animation with texture frame loading` | **DEFERRED** — references ticket C1-VISUAL-004, genuine future visual upgrade. Placeholder colour animation ships fine for V4 parity. |

---

## Top Architectural Deferred

1. **C1-VISUAL-004** (`WaterLavaAnimController.cs:26`) — Swap colour-palette placeholder with real texture frames (water_01..08 / lava_01..08 PNGs). Requires Editor tool + Flux texture gen pass.

*(No further deferred items — codebase is clean.)*
