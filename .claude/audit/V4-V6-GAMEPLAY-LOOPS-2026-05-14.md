# Audit V4 ↔ V6 — Gameplay Loops

Date : 2026-05-14
Auteur : Opus 4.7
Scope : tous les gameplay loops V4 (Phaser, `src-v2/`) vs V6 (Unity 6 `crowd-defense/`)
Méthode : lecture statique du code, pas de runtime test.

V4 source : `/Users/mike/Work/milan project/src-v2/`
V6 source : `/Users/mike/Work/crowd-defense/Assets/`

---

## TL;DR

V6 a une parité fonctionnelle solide sur le **cœur** (boot, menu, worldmap, level loop, save, audio, pause, restart, autopause, mute, daily, endless, achievements, cutscene, skins, treasure, lifetime stats, wave launch button).

V6 est **manquant** sur les **modes optionnels** : Carnaval, Arène Boss, Mini-jeux Foire, Coop 2P, choix narratifs branchés, tickets-currency, trophy +5¢ start bonus, webfonts Bangers/Fredoka, daily streak counter.

Comptage : **18 loops audités**, **9 PARITY** (avec ou sans détail mineur), **3 PARTIAL**, **6 ABSENT**.

---

## Format

Pour chaque loop : `STATUS` + détails V4 + détails V6 + verdict.

- **PARITY** : feature présente, comportement équivalent.
- **PARTIAL** : feature présente, mais shape différente ou parts manquantes.
- **ABSENT** : feature absente côté V6.
- **DIVERGENT** : V6 a son propre design différent et incompatible.

---

## 1. Boot → Menu — **PARTIAL**

### V4
- `BootScene.js` (152 lignes) : splash gradient sombre→orange, étoiles, titre "MILAN PARK DEFENSE" (Bangers 100px) + sous-titre, tile spinner animée, bouton "▶ JOUER", trigger Space ou click → CampaignMenuScene.
- Footer affiche `"Phaser 4 • CC0 • v1.0 — Click ou ESPACE pour jouer"` (version hardcodée).
- Init audio + MusicManager `play("menu")`.
- Volume slider draggable persisté en `parkdef:volume`.

### V6
- 2 scènes : `Loader.unity` (LoaderToMenu.cs, 30s gate) + `Menu.unity` (MenuController).
- `SplashScreen.cs` (124 lignes, RuntimeInitializeOnLoadMethod) : runtime canvas "CROWD DEFENSE" + tagline "Tower Defense Game", fade-in/hold/fade-out → SceneManager.LoadScene("Menu"). Skip 2e boot via `skip_splash_v1`.
- **Pas de version/build hash affiché** (V4 montrait `v1.0`).
- **Pas de volume slider** sur splash (V6 a un settings panel séparé).

### Verdict
PARTIAL — boot fonctionnel, mais le branding (titre, fonts, version) est plus pauvre côté V6. Recommandation P2 : afficher `Application.version` + commit short hash en bas du splash.

---

## 2. Menu Principal — **PARITY**

### V4
- `CampaignMenuScene.js` (534 lignes) : titre Bangers, étoiles totales `★ N/180`, 10 lignes worlds × 6 cells, boutons Stats / Foire / Aide / Trophées + Endless / Carnaval / Arène-Boss / Daily.

### V6
- `MenuController.cs` (175 lignes) : 5 boutons (`btn-continue`, `btn-newrun`, `btn-settings`, `btn-quit`, `btn-talents`).
- Continue = SaveSlotController panel (3 slots A/B/C — V6-exclusif).
- New Run → `LevelLoader.GoToWorldMap()` → WorldMapController.
- Demo mode auto-trigger après 60s inactivité (V6-exclusif).
- StatsLifetimePanel séparé (équivalent V4 StatsScene).

### Verdict
PARITY différente structure mais entrées équivalentes :
- V4 = "Menu = HUB campagne" (worlds visibles directement)
- V6 = "Menu = lobby" + WorldMap séparé.
Saves slots est un PLUS V6.

---

## 3. Campagne 10 mondes × 6 levels (V4) vs 8×10 (V6) — **PARTIAL**

### V4
- 10 mondes nommés (Plein Été, Crépuscule, Tempête, Volcan, Apocalypse, Foire Magique, Espace, Sous-Marin, Médiéval, Cyberpunk) × 6 levels = **60 levels**.
- Codes `worldN-K` ou `N.K` (e.g. `1.1`, `6.6`, `10.6`).
- Stars unlock séquentiel.

