# Axis F — UX-POLISH Plan évolutif

**Date** : 2026-05-11
**Branch** : `axis/ux`
**Sub-Opus** : SO-UX
**Budget** : 6-10h

---

## Stage A deliverables

### F.1 — Fix HUD font Roboto (priorité 1 CRITIQUE)

**Diagnostic** :
- USS files (HUD.uss, RadialMenu.uss, MenuLevelSelect.uss) référencent `url("project://database/Assets/Fonts/Roboto-Regular.ttf")` directement.
- En WebGL, UI Toolkit ne charge pas fiablement les .ttf bruts → "Unable to load font face for [Roboto-Regular]".
- Solution canon Unity 6 : **FontAsset Unity** (`Assets/Fonts/Roboto-Regular SDF.asset`) généré depuis le .ttf via TextCore (TMP Font Asset format compatible UI Toolkit), puis référencé via `--unity-font-definition`.
- Backup : PanelSettings `textSettings` field pour fallback global.

**Steps** :
1. Editor script `Assets/Editor/CreateFontAssetTool.cs` qui génère un FontAsset SDF depuis Roboto-Regular.ttf via `UnityEngine.TextCore.Text.FontAsset.CreateFontAsset()` (API officielle Unity 6).
2. Crée `Assets/Fonts/Roboto-Regular SDF.asset` (dynamic atlas SDF mode).
3. Update toutes les USS (`HUD.uss`, `RadialMenu.uss`, `MenuLevelSelect.uss`) :
   - Remplacer `-unity-font: url(".../Roboto-Regular.ttf")` par `-unity-font-definition: url("project://database/Assets/Fonts/Roboto-Regular SDF.asset")`.
4. Test compile Unity batch + verify USS warnings/errors cleared.

### F.2 — Unity Localization package setup

**Due diligence** :
- Package `com.unity.localization` Unity verified, dernière stable Unity 6 : `1.5.x`.
- Compat WebGL : OK.
- Pin sur version stable.

**Steps** :
1. Add `"com.unity.localization": "1.5.5"` dans `Packages/manifest.json`.
2. Refresh Unity → installation.
3. Créer dossier `Assets/Resources/Localization/`.
4. Créer 2 locales : `en` (English, default), `fr` (Français).
5. Créer 5 String Tables (StringTableCollection) :
   - `UI.asset` — labels HUD, menus, settings
   - `Towers.asset` — tower names + desc
   - `Enemies.asset` — enemy names + flavor
   - `Levels.asset` — level briefings + names
   - `Achievements.asset` — Phase 5
6. Populate table `UI` avec 30+ keys minimum extraites du HUD existant.

### F.3 — L() static helper + integration HUD

**Steps** :
1. Créer `Assets/Scripts/UI/L.cs` static class avec :
   - `L.Get(string key, string table = "UI")` → fallback key si miss.
   - `L.Get(string key, params object[] args)` → format avec args.
   - `L.SetLocale(string code)` → change locale dynamique.
2. Refactor `HudController.cs` :
   - Remplacer textes durs ("GOLD", "WAVE", "HP", "Lancer la vague", etc.) par `L.Get("hud.gold_label")`.
   - Préserver API publique (Start/OnDestroy/Update, callbacks events).

### F.4 — SettingsRegistry + SettingsPanel UI

**Steps** :
1. Créer `Assets/Scripts/UI/SettingsRegistry.cs` (MonoSingleton C6 contract) :
   - Fields : MasterVolume, SFXVolume, MusicVolume, Muted, QualityLevel, VFXEnabled, ShakeEnabled, ColorblindMode, ReduceMotion, LargeText.
   - Save/Load PlayerPrefs.
   - Awake : Load + apply AudioController + QualitySettings.
2. Créer `Assets/UI/SettingsPanel.uxml` :
   - Panel modal centré.
   - Section Audio : 3 sliders (Master/SFX/Music) + Mute toggle.
   - Section Graphics : Quality dropdown (Mobile/Desktop/High) + VFX toggle + Shake toggle.
   - Section Accessibility : Colorblind toggle + ReduceMotion + LargeText.
   - Section Language : dropdown (English/Français).
   - Close button.
3. Créer `Assets/UI/SettingsPanel.uss` style.
4. Créer `Assets/Scripts/UI/SettingsPanelController.cs` :
   - Bind sliders → SettingsRegistry.
   - Bind Audio sliders → AudioController.SetMasterVolume/SetMuted (les autres pas implémentés dans AudioController actuel — fallback masterVolume sufficient pour Stage A).
   - Bind Language dropdown → L.SetLocale().

### F.5 — Tutorial UI (defer)

**Status** : Attendre que Axis D Content fournisse spec `docs/specs/levels/W1-1-tutorial-flow.md`.
- Non bloquant pour Stage A.
- Documenté ici comme placeholder.

---

## Spawning strategy

Le Sub-Opus exécute F1+F2+F3+F4 séquentiellement en autonomie (le worktree partage Unity Editor + Library, donc spawning multi-Sonnets en parallèle n'apporte rien — chaque sonnet attend MCP refresh).

**Ordre exécution** :
1. F.1 Font fix (priorité 1, débloque WebGL test)
2. F.4 SettingsRegistry + Panel (indépendant)
3. F.2 Localization package + tables (peut nécessiter user input UI Editor)
4. F.3 L.cs helper + HudController refactor (dépend F.2)

**QA-2 par commit** : per axe protocol (cf qa-gates.md).
**QA-3 pre-merge** : avant push axis/ux final.

---

## Constraints respectées

- Pas de touche aux scripts gameplay (Tower/Enemy/Castle/Wave/Level/Economy/Balance).
- HudController.cs : dans `Assets/Scripts/UI/`, donc ma zone. Préserver API publique : Start(), OnDestroy(), Update(), TryLaunchWave(), OnGoldChanged(), OnHPChanged(), OnWaveStart(), OnBreakStateChanged(), OnStateChanged().
- Due diligence package : `com.unity.localization` Unity verified, pin version stable, doc raison.
- AudioController API limitée : SetMasterVolume + SetMuted présents ; SetSFXVolume + SetMusicVolume manquants → SettingsRegistry stocke valeurs mais binding partiel. Note : extension Axis B à demander post-Stage A.

---

## Status tracking

| Tâche | Status | Commit |
|---|---|---|
| Plan évolutif | DONE | this commit |
| F.1 Font fix | TODO | — |
| F.4 SettingsRegistry + Panel | TODO | — |
| F.2 Localization setup | TODO | — |
| F.3 L.cs + HudController | TODO | — |
| F.5 Tutorial overlay | DEFER (Axis D dep) | — |
| QA-3 pre-merge | TODO | — |
| Push axis/ux | TODO | — |
| Rapport final | TODO | — |
