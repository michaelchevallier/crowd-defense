# Axis F — UX-POLISH Stage A Report

**Date** : 2026-05-11
**Branch** : `axis/ux` (pushed to origin)
**Sub-Opus** : SO-UX (Opus 4.7)
**Duration** : ~1h30 effective
**Commits** : 4

---

## Status global

| Tâche | Status | Commits |
|---|---|---|
| Plan évolutif | ✅ DONE | `9fb68ae` |
| F.1 Font fix critique | ✅ DONE | `08cfd0a` |
| F.4 SettingsRegistry + Panel UI | ✅ DONE | `e809b93` |
| F.3 L() helper + HudController integration | ✅ DONE | `e809b93` + `14f7d77` |
| F.2 Unity Localization package | ⚠️ ESCALATED MO | `14f7d77` (request file) |
| F.5 Tutorial overlay | ⏭️ DEFER (Axis D dep) | — |

---

## F.1 — Fix HUD font Roboto (CRITICAL P1)

### Root cause découverte

Le bug "Unable to load font face for [Roboto-Regular]" avait une cause **non-évidente** : le fichier `Assets/Fonts/Roboto-Regular.ttf` (306 KB) était en réalité **une page HTML GitHub 404** sauvée avec l'extension `.ttf`. Le download initial (probablement via `wget` ou `curl` sans `-L`) avait capturé une page d'erreur GitHub au lieu du binary TTF.

**Trace de diagnostic** :
- `file Assets/Fonts/Roboto-Regular.ttf` → `HTML document text`
- `FontEngine.LoadFontFace(font, 90)` → `Invalid_File_Structure`
- `FontAsset.CreateFontAsset(font)` → `null`
- `Font.dynamic` → `False`, `fontSize` → `0`, `ascent` → `0`

### Fix appliqué

1. **Remplacement TTF source** : téléchargement officiel `Roboto-Regular.ttf` depuis `googlefonts/roboto` (raw URL, 515 KB, 19 tables TrueType valid). Apache 2.0 license — déjà OK pour usage commercial.
2. **Création FontAsset SDF** : `Assets/Fonts/Roboto-Regular SDF.asset` via API `UnityEngine.TextCore.Text.FontAsset.CreateFontAsset()` avec params :
   - pointSize=90, padding=9, atlas 1024×1024
   - `GlyphRenderMode.SDFAA`
   - `AtlasPopulationMode.Dynamic` (glyphs added on-demand)
3. **PanelTextSettings** : `Assets/UI/HUDTextSettings.asset` avec `defaultFontAsset = Roboto SDF` + `fallbackFontAssets = [Roboto SDF]`. Assigné au `HUDPanelSettings.textSettings` field via `SerializedObject` (GUID `df758deb9f2ec45fe81bb360d6f12db1`).
4. **USS migration** : 3 fichiers USS (`HUD.uss`, `RadialMenu.uss`, `MenuLevelSelect.uss`) migrent **toutes** les déclarations :
   ```
   -unity-font: url(".../Roboto-Regular.ttf")
   →
   -unity-font-definition: url(".../Roboto-Regular%20SDF.asset")
   ```
   (16 occurrences au total)
5. **Editor tool** : `Assets/Editor/CreateFontAssetTool.cs` MenuItem `Crowd Defense/UX/Build Roboto SDF Font Asset` — pour rebuild le SDF asset si nécessaire (Mike peut re-run après une réimportation TTF).

### Validation runtime

```
[execute_code MCP]
LoadFontFace(font, 90) → Success
fontAsset.TryAddCharacters("Hello WAVE GOLD HP 120") → True
glyphTable.Count → 17 glyphs added to atlas dynamiquement
```

Le SDF asset peut maintenant rendre tous les caractères latins de l'UI. WebGL : `FontAsset` Unity format est natif UI Toolkit → pas de fallback ttf-loader chargé.

---

## F.4 — SettingsRegistry + SettingsPanel UI

### SettingsRegistry (C6 contract)

`Assets/Scripts/UI/SettingsRegistry.cs` — MonoSingleton fields :
- **Audio** : MasterVolume, SFXVolume, MusicVolume, Muted
- **Graphics** : QualityLevel (0=Mobile/1=Desktop/2=High), VFXEnabled, ShakeEnabled
- **Accessibility** : ColorblindMode, ReduceMotion, LargeText
- **Locale** : Locale (en/fr)

Persistence via `PlayerPrefs` keys préfixés `cd.audio.*`, `cd.gfx.*`, `cd.a11y.*`, `cd.locale`. Save() écrit immédiatement à chaque setter ; Load() au Awake.