### V6
- `WorldMapController.cs` : **WorldCount = 8, LevelsPerWorld = 10 → 80 levels théoriques.**
- Themes : Plaine, Desert, Ocean, Volcan, Foret, Glace, Marais, Nebula.
- Fichiers SO physiques sur disque : `Assets/ScriptableObjects/Levels/W1-1.asset` à `W10-9.asset` → **90 levels SO** (10×9, pas 8×10).
- Mismatch : code = 8×10, données = 10×9. La WorldMap affiche `W1`..`W8` mais lis `world1-1`..`world8-10`. Les SOs `W9-*` et `W10-*` sont sur disque mais non exposés au worldmap.

### Verdict
PARTIAL :
- Comptage différent (80 vs 60), mais c'est par design (V6 est la refonte).
- **DESYNC interne V6** entre code (8×10) et SO disponibles (10×9). 
- Naming : V6 fichier `W1-1.asset` vs code lookup `world1-1` — vérifier que `LevelRegistry.FindById("world1-1")` map bien au SO `W1-1` (probable via Id field interne du SO).
- Theme names très différents (V4 = mondes thématiques riches "Apocalypse / Cyberpunk", V6 = biomes basiques "Plaine / Glace"). Décision design Mike.

---

## 4. Mode Carnaval (5 levels conveyor sans économie) — **ABSENT**

### V4
- `CampaignMenuScene.drawCarnivalButton` + ids `c.1`..`c.5`, `level.mode === "conveyor"` dans `LevelScene.js:273`.
- `ui/ConveyorBelt.js` remplace la Toolbar normale, pas d'économie.
- 5 levels dans data, accessible depuis Menu.

### V6
- Aucun fichier `Carnaval` / `Conveyor` / mode-spécifique.
- `grep "Carnival"` + `"Conveyor"` → 0 hits.

### Verdict
ABSENT — feature complète manquante. P1 si Mike veut récupérer (~3-5 jours dev car nécessite UI conveyor + LevelMode enum + LevelData flag).

---

## 5. Mode Arène des Boss (3 vagues 3 boss) — **ABSENT**

### V4
- `CampaignMenuScene.drawBossArenaButton` + level id `boss-arena`, unlock après `5.6`.
- Boss types : magicboss / lavaqueen / carnivalboss multi-phases.
- Pas de UI special, juste un LevelData avec 3 waves.

### V6
- 0 hits sur `boss-arena` ou `BossArena` ou `arena` dans Scripts/.
- `BossSystem.cs` existe (bosses individuels), mais pas de mode "3 boss en série".

### Verdict
ABSENT — facile à recréer (3-wave LevelData avec 3 boss enemies). P1.

---

## 6. Défi du Jour (seed-by-date, streak, 1 essai/jour) — **PARTIAL**

### V4 (`src-v2/systems/Daily.js`, 100 lignes)
- `dateSeed` = YYYYMMDD → mulberry32 RNG.
- 4 waves générées avec pools de visitors easy/mid/hard.
- 5 tiles aléatoires + `coin`+`lava` obligatoires.
- `recordDaily(stars, killed, escaped)`, `getDailyStreak()` (streak consécutif jours).
- `hasPlayedToday()` bloque rejouer.
- Stocké dans `save.daily[YYYY-MM-DD] = { stars, killed, escaped, ts }`.
- Trigger trophée `daily_streak_3`.

### V6
- 2 systèmes parallèles (concurrents !) :
  - `Daily.cs` (160 lignes) : porte fidèlement V5 Daily.js, génère `DailyLevelSpec` avec 5 waves de pools easy/mid/hard/boss. Mulberry32 identique. **Streak NON implémenté.**
  - `DailyChallenge.cs` (119 lignes) : système alternatif, jour-de-l'année (`DayOfYear`) au lieu de YYYYMMDD, génère un défi sur un niveau campagne existant (1-10) avec un modificateur (NoFrost / NoMage / NoArcher / HalfGold / NoPerks) + HP multiplier. Reward 500 pieces. `HasCompletedToday()`. **Pas de streak.**
- `DailyChallengeModal.cs` côté UI utilise `DailyChallenge` (pas `Daily.cs`).
- `Daily.cs` semble orphelin / non câblé (à vérifier).

### Verdict
PARTIAL — deux implémentations, ambiguïté sur celle active. La V6 DailyChallenge est conceptuellement différente (modificateur sur level existant) vs V4 (level entièrement procédural). Streak counter ABSENT. P1 : décider quel système retenir, brancher correctement, ajouter streak counter.

