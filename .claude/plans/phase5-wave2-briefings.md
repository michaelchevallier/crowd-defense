# Phase 5 Wave 2 — briefings ready-to-dispatch (15 P1)

> Source : `.claude/plans/phase5-parity-v4-master.md` §4.2.
> Dispatch dès que Wave 1 atteint 75% (6/8 P0 mergés).
> Groupements pour éviter conflits HUD.uxml / Toolbar.

## Groupement (8 agents simultanés max)

| Agent | Tickets fusionnés | Zone | Conflit avec |
|---|---|---|---|
| A1 | P1-LVL-3 Boss Rush mode | Levels + LevelLoader | RunMode (P0-UI-1) |
| A2 | P1-LVL-4 Refondre 3 multi-castle (W5-8, W9-8, W10-8) | Levels SO uniquement | - |
| A3 | P1-LVL-5 Désync worlds 10 + P1-GP-1 cleanup 2× Daily | WorldMapController + Daily.cs | UI-5 (settled) |
| A4 | P1-GP-2 Daily streak counter + P1-EN-1 popup +1/BOSS DOWN | DailyChallenge + Castle.cs + Enemy.cs | hot zone |
| A5 | P1-UI-1 top-bar buttons + P1-UI-2 gems pill HUD | HUD.uxml top-bar | - |
| A6 | P1-UI-3 behavior badges + P1-UI-4 synergy tooltips + P1-UI-5 locked state toolbar | Toolbar.uxml/Controller | - |
| A7 | P1-UI-6 briefing modal + P1-UI-7 pause settings/help | UXMLs overlay | - |
| A8 | P1-UI-8 support mode + P1-AST-1 skybox wiring verify | GameSession + SkyboxController | hot zone |

## Briefing A1 — P1-LVL-3 Boss Rush mode

Type : feature-dev. Effort 3-5 commits.

Porter le Boss Rush mode V4 (`milan project/src-v3/data/boss_rush.js` + `milan project/src-v3/ui/BossRushMenu.js` si présent). Mode : 9 boss back-to-back (W1-8 → W9-8), pas d'économie entre fights, castleHP shared continu, victory final = mode unlock + gems reward.

Fichiers à créer :
- `/Users/mike/Work/crowd-defense/Assets/Scripts/Systems/BossRushMode.cs` — manager state machine.
- `/Users/mike/Work/crowd-defense/Assets/Scripts/Data/BossRushDef.cs` — chain of W*-8 levels.

Fichiers à modifier :
- `Assets/Scripts/Systems/LevelLoader.cs` — ajouter `LoadBossRushNext()`.
- `Assets/Scripts/UI/WorldMapController.cs` — re-enable `tile-boss-rush` (disabled par P0-UI-5).

Workflow git worktree : `git pull --rebase origin main` avant tout. Commits atomiques. Push origin HEAD.

## Briefing A2 — P1-LVL-4 Refondre 3 multi-castle levels

Type : level-designer (ou feature-dev fallback). Effort 3 commits.

Refondre `world5-8.asset`, `world9-8.asset`, `world10-8.asset` (présentement multi-château ou multi-portal mal placés) en **mono-château + 4 portals convergents** (cf décision Q9-Q11 §3.1 du plan).

Fichiers à modifier : 3 `.asset` levels dans `Assets/ScriptableObjects/Levels/world5-8.asset`, `world9-8.asset`, `world10-8.asset`. Édit YAML direct ou via Editor builder.

Critères : 1 seul castle, 4 portails (chacun ≤ 5 cellules du bord), paths convergents 22×16 grid PCWL DR T B.

## Briefing A3 — P1-LVL-5 + P1-GP-1

P1-LVL-5 : étendre `WorldMapController` à 10 mondes (au lieu de 8). Champ `WorldCount = 10`, ajout 2 worldButton stubs si absents dans WorldMap.uxml.

P1-GP-1 : cleanup `Assets/Scripts/Systems/Daily.cs` (orphelin) — supprime ou archive en `.bak`. Le système actif est `DailyChallenge.cs`.

## Briefing A4 — P1-GP-2 + P1-EN-1

P1-GP-2 : ajouter daily streak counter dans `DailyChallenge.cs` (champ + persistence `PlayerPrefs.SetInt("daily_streak", ...)` + decrement si miss day, increment si win).

P1-EN-1 : popup `+1` (vert) sur kill enemy + `BOSS DOWN!` (or) sur kill boss + ring VFX boss death. Hook dans `Enemy.cs` OnDeath via JuiceFX.PopText (signature à confirmer via grep).

## Briefing A5 — P1-UI-1 + P1-UI-2

P1-UI-1 : ajouter 3 boutons top-bar HUD (Shop, Map, Encyclopedia déjà présent en P0-UI-3) — Shop+Map à ajouter, navigation depuis HUD vers ces écrans.

P1-UI-2 : pill `💎 N gems` à droite de la gold pill HUD. Bind `SaveSystem.GetGems()` ou registry équivalent.

Fichiers : `Assets/UI/HUD.uxml`, `Assets/UI/HUD.uss`, `Assets/Scripts/UI/HudController.cs`. Pull rebase d'abord.

## Briefing A6 — P1-UI-3 + P1-UI-4 + P1-UI-5

P1-UI-3 : Behavior badges sur tower preview (explosion/perce/slow/aura) — chips sous le nom de la tour avec icônes texte.

P1-UI-4 : Synergy tooltips au hover tower cell — utiliser `TooltipManager` si existant ou `RegisterCallback<MouseEnterEvent>`.

P1-UI-5 : Locked state toolbar — toolbar-cell--locked CSS (parallèle à --forbidden de P0-LVL-1).

Fichiers : `Assets/UI/TowerToolbar.uxml`, `TowerToolbar.uss`, `Assets/Scripts/UI/TowerToolbarController.cs`.

## Briefing A7 — P1-UI-6 + P1-UI-7

P1-UI-6 : Briefing modal pré-level — title (level name) + briefing text + countdown 3-2-1-GO avant spawn first wave. Hook OnLevelStart.

P1-UI-7 : Pause menu sub-settings + help button (déjà bouton pause existant — ajouter Sub-panel ou navigation Settings + Help dialogs).

Fichiers : nouveau `Assets/UI/BriefingModal.uxml`, modif `Assets/UI/PauseMenu.uxml` + controllers.

## Briefing A8 — P1-UI-8 + P1-AST-1

P1-UI-8 : Support mode — détecte 2 défaites consécutives sur même niveau → propose dialog "Aide automatique : +15% castleHP, +20% gold, -15% mob HP. Activer ?". Stocke `support_mode_active` SaveSystem.

P1-AST-1 : Verify skybox V8I wiring 10 slots Inspector SkyboxController. Re-check que les 10 skybox materials sont assignés (cf commit `735abdc4` skybox apply on Awake).

Fichiers : `Assets/Scripts/Systems/GameSession.cs` (support mode counter), `Assets/Scripts/Systems/SkyboxController.cs` (verify).

## Dispatch pattern Wave 2

À l'atteinte de 75% Wave 1 (6/8 P0 mergés), spawn 8 agents en parallèle dans 1 message Agent tool calls. Tous `isolation: "worktree"`, `run_in_background: true`. Sub-type :
- feature-dev : A1, A4, A5, A6, A7, A8
- bug-fixer : A3 (cleanup), A2 (level data edit — feature-dev fallback)

Verification post-merge : `git log --oneline origin/main -30` doit montrer ~25-30 nouveaux commits Wave 2.
