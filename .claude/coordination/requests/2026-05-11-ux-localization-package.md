# Request — Add `com.unity.localization` package to manifest.json

**Date** : 2026-05-11
**From** : SO-UX (Axis F)
**To** : Main Orchestrator
**Status** : OPEN

## Context

Axis F UX Stage A nécessite Unity Localization pour i18n (cf api-contracts.md C5). `Packages/manifest.json` est hot zone (file-ownership.md ligne 24) → SO-UX ne peut pas modifier directement.

## Request

Add to `Packages/manifest.json` :
```json
"com.unity.localization": "1.5.11"
```

## Due diligence

- **Source** : Unity verified registry (`https://docs.unity3d.com/Packages/com.unity.localization@1.5/`)
- **Latest stable** : 1.5.11 (released 2026-03-18, cf CHANGELOG)
- **Compat** : Unity 2019.4+ (Unity 6 OK)
- **WebGL** : pas de mention d'incompat (officiellement supporté multi-platform)
- **Bundle size impact estimé** : +1-2 MB compressé (acceptable)

## Alternative si refus / différé

Stage A SO-UX a livré une implémentation **L.cs fallback Dictionary** (cf `Assets/Scripts/UI/L.cs`) qui :
- Implémente l'API C5 contract exactement (Get, Get with args, SetLocale, OnLocaleChanged event)
- Contient 30+ keys minimum table UI (FR + EN)
- 0 dépendance externe → 0 risque WebGL
- Bundle size impact ≈ 0 KB (compile-time const dict)

Le HudController + SettingsPanelController consomment **uniquement** l'API `L.Get` / `L.SetLocale` → migration vers Unity Localization plus tard sera juste un swap interne de L.cs sans changement consumer code.

## Risque si delay

Aucun bloquant Stage A. Tables Unity Localization (5 StringTableCollection .asset) restent à créer Stage B+ si package ajouté.