---

## 7. Cutscenes branchées (choix narratifs sauvegardés) — **ABSENT**

### V4 (`CutsceneScene.js`, 184 lignes + `data/cutscenes.js`)
- 6 cutscenes ASCII avec art + panels textes.
- 3 cutscenes ont `choices` array (worlds 4 / 7 / 10) :
  - W4 : `save_kids` (+startCoins) vs `fire_all` (+kill bonus per visitor)
  - W7 : `diplomacy` (cooldown reduction) vs `annihilation`
  - W10 : `human` vs `machine` (toolbar cooldown × 0.75 si machine)
- Sauvegardé dans `save.narrativeChoices[worldId] = choiceId`.
- Trophée `narrative_choice` débloqué dès le 1er choix.
- Stats screen affiche les voies empruntées (cf. `StatsScene.js:88-105`).
- Effet appliqué via `LevelScene._narrativeStartBonus()`, `_narrativeKillBonus()`, `toolbar._cooldownMul`.

### V6
- `CutsceneController.cs` (207 lignes) : typewriter d'un dialogue linéaire portraits left/right, **aucun système de choix**.
- `CutsceneDef.cs` : `Lines` only, pas de `Choices`.
- Pas de `narrativeChoices` field dans `ProgressData`.
- `EventSystem` propose des choix in-run via `EventDef` (event aléatoire entre vagues, 30% chance) — DIFFÉRENT du système narratif V4 (choix permanents qui persistent inter-runs).

### Verdict
ABSENT — gros écart design. V6 a remplacé "choix narratif persistant entre runs" par "events random pendant le run". Si Mike veut récupérer le système V4 : P1, 2-3 jours (extension CutsceneDef + UI choice + RunContext persisting bonuses + 3 effets gameplay).

---

## 8. Trophées (17 + galerie + bonus +5¢ start permanent) — **PARTIAL**

### V4 (`systems/Trophies.js`, 99 lignes + `TrophyScene.js`)
- **17 trophées** définis statiquement avec emoji + check function.
- `trophyBonus()` = `Object.keys(save.trophies).length * 5` → **chaque trophée donne +5¢ permanent au start de chaque niveau** (appliqué `LevelScene.js:107`).
- Galerie `TrophyScene.js` (5 cols × N rows) listant tous + débloqués.
- Trophées thématiques : world_done × 4 (espace/subocean/medieval/cyberpunk), boss kills × 3, daily_first / daily_streak_3, no_escape, first_skin, narrative_choice, etc.