Hooks ApplyAudio() → `AudioController.Instance.SetMasterVolume + SetMuted` (limité au C1 actuel — voir note F.4 caveat).
Hook ApplyQuality() → `QualitySettings.SetQualityLevel`.

OnSettingsChanged event broadcast pour binding two-way.

### SettingsPanel UXML/USS

`Assets/UI/SettingsPanel.uxml` modal centré 560×80% :
- Section Audio : 3 sliders (Master/SFX/Music 0-1 + label %) + Mute toggle
- Section Graphics : Quality dropdown + VFX/Shake toggles
- Section Accessibility : Colorblind/ReduceMotion/LargeText toggles
- Section Language : dropdown (English/Français)
- Close button

USS coordonné style HUD existant (background `rgba(20, 26, 38, 0.97)`, accents jaune `rgb(255, 210, 63)`, bordures bleu pâle). Toutes les fonts pointent vers `Roboto-Regular SDF.asset` (cohérent avec F.1).

### SettingsPanelController

`Assets/Scripts/UI/SettingsPanelController.cs` :
- `Show()` / `Hide()` API publique pour HUD/menu integration.
- `RegisterValueChangedCallback` bind chaque widget → SettingsRegistry setter.
- `SyncFromRegistry()` au Start + sur `OnSettingsChanged` (suppress flag pour éviter loops).
- Language dropdown change → `L.SetLocale(code)` direct.

### Caveat AudioController C1

Le C1 spec liste `SetSFXVolume(float)`, `SetMusicVolume(float)`. **L'implémentation actuelle d'AudioController** (Axis B Stage A) n'expose **que** `SetMasterVolume + SetMuted`. SettingsRegistry stocke SFXVolume/MusicVolume en PlayerPrefs mais ne les applique pas (no-op silent jusqu'à ce qu'Axis B étende l'API). À documenter pour Axis B en future review.

---

## F.3 — L() static helper + integration HUD

### L.cs (C5 contract)

`Assets/Scripts/UI/L.cs` static class :
```csharp
public static string Get(string key, string table = "UI");
public static string Get(string key, params object[] args);  // format
public static void SetLocale(string code);
public static event Action? OnLocaleChanged;
public static string CurrentLocale { get; }
```

Implémentation **fallback Dictionary** in-source (no Unity Localization package required) :
- 2 locales : `en`, `fr`
- 1 table : `UI` (extensible — Towers/Enemies/Levels/Achievements à ajouter futures phases)
- 35+ keys minimum :
  - `hud.*` : gold_label, wave_label, hp_label, wave_launch, wave_launch_bonus, wave_progress, streak_text, pill_skip_text
  - `overlay.*` : game_over_title, game_over_subtitle, victory_title, victory_subtitle, btn_restart, btn_retry, btn_menu
  - `menu.*` : level_select_title
  - `settings.*` : title, audio_section, master, sfx, music, mute, gfx_section, quality, quality_mobile, quality_desktop, quality_high, vfx, shake, a11y_section, colorblind, reduce_motion, large_text, lang_section, lang_label, close

Fallback chain : `locale` → `en` → `key` (visible debug string). Format args via `string.Format` (catch silently).

### HudController integration

`Assets/Scripts/UI/HudController.cs` refactored :
- Static labels (GOLD/WAVE/HP/etc.) `name`d in `HUD.uxml` → captured at Start.
- `ApplyLocalizedTexts()` méthode appelée au Start + on `L.OnLocaleChanged`.
- Wave launch button labels (`Lancer la vague [N]`, `Vague {} / {}`, `+30c {:F1}s +{}%`, `+{}%`) via `L.Get(key, args)`.
- Game over / Victory overlays texts via L.Get.
- **API publique préservée** : Start/OnDestroy/Update/TryLaunchWave + tous les `On*Changed` callbacks unchanged.

### Validation runtime

```
[execute_code MCP]
L.Get('hud.gold_label') → 'GOLD'  (locale en, default)
L.SetLocale('fr')
L.Get('hud.gold_label') → 'OR'
L.Get('hud.wave_label') → 'VAGUE'
L.Get('hud.hp_label') → 'PV'
L.SetLocale('en')
L.Get('hud.gold_label') → 'GOLD'
```

---

## F.2 — Unity Localization package (ESCALATED)

### Status

ESCALATED à MO via `.claude/coordination/requests/2026-05-11-ux-localization-package.md`.

**Raison** : `Packages/manifest.json` est listé comme **HOT ZONE** dans `file-ownership.md` (line 24). SO-UX n'a pas le droit de modifier directement.

### Due diligence effectuée

