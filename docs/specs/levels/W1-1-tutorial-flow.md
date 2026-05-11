# W1-1 Tutorial Flow — Onboarding First-Time User

**Date** : 2026-05-11
**Spec author** : SO-CONTENT (Axis CONTENT-LEVELS)
**Implementation owner** : Axis F UX (UI Toolkit + Localization)
**Target level** : W1-1 (`world1-1` / `Plaine — 1`)
**Audience** : nouveau joueur (zéro connaissance TD/Crowd Defense), zéro skill.

---

## 1. Contexte

W1-1 est **le seul level conçu pour onboarding**. Tous les autres levels (W1-2..W10-8) assument que le joueur connaît :
- Le bouton "Lancer la vague (N)" et le skip bonus 5s
- La pose de tour via toolbar
- L'upgrade L2/L3 via clic tour
- Le coût gold + la lecture du HUD

**Constants W1-1** :
- `castleHP: 200` (D1-04 §1 Q14 floor onboarding permissif)
- `startCoins: 120`
- `waves: 4` (35 + 76 + 87 + 90 = 288 enemies total, fidèle Phaser source)
- Pas de magnet possible (`magnet.unlockWorld = 4`)
- Pas de mob air (Flyer arrive en W2)

---

## 2. Hint chains — séquence pédagogique

### Phase 0 — Avant wave 1

**Trigger** : `LevelRunner.OnLevelLoaded()` + `waveIndex == 0` + `!waveActive` + `playerTowerCount == 0`

**Hint affiché** (toast en bas centre, 4s puis fade) :
> "Place ta première tour en cliquant sur la **toolbar** en bas, puis sur la **map**."

**Localization key** : `tutorial.w1_1.hint_place_tower`

**État cible** : joueur a posé au moins 1 tour (Archer suggéré, cost 30¢ = abordable).

### Phase 1 — Tour posée, wave non lancée

**Trigger** : `playerTowerCount >= 1` + `waveIndex == 0` + `!waveActive`

**Hint affiché** (pill au-dessus du bouton "Lancer la vague", 3s) :
> "Quand tu es prêt, lance la **vague (N)**. Skip dans les 5s = +30¢ bonus."

**Localization key** : `tutorial.w1_1.hint_launch_wave`

**Sub-hint optionnel** (si joueur attend > 15s sans cliquer) :
> "Astuce : appuie sur **N** pour lancer la vague rapidement."

**Localization key** : `tutorial.w1_1.hint_n_shortcut`

### Phase 2 — Première wave en cours

**Trigger** : `waveActive` + `waveIndex == 0` + `firstKill == false`

**Pas de hint actif** (le joueur observe). Juste les feedbacks visuels (juice + audio + VFX).

### Phase 3 — Wave 1 cleared, transition vers wave 2

**Trigger** : `OnWaveCleared(0)` + `coins >= 60` (upgrade L1→L2 archer)

**Hint affiché** (pill au-dessus de la tour posée + arrow indicator) :
> "Clique sur ta tour pour l'**améliorer**."

**Localization key** : `tutorial.w1_1.hint_upgrade_tower`

**État cible** : joueur clique tour → ouvre radial menu → choisit Upgrade.

### Phase 4 — Wave 2 préparation

**Trigger** : `waveIndex == 1` + `!waveActive` + `playerTowerCount >= 2 || hasUpgradedTower`

**Pas de hint actif** (le joueur a compris la boucle).

### Phase 5 — Wave 2 leak ou difficulté

**Trigger optionnel** : `castleHP < 150` (lost > 50 HP)

**Hint affiché** (toast warning) :
> "Pose **plus de tours** ou **améliore** les existantes. Le château est en danger."

**Localization key** : `tutorial.w1_1.hint_castle_danger`

### Phase 6 — Wave 3, présentation synergies (preview)

**Trigger** : `waveIndex == 2` + `!waveActive`

**Hint affiché** (info bubble, dismissible) :
> "Bientôt : des **synergies** entre tours (cf manuel)."

**Localization key** : `tutorial.w1_1.hint_synergies_preview`

**Note** : pour Phase 5 implémentation, ce hint pointe vers un manuel ou tutoriel séparé. En W1-1, on ne fait que **teaser**.

### Phase 7 — Wave 4 (finale W1-1)

**Trigger** : `waveIndex == 3` + `waveActive`

**Pas de hint actif** (gameplay culminant).

### Phase 8 — Level cleared

**Trigger** : `LevelRunner.OnLevelComplete()`

**Hint affiché** (modal final) :
> "**Plaine — 1 vaincue !** Tu maîtrises les bases. Prochain level : W1-2."

**Localization key** : `tutorial.w1_1.hint_level_cleared`

---

## 3. Spec d'implémentation (Axis F UX)

### 3.1 Trigger system

Les hints sont déclenchés par un nouveau composant `TutorialController` (MonoBehaviour) attaché à un GameObject dans la scène Main.

```csharp
namespace CrowdDefense.UI;

public class TutorialController : MonoBehaviour
{
    [SerializeField] private LevelData? targetLevel;  // ex: W1-1.asset
    [SerializeField] private bool persistDismissedHints = true;  // PlayerPrefs

    private readonly List<TutorialHint> _hints = new();
    
    // À appeler depuis LevelRunner.OnLevelLoaded()
    public void Init(LevelData level);
    
    // À appeler depuis WaveManager.OnWaveStart() / OnWaveCleared()
    public void OnWaveEvent(WaveEvent ev);
    
    // À appeler depuis Tower.OnPlaced()
    public void OnTowerEvent(TowerEvent ev);
}

public class TutorialHint
{
    public string LocalizationKey;
    public HintTrigger Trigger;
    public HintPresentation Presentation;  // Toast / Pill / Modal / InfoBubble
    public bool DismissOnce;  // si true, marqué PlayerPrefs
}
```