### V6 (`Achievements.cs` + `AchievementRegistry`)
- **58 achievement assets sur disque** (`Assets/ScriptableObjects/Achievements/*.asset`) — bien plus que V4.
- `AchievementsPanel.cs` (galerie UI) + `AchievementToastController` (toast au unlock).
- Système flexible : `AchievementPredicateType.Counter` + `eventKey` + `threshold` → `TrackEvent(key, delta)` cumulative.
- `OnUnlocked` event + `rewardGold` per achievement (pending gold qu'on récupère au prochain launch).
- Special : `IsAllUnlocked()` → débloque "Perk Legendaire" (V6-exclusif).
- **Pas de bonus +5¢ start permanent** automatique. V6 a `MetaUpgradeSystem.StartCoinsBonus` (talents tree) mais c'est différent — talents achetés explicitement, pas accumulés via trophées.

### Verdict
PARTIAL — V6 a **plus** d'achievements (58 vs 17), mais ABSENT sur l'effet "+5¢ start permanent par trophée". Recommandation P2 : soit ajouter un MetaUpgrade auto-acheté par achievement, soit explicitement câbler `Economy.GetStartCoins()` pour intégrer `Achievements.Instance.UnlockedCount * 5`.

---

## 9. Stats Screen (kills, étoiles, tickets, streak, endless) — **PARTIAL**

### V4 (`StatsScene.js`, 127 lignes)
- 8 cards 2 cols : Kills, Tickets, Stars (× / total), Trophées, Niveaux finis, Streak Daily, Endless record, Daily joués.
- Sub-section "Voies narratives empruntées" (lien W4/W7/W10 choices).

### V6 (`StatsLifetimePanel.cs`, 410 lignes + `StatisticsController.cs`)
- Beaucoup plus riche : Today / Career Totals / Tower Stats (top 5 par poses) / Leaderboard Top 5 / Endless Top 10 / Per-world stars+score table.
- Source = `LifetimeStats.cs` (kills, gold, time played, wins, runs, par-tower placed/kills, leaderboard, per-world data).
- ABSENT côté V6 : "Streak daily", "Tickets cumulés" (V6 n'a pas de tickets-currency), "Voies narratives" (V6 n'a pas de choix narratifs).

### Verdict
PARTIAL — V6 dépasse V4 sur la profondeur stats (career, today, per-tower), mais manque les compteurs spécifiques V4 (streak, tickets, narrative paths).

---

## 10. Skins Shop (12 skins achetables tickets) — **DIVERGENT**

### V4 (`SkinsScene.js`, 228 lignes)
- **28 skins définis** (5 tiers : COMMUN / RARE / ÉPIQUE / LÉGENDAIRE / MYTHIQUE).
- Prix en **tickets** (`spendTickets()`), tickets gagnés via gameplay (`bumpTickets`, `events.on("ticket-collected")`).
- 1 skin équipé par tile via `setEquippedSkin(tileId, skinId)`.
- Premier skin → trophée `first_skin`.

### V6 (`SkinSystem.cs`, 189 lignes + `Assets/ScriptableObjects/Skins/`)
- **19 skin assets** sur disque (castles × 10, knights × 5, vfx × 4).
- Skins classés par `SkinTargetType` : Hero / Tower / Enemy.
- Prix gérés différemment : V6 n'a **pas de "tickets" currency** — la save a `gems` + `gold`, et `IsSkinOwned()` est testé. Aucun `UnlockSkin` automatique dans le code skin lui-même — c'est probablement géré via metaupgrades ou shop par ailleurs.
- `SkinPickerController.cs` côté UI.
- `SaveSystem.GetEquippedSkin(typeKey, targetId)` : équipement par (type, id) pair.

### Verdict
DIVERGENT — V6 a son propre système skins, conceptuellement plus propre (typing strict + alternate GLTF + material override + body color), mais **manque la currency tickets** et le système de tiers V4. Pas un blocker, c'est un nouveau design. P3.

---

## 11. Coop 2P split-clavier (P2 flèches + Enter) — **ABSENT**

### V4 (`ui/Player2Cursor.js`, 96 lignes)
- Curseur cyan visible quand P2 appuie flèches.
- Mouvement Arrow keys snap-to-cell + Enter pour placer la tile actuellement sélectionnée.
- Cleanup au shutdown / destroy.

### V6
- 0 hits sur `Player2` / `splitscreen` / `coop` dans Scripts/.

### Verdict
ABSENT. P3 (feature niche, faible ROI).

---

## 12. Mini-jeux Foire (5 mini-jeux : chamboule, pigeon, roue, crêpes, bumper) — **ABSENT**

### V4 (`scenes/FairgroundHubScene.js` 126 + `FairgroundScene.js` 1798)
- Hub avec **10 mini-jeux** (V4 a évolué au-delà du brief, brief disait 5) : chamboule-tout, tir au pigeon, roue de la fortune, course de crêpes, bumper cars, calcul rapide, feu tricolore, tir à l'arc, tri bonbons, saut de lave.
- `FairgroundScene.js` single 1798-line file qui switch sur `gameType`.
- Tickets gagnés in-game stockés dans save, `recordFairgroundScore(gameType, score)` + `getFairgroundBest()`.

### V6
- 0 hits sur `Fairground` / `chamboule` / `MiniGame` etc.

### Verdict
ABSENT complet. Port complet = 1-2 semaines (10 mini-jeux). P2 si Mike veut maintenir l'aspect "foire" du jeu V4, sinon décision de cut.

---

## 13. Treasure Chests (random +75¢ pendant gameplay) — **PARTIAL → upgraded**

### V4 (`entities/TreasureChest.js` + `LevelScene.js:341-355`)
- Spawn aléatoire (38-50s) max 3 chests par niveau, +75¢.
- Click-to-open.

### V6 (`Systems/TreasureSpawner.cs` + `Entities/TreasureTile.cs`)
- **Plus avancé** : 2 modes
  - A) Static tiles via map cells `*` (placés à la level load, broken par enemies, pay-out à wave end).
  - B) Wave-break random treasure (20% chance, `BalanceConfig.BreakTreasureChance`, spawné sur path waypoint, hero auto-collect dans 1.5m).

### Verdict
PARITY (V6 dépasse V4). Pas d'écart.

---