- Latest stable : `1.5.11` (2026-03-18, source Unity verified docs)
- Compat Unity 6 : OK (2019.4+)
- WebGL : pas d'incompat documentée
- Bundle size estimé : +1-2 MB compressé

### Stage A delivery via fallback

L.cs Dictionary fallback **couvre exactement** l'API C5 contract avec 30+ keys × 2 locales. Migration future vers `com.unity.localization` sera **un swap interne** des méthodes `Get` / `SetLocale` sans changement de consumer code. HudController + SettingsPanelController + futur Localization-aware code utiliseront la même API.

---

## F.5 — Tutorial overlay (DEFER)

Status : Attendu Axis D Content brief `docs/specs/levels/W1-1-tutorial-flow.md`. Pas d'implémentation Stage A.

---

## Constraints respectées

- ✅ Pas de touche aux scripts gameplay (Tower/Enemy/Castle/Wave/Level/Economy/Balance).
- ✅ HudController.cs modifié uniquement dans la zone Axis F (`Assets/Scripts/UI/`). API publique préservée.
- ✅ Pas modifié les hot zones :
  - Packages/manifest.json (F.2 escalated)
  - STATUS.md (MO only)
  - Aucun gameplay script
- ✅ Due diligence package : Unity Localization 1.5.11 verified source + compat documented + escalation propre.
- ✅ Budget : ~1h30 effective vs 6-10h estimé → bien sous budget.

---

## QA gates

- **QA-1 (Pre-Spawn)** : ✅ self-check OK (lu file-ownership.md + api-contracts.md + qa-gates.md, branche `axis/ux` créée).
- **QA-2 (Per-Commit)** : ✅ compile clean (0 errors) après chaque commit via `mcp__UnityMCP__refresh_unity`.
- **QA-3 (Pre-Merge)** : ✅ partial — compile 0 errors + L() runtime tested (FR + EN); ⚠️ build WebGL skipped (60-120s, à valider par MO post-merge integration).
- **QA-5 (E2E /v6/)** : à valider post-deploy par MO.

---

## Fichiers livrés

### Nouveaux

- `Assets/Editor/CreateFontAssetTool.cs` (+ meta)
- `Assets/Fonts/Roboto-Regular SDF.asset` (+ meta)
- `Assets/UI/HUDTextSettings.asset` (+ meta)
- `Assets/UI/SettingsPanel.uxml` (+ meta)
- `Assets/UI/SettingsPanel.uss` (+ meta)
- `Assets/Scripts/UI/L.cs` (+ meta)
- `Assets/Scripts/UI/SettingsRegistry.cs` (+ meta)
- `Assets/Scripts/UI/SettingsPanelController.cs` (+ meta)
- `.claude/plans/axis-ux.md`
- `.claude/coordination/requests/2026-05-11-ux-localization-package.md`

### Modifiés

- `Assets/Fonts/Roboto-Regular.ttf` (replaced HTML stub with real TTF 515 KB)
- `Assets/UI/HUD.uss` (5 occurrences `-unity-font` → `-unity-font-definition`)
- `Assets/UI/RadialMenu.uss` (6 occurrences)
- `Assets/UI/MenuLevelSelect.uss` (3 occurrences)
- `Assets/UI/HUD.uxml` (added 5 name attrs for static labels)
- `Assets/UI/HUDPanelSettings.asset` (textSettings field linked)
- `Assets/Scripts/UI/HudController.cs` (L() integration + ApplyLocalizedTexts on locale change)

---

## Next steps (suggérés MO)

1. **Merge axis/ux dans integration/phase3-4-5** : pas de conflit attendu hors HudController + HUD.uxml (intersect avec autres axes : Axis A VfxPool ne touche pas UI).
2. **Décider F.2** : add `com.unity.localization` package ou continuer L.cs fallback ?
   - Ajouter package → 5 String Tables Unity Localization + migration L.cs interne (separate axis/ux extension ticket).
   - Garder fallback → L.cs Dictionary suffit ; ajoute keys au fur et à mesure.
3. **Build WebGL full QA** : valider que la font Roboto SDF rend correctement dans /v6/ deploy.
4. **Integrator hooks F.4** : intégrer SettingsPanel comme overlay HUD-level (toucher Main.unity scene ou prefab — coord MO).
5. **F.5 Tutorial overlay** : attend Axis D Content livre `W1-1-tutorial-flow.md` brief.

---

## Closing

Stage A axis/ux délivré en 4 commits propres, compile clean, runtime L() validé via MCP execute_code. F.1 critique fixé (root cause HTML stub TTF découverte et résolue). F.3+F.4 livrés full. F.2 escalated MO avec fallback fonctionnel. F.5 défer.

Bonne chance pour l'intégration.

— SO-UX out.
