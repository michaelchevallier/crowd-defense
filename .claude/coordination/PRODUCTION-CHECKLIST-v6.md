# Production Checklist v6 — Iso V4 Final

**Date** : 2026-05-12
**Target** : parity with V4 final ("ready ship" state)
**Stack** : Unity 6 LTS (`6000.0.74f1`), C# / .NET Standard 2.1

---

## Build & Deploy

- [x] Build clean compile (status: clean, no errors)
- [ ] WebGL deploy gh-pages latest (status: 650 commits behind, Opus fix en cours)

## Content Wiring

- [x] All assets (skins, animations) wired (status: 33 controllers + 92 registry entries)
- [ ] All scenes load without crash (Loader → Menu → WorldMap → Main → end)

## UX & Platform

- [x] Mobile touch UX working (commits a6124fb + 8445cb3)
- [x] Sound + music play correctly (commits 9cb33fd + 99d5d59)
- [x] Save/Load reliable (commits d264039 + 3515176)

## Gameplay Systems

- [x] Difficulty Easy/Normal/Hard working (commit 03600d6)
- [x] Hero pick screen + perks pick (commits 4769efd + cae4b7b)
- [ ] Boss flow OK (intro banner + 4 phases + enrage VFX + death cinematic)

## Persistence & Localization

- [x] Achievements / Bestiary / Lifetime persistent (commits 97347cf + 9424cfe + ec5a1bc)
- [x] All localizations FR/EN/ES complete (commit df02b2e + audit a90f58fa)

## Performance & Distribution

- [ ] Performance 60 FPS desktop / 30 FPS mobile (VfxPool LOD + GPU instancing)
- [ ] Mobile build iOS Xcode (TODO Mike — credentials Apple Developer + cert local)
- [ ] Steam build (TODO Mike — Steamworks SDK setup + appid)

---

## Legend

- [x] = done / verified
- [ ] = pending / in progress
- ✅ status: completed
- ⚠️ status: known gap, fix planned
- 🚧 status: in progress

## Notes

Items marked TODO Mike require manual action (credentials, store submission, dev account setup) — non-automatable from Claude Code.