## 14. Pause auto quand tab perd focus — **PARITY**

### V4 (`LevelScene.js:1241-1246`)
- `document.addEventListener("visibilitychange", ...)` → si `document.hidden` et pas déjà paused → `_openPauseMenu()`.
- Cleanup au shutdown/destroy.

### V6 (`LevelRunner.cs:278-294`)
- `OnApplicationFocus(bool hasFocus)` Unity callback → si `!hasFocus && !_paused` → `Pause()` + `_autoPaused = true`. Resume auto si refocus et auto-paused.
- Gated par `UI.SettingsRegistry.Instance?.AutoPauseOnBlur` setting (V6 plus, settable par user).

### Verdict
PARITY (V6 ajoute un setting user, mieux). RAS.

---

## 15. Webfonts Bangers + Fredoka — **ABSENT (degraded)**

### V4
- Google Fonts CDN inline en HTML, utilisé partout via `fontFamily: "Bangers, Fredoka, system-ui"`.
- Style visuel signature du jeu V4 (gros titres BD + corps Fredoka).

### V6
- `Assets/Fonts/Roboto-Regular SDF.asset` UNIQUEMENT.
- 0 hits sur `Bangers` ou `Fredoka` dans Scripts/ ou Assets/ hors TextMesh Pro samples (qui sont des exemples non utilisés).
- UI Toolkit utilise SDF Roboto par défaut.

### Verdict
ABSENT — V6 UI a un look "neutre Unity" vs V4 "BD cartoony foire". Si Mike veut maintenir l'ID visuel : importer Bangers SDF + Fredoka SDF, mettre à jour UXML/USS styles. P2.

---

## 16. Bouton Mute 🔊 HUD top-right — **PARITY**

### V4 (`LevelScene.js:1273-1295`)
- Container top-right, click toggle 🔊 ↔ 🔇, persiste dans `save.settings.muted`, applique sur Audio + MusicManager.

### V6 (`UI/MuteToggleController.cs`, 55 lignes)
- Button `#btn-mute`, click toggle, persiste dans `PlayerPrefs["cd.audio.muted"]`, applique sur MusicManager + AudioController, raccourci clavier `M`, toast confirmation.

### Verdict
PARITY. RAS.

---

## 17. Restart rapide Shift+R — **PARITY**

### V4 (`LevelScene.js:1251-1258`)
- `keydown-R` + `e.shiftKey` → fadeOut → `scene.start("LevelScene", { levelId: this.level.id })`.

### V6 (`Visual/CameraController.cs:148-150`)
- `Input.GetKey(LeftShift|RightShift) && Input.GetKeyDown(R)` → `LevelRunner.Instance?.RestartLevel()` → `LevelLoader.LoadLevel(currentLevel.Id)`.
- Aussi exposé via PauseMenu "Restart" button + HUD `btn-restart-go`.

### Verdict
PARITY. RAS.

---

## 18. Bouton "Lancer la vague (N)" (joueur-driven pacing) — **PARITY**

### V4
- **ABSENT** côté V4 — V4 utilise auto-pacing temporel (`waveManager.tick(gt)`, prochaine vague spawn après délai). Pas de bouton manuel.

### V6 (`HudController.cs:419-424` + `WaveManager.cs:361-400`)
- **Présent et avancé** : `IsWaitingForPlayerStart`, bouton `wave-launch-pill` + N hotkey + Space, fenêtre de **skip bonus** (`SkipBonusGold = 30¢` + streak `StreakBonusPerWave = +5%/wave`, cap configurable).
- L10n : `"Launch wave [N]"` / `"Lancer la vague [N]"`.

### Verdict
V6 SUPÉRIEUR à V4 sur ce point — c'est une des features décidées dans la refonte (Q11=A "skip bonus stack"). PARITY+.

---

## Récap Gap List

### P0 — Blockers / cassé
- Aucun blocker repéré, V6 boot + menu + level loop tournent (sous réserve runtime test Unity).

### P1 — Important pour parité fonctionnelle
- **#4 Mode Carnaval** : feature complète manquante. ~3-5 jours.
- **#5 Mode Arène Boss** : facile à recréer via LevelData 3-wave. ~1 jour.
- **#6 Daily Streak counter** : ambiguïté Daily.cs vs DailyChallenge.cs, choisir l'un + ajouter streak. ~1 jour.
- **#7 Cutscene choices branchées + effets persistants** : gros morceau, 2-3 jours pour extension CutsceneDef + UI choice + 3 effets.
- **#3 Mismatch worldmap 8×10 code vs 10×9 SOs** : décider si on étend WorldCount→10 ou si on retire W9/W10 SOs. 30 min décision + 30 min impl.