### 3.2 UI Toolkit binding

- Toast : réutilise `.toast` style existant (D1-02 spec) + variant `.toast-tutorial` (bordure dorée).
- Pill : positionné dynamiquement near le bouton "Lancer la vague" (D1-02 §wave-launch-pill).
- Modal : full-screen overlay avec backdrop + bouton "Continue".
- Arrow indicator : SVG sprite + animation pulse 1.2s (pour pointer une tour posée par exemple).

### 3.3 Localization

Toutes les strings via `L.Get("tutorial.w1_1.hint_*", "UI")` (C5 contract). Table `UI` accumule les 8 nouvelles keys :
- `tutorial.w1_1.hint_place_tower`
- `tutorial.w1_1.hint_launch_wave`
- `tutorial.w1_1.hint_n_shortcut`
- `tutorial.w1_1.hint_upgrade_tower`
- `tutorial.w1_1.hint_castle_danger`
- `tutorial.w1_1.hint_synergies_preview`
- `tutorial.w1_1.hint_level_cleared`
- (français + anglais minimum, autres locales Phase 5)

### 3.4 Persistence dismiss

Si le joueur a dismiss un hint W1-1 (via X close ou tap), enregistrer dans PlayerPrefs `tutorial.w1_1.dismissed.{hint_key} = 1`. Au re-load W1-1, ne pas re-afficher (sauf si Settings → "Reset tutorial").

### 3.5 Skippable

Bouton "Skip Tutorial" disponible dans Settings → écrit `tutorial.w1_1.skipAll = 1`. Permet aux dev/Mike testeurs de re-jouer W1-1 sans hints.

---

## 4. Test plan

### 4.1 Manuel (Mike playtest)

1. Reset save : `SaveSystem.ResetProgress()` via dev menu.
2. Charger W1-1.
3. Vérifier hint Phase 0 apparaît à load (4s puis fade).
4. Poser une tour Archer (toolbar → click map).
5. Vérifier hint Phase 1 apparaît (pill).
6. Attendre 20s sans cliquer → vérifier sub-hint Phase 1 (N shortcut).
7. Cliquer "Lancer la vague" → vérifier wave 1 démarre, pas de hint actif (Phase 2).
8. Attendre wave 1 cleared → vérifier hint Phase 3 (upgrade) si coins ≥ 60.
9. Upgrade la tour → vérifier hint disparaît.
10. Continuer wave 2-3-4 normalement → vérifier hint Phase 6 apparaît (synergies preview) au break wave 3.
11. Compléter W1-1 → vérifier modal Phase 8.
12. Reload W1-1 → vérifier hints **ne s'affichent plus** (PlayerPrefs dismissed).
13. Settings → "Reset tutorial" → reload W1-1 → vérifier hints réapparaissent.

### 4.2 Auto-QA (Axis G QA)

Test scenario `.claude/qa/scenarios/sprint-tutorial/w1_1_tutorial.mjs` :
- Hard assert : `TutorialController` instance exists on scene
- Hard assert : `L.Get("tutorial.w1_1.hint_place_tower")` returns non-empty string
- Soft assert (LLM judge) : screenshot of toast Phase 0 → "le hint est lisible, visible, non bloquant"

---

## 5. Edge cases

- **Joueur place 2 tours avant Phase 1 trigger** : OK, hint Phase 1 s'affiche quand même (trigger checked sur `playerTowerCount >= 1`).
- **Joueur perd W1-1 (game over)** : modal "Tu as perdu, recommencer ?" + reset hints state pour next attempt.
- **Joueur skip wave dans la fenêtre 5s sans avoir compris** : OK, le streak bonus s'applique, mais Phase 3 hint démarre après wave 1 cleared comme prévu.
- **W1-1 disponible via "Replay" depuis menu après campaign clear** : `tutorial.w1_1.skipAll = 1` auto-set pour les replays.

---

## 6. Coordination Axis F UX

- Spec gelée 2026-05-11 (cette spec).
- Implementation : feature-dev Sonnet ticket UX-TUT-01 (à créer par SO-UX).
- Files à créer/modifier :
  - `Assets/Scripts/UI/TutorialController.cs` (NEW, owned UX)
  - `Assets/Resources/Localization/UI.po` (NEW keys ajoutés)
  - `Assets/UI/Tutorial/*.uxml` (NEW templates)
  - `Assets/UI/Tutorial/*.uss` (NEW styles)
- Hooks dans hot zones (LevelRunner.OnLevelLoaded, WaveManager.OnWaveStart, etc.) = **Integrator** (Stage B), pas axis F directement.

---

## 7. Décisions ouvertes (Mike arbitrage)

1. **Skip W1-1 tutorial total** : faut-il auto-skip si `SaveSystem.IsLevelCleared("world1-1") == true` ? **Recommandation** : oui, pour le replay.
2. **Hint Phase 6 synergies preview** : faut-il l'afficher dès W1-1 (early teaser) ou attendre W3+ ? **Recommandation** : W1-1 teaser court (info bubble dismissible), pas de pression.
3. **Modal Phase 8 "level cleared"** : conserver ou laisser le LevelComplete UI standard suffire ? **Recommandation** : ajouter le texte "Tu maîtrises les bases" via un panneau dédié W1-1 spécifique.