### P2 — Nice to have / polish
- **#1 Splash version+hash affiché** (`Application.version` + git commit short). Trivial.
- **#8 Trophy +5¢ start permanent par achievement** : intégrer `Achievements.UnlockedCount * 5` dans `Economy.GetStartCoins()`. 1h.
- **#9 Stats screen : tickets / streak / narrative paths** : conditionnel à #6 et #7. Skip si autres P1 pas faits.
- **#12 Mini-jeux Foire (10 jeux)** : 1-2 semaines de dev pur, ROI très variable selon stratégie Mike.
- **#15 Webfonts Bangers + Fredoka** : import SDF + USS update. 2-3h.

### P3 — Optional / skip
- **#10 Skins tickets-currency tiers** : V6 a son propre système (skins par hero/tower/enemy, sans tiers + sans tickets). Décision design : V4 system est "skin cosmétique payant", V6 est plus "loadout système". Pas un must-have.
- **#11 Coop 2P split-clavier** : feature niche, faible ROI.
- Demo mode auto-trigger 60s : V6-only PLUS, à conserver.
- Save slots A/B/C : V6-only PLUS, à conserver.
- Auto Pause setting toggleable : V6-only PLUS.

---

## Tableau récap

| # | Loop | V4 | V6 | Status | Priorité |
|---|------|----|----|--------|----------|
| 1 | Boot → Menu | OK | OK (sans version) | PARTIAL | P2 |
| 2 | Menu principal | 7 boutons | 5 boutons + saves slots | PARITY | – |
| 3 | Campagne | 10×6=60 | 8×10=80 code, 10×9=90 SOs | PARTIAL | P1 |
| 4 | Mode Carnaval | 5 lvls conveyor | – | ABSENT | P1 |
| 5 | Mode Arène Boss | 3-wave boss | – | ABSENT | P1 |
| 6 | Défi du Jour | YYYYMMDD seed + streak | 2 systèmes // sans streak | PARTIAL | P1 |
| 7 | Cutscenes branchées | 6 + 3 avec choix | 6 linéaires, pas de choix | ABSENT | P1 |
| 8 | Trophées + bonus | 17 + +5¢ start | 58 + sans bonus permanent | PARTIAL | P2 |
| 9 | Stats screen | 8 cards | Lifetime / Career / Endless top10 | PARTIAL | P2 |
| 10 | Skins shop | 28 skins tickets/tiers | 19 skins (hero/tower/enemy) | DIVERGENT | P3 |
| 11 | Coop 2P | Flèches + Enter | – | ABSENT | P3 |
| 12 | Mini-jeux Foire | 10 jeux | – | ABSENT | P2 |
| 13 | Treasure chests | +75¢ random | Static + break-spawn 20% | PARITY+ | – |
| 14 | Pause auto tab blur | OK | OK + setting | PARITY | – |
| 15 | Webfonts | Bangers/Fredoka | Roboto | ABSENT | P2 |
| 16 | Mute button | 🔊 top-right | 🔊/M-key + toast | PARITY | – |
| 17 | Restart Shift+R | OK | OK + UI buttons | PARITY | – |
| 18 | "Lancer vague" button | – | N-key + Space + skip bonus | V6+ | – |

---

## Recommandations stratégiques

1. **Quick wins P1** (en 1-2 jours) : Mode Arène Boss (#5) + Daily streak counter (#6) + worldmap 8×10/10×9 mismatch (#3). 
2. **Décisions design** à acter avant dev :
   - Choix narratifs (#7) : on garde le concept V4 (3 choix permanents impactant gameplay) ou on assume les V6 events (random per-run) ?
   - Carnaval (#4) : on conserve ou on cut ? Si conserve, port long.
   - Mini-jeux Foire (#12) : 1-2 semaines de port, ROI ? Soit on conserve l'ID "foire" du V4, soit on assume pivot complet vers TD pur.
   - Skins (#10) : on porte les 28 skins V4 avec tickets ? Ou on assume le système V6 ?
3. **Polish facile P2** : version splash + webfonts + trophy bonus start. ~6h total cumulé.
4. **V6 PLUS à célébrer** : skip bonus + streak, save slots, demo mode, auto-pause setting, treasure system avancé, achievement system flexible — ne pas régresser dessus.
