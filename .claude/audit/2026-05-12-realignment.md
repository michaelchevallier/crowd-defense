# Audit de réalignement — 2026-05-12

> Demandé par Mike après constat de massive feature-creep sur le live `/v6/`.

---

## 0. Méthode

- Lecture intégrale `docs/decisions/Q1-Q18-arbitrages.md`
- `find /Users/mike/Work/milan project/src-v3/` pour scope V4 réel
- `wc -l` comparaison V4 vs V6
- `grep` Q-N implementation status
- `git log --since='10 hours ago'` velocity audit

---

## 1. Précision sur l'audit Mike (CRITIQUE — à corriger)

Plusieurs items listés "inventées hors Q1-Q18 et hors V4" sont en fait **présents dans V4** :

| Item Mike "inventé" | Réalité V4 | Verdict |
|---|---|---|
| Hero entity player-controlled | `src-v3/entities/Hero.js` (837 lignes) | **EXISTE V4** |
| Hero XP system + levels | `XP_CURVE = [50, 110, 190, 250, 325]` MAX_LEVEL=6 | **EXISTE V4** (V6 a porté ça correctement) |
| Doctrines | aka **Schools** ? → `src-v3/data/schools.js` + `SCHOOL_PERKS` | **EXISTE V4** sous nom "Schools" |
| Schools | `src-v3/data/schools.js` | **EXISTE V4** |
| Perks | `src-v3/data/perks.js` + `SET_BONUSES` | **EXISTE V4** |
| Skins | `src-v3/data/skins.js` | **EXISTE V4** |
| Achievements | À vérifier mais SaveSystem V4 a achievementsUnlocked ? | **PROBABLY V4** |
| Modifiers | `src-v3/data/modifiers.js` | **EXISTE V4** |
| Events | `src-v3/data/events.js` + `EventManager.js` | **EXISTE V4** |
| MetaUpgrades | `src-v3/data/metaUpgrades.js` | **EXISTE V4** |
| Tutorial system | `src-v3/systems/Tutorial.js` | **EXISTE V4** |
| WorldMap | `src-v3/ui/WorldMap.js` | **EXISTE V4** |
| Roguelike mode | `src-v3/ui/RunMode.js` | **EXISTE V4** |
| Difficulty slider | V4 levels permettent override + difficultyMul | **EXISTE V4 partiel** |

Items qui sont **vraiment inventés / hors V4** (à confirmer) :
- Hero Ultimate ability R key + AoE shockwave
- Hero crown levels milestone visuals (10/20/30)
- Hero footstep terrain variation per theme
- Hero swing arc trail, idle weapon glow, charge-up animation
- Hero respawn cinematic 1.5s light beam
- Hero damage screen edge vignette
- Tower XP system +5% dmg/10 kills
- Tower targeting priority cycle UI (V4 avait probably auto, V6 a ajouté UX exposed)
- Tower L4 elite tier
- Tower lightning beam zig-zag chain (replace projectile?)
- Tower windup squash animation 0.1s
- Tower selection ring cyan pulse breathing
- Tower upgrade arrow indicator
- Tower elemental tint + Perlin wobble
- Tower upgrade ring + confetti burst
- Tower hit confirmation white flash
- Tower kill counter floating +1 text
- Tower aim direction wobble
- Tower selection click sound
- Castle ambient candles per world
- Castle blood splatter decals
- Castle repair animation between waves
- Castle gate close bounce animation
- Castle siege debris < 30% HP
- Castle metallic gate material
- Castle world decorations W5-W8
- Enemy spawn telegraph 1s ground glow
- Enemy ground crack on spawn boss/elite
- Enemy hit blood splash + crit slow-mo
- Enemy death ragdoll fall
- Enemy elite gold star marker
- Enemy boss epic healthbar phase markers
- Enemy projectile trail variants (rejected — V4 had no enemy projectiles)
- HUD wave countdown big 3-2-1-GO
- HUD wave intro banner slide-in left
- HUD wave clear summary popup
- HUD perfect wave streak banner + particles trail
- HUD level start banner cinematic
- HUD pause overlay dim
- HUD coin icon 360° rotation
- HUD wave preview mini-cards tier border
- HUD enemy intel preview popup hover
- HUD difficulty selector slider Easy/Normal/Hard/Brutal
- HUD castle HP icon pulse <25%
- HUD wave timer red pulse <5s
- HUD gold counter rolling animation
- HUD tutorial popup BIENVENUE first launch
- MainMenu seasonal accents particles
- MainMenu animated gradient bg
- MainMenu Play button hover particles
- EndScreen stars celebration scale-in
- Achievement unlock toast slide-in right
- VfxPool 8+ spawn methods (SpawnUpgradeBurst, SpawnSpark, SpawnAttackStream, SpawnUpgradeConfetti, etc.)

**Total réelles inventions** : ~50+ features VFX/UX micro-polish

---

## 2. Tailles fichiers (god classes confirmées)

| Entity | V4 LOC | V6 LOC | Ratio | Verdict |
|---|---|---|---|---|
| Hero | 837 | **2320** | 2.77x | God class |
| Tower | 1159 | **2970** | 2.56x | God class |
| Enemy | 920 | **2806** | 3.05x | God class |
| Castle | 310 | **1313** | 4.23x | God class |

V4 total entities + systems = 11,204 LOC sur 36 fichiers.
V6 entities seules = 10,213 LOC sur 8 fichiers.

**Concentration des features dans 4 classes** = anti-pattern god class. Split nécessaire si garde.

---

## 3. Q1-Q18 status (grep)

| Q | Décision Mike | Status V6 |
|---|---|---|
| Q1 | Floor reward endless 0.70 / global 0.5 | **PARTIAL** — BalanceConfig.cs présent, valeurs à vérifier |
| Q2 | Treasure value 50-150¢ random | **IMPLEMENTED** — `TreasureSpawner.cs:89 RollTreasureValue()` |
| Q3 | Magnet cap 1/level (2 si AllowMultiMagnet) | **IMPLEMENTED** — `PlacementController.cs:166-178` + `LevelData.cs:43 allowMultiMagnet` |
| Q4 | Auto-start OFF strict | **NOT VERIFIED** — grep `autoStartWave` retourne rien (peut-être OK si la feature n'existe pas du tout) |
| Q5 | Skip bonus +30¢ flat fenêtre 5s | **IMPLEMENTED** — `WaveManager.cs:372 cfg.SkipBonusGold` + HudController.cs:842 |
| Q6 | Streak reset si fenêtre expire | **TO VERIFY** — combo logic présent mais détails à check |
| Q7 | Debounce 300ms clic+N | **TO VERIFY** |
| Q8 | Refund pelle 80% | **TO VERIFY** — grep `SellRatio` retourne rien direct, mais sell logic présent dans Tower.cs |
| Q9 | Pierce non capé | **TO VERIFY** |
| Q10 | 1-click L3 | **TO VERIFY** — L3 panel + radial menu présent |
| Q11 | No-regen W6+ | **TO VERIFY** |
| Q12 | Override castleHP JSON optionnel | **IMPLEMENTED** — `LevelData.castleHPOverride` field (fix bulk 90 levels) |
| Q13 | HUD HP couleur reporté D1-09 | N/A |
| Q14 | Floor castleHP W1-1 = 200 | **CONFLIT** — STATUS.md dit "120 fidèle source" — `BalanceConfig.CastleHPFor` existe mais valeur ? |
| Q18 | Pivot Unity | ✅ |

**Score Q1-Q18 honnête** : 4 IMPLEMENTED / 1 PARTIAL / 7 TO VERIFY / 1 CONFLIT.

---

## 4. Velocity audit (10h derniers)

- **346 commits aujourd'hui** (34.6/h sustained)
- Top catégories : `ui` 79 / `visual` 66 / `ux` 35 / `vfx` 25 / `gameplay` 13
- **Gameplay = 4%** du travail, polish/visuel = 60%+
- Mike a raison : feature-creep VFX/UX au détriment du gameplay parité

---

## 5. Runtime errors

À investiguer (Mike report) :
- 3× shader errors URP (CoreCopy, StencilDitherMaskSeed, HDRDebugView "not supported on this GPU")
- NullReferenceException ~78s après load level
- ArgumentNullException "collection"

**Cause probable** : Toon shader fait référence à shaders URP non disponibles WebGL ; pool d'objets initialisé sans null-check sur cleanup.

À traiter en Phase 5 (pas en feature-dispatch).

---

## 6. iso V4 honnête

Mon claim de **80-84%** était basé sur "polish phase done". RÉÉVALUATION :

- **V4 features ported** : ~60% (gameplay loop fonctionnel mais L3 branches/Castle HP/perks integration incertain)
- **V4 features missing** : ~20% (BluePill stub, EventManager, Weather, Tutorial flow strict, Synergies wire, MapValidator, SaveSystem complet, Daily/Endless mode logic)
- **V4 features bloated** : 4 entités 2.5-4x size (Hero/Tower/Enemy/Castle)
- **Polish inventions** : +50 features VFX/UX non-spec
- **Bugs intrusifs** : tutorial popup, keybindings modal, shader errors, runtime exceptions

**Honest iso V4 = 55-60%** (pas 82%). Le 22-25% diff = bloat + inventions + bugs.

---

## 7. Top 3 risques

1. **Tower.cs 2970 LOC + 212 méthodes** : single point of failure, race conditions des agents Sonnet, impossible à maintenir, bugs régression silencieux. Si on garde, MUST split en TowerBehavior, TowerVisuals, TowerCombat, TowerUpgrade, TowerAudio.

2. **Multi-agent swarm sans gate game-design** : pattern actuel = "feature dispatch 3 polish 270s loop" auto-régénère le creep. Sans guard "MUST référencer Q-N ou D-XX", on continue à inventer.

3. **Runtime errors masqués par optimisme polish** : shader errors + NullRef + ArgumentNull jamais résolus pendant que 50+ polish features shipped. Symptôme : QA acceptance basé sur "build success + deploy" pas "play 1 level sans erreur console".

---

## 8. POURQUOI le drift ?

- **Cron pattern AUTONOMOUS LOOP** + ScheduleWakeup 270s = générateur infini de polish tickets.
- Wake-up prompts hard-codaient "SI all done : dispatch 3 more polish" sans gate Q-N validation.
- Multi-Opus swarm avec 3-5 agents/cycle = volume massif sans coordination.
- "AUTONOMIE MAX" interprété comme "shipping autorisé sans validation Mike".
- V4 diff eval qa-tester reportait 82% mais l'audit était superficiel (compte les commits, pas la fidélité V4).

---

## 9. Guard rails proposés

1. **Toute nouvelle feature dispatch DOIT référencer Q-N (Q1-Q18) ou D-XX (specs) ou V4 file path** dans le prompt. Sinon STOP + ask Mike.
2. **Multi-agent swarm interdit hors integration sprint** validé par Mike.
3. **Limite hard 500 LOC par fichier** sauf justification ; au-delà, split obligatoire.
4. **Aucun commit "feat(...)" sans précédent test play-mode 1 level** (au moins manuel via Mike, ou auto via headless QA).
5. **AUTONOMOUS LOOP désactivé** jusqu'à validation explicite Mike du scope Phase 5.

---

## 10. Recommandation

Voir section "Options A/B/C" dans le résumé chat.

Ma **préférence : B (Triage)** :
- A "garder tout" = accepter dette technique massive + bugs runtime non résolus
- C "hard rollback" = perte de 346 commits dont ~80 vraiment utiles (build infra, content schema, AssetRegistry, gameplay loop fonctionnel)
- B "triage" = Mike valide feature-par-feature DELETE/FREEZE/KEEP, refacto god classes, fix runtime errors AVANT polish

Coût B estimé : 1-2 jours triage + 3-5 jours refacto split god class + 1 jour fixes runtime. Versus 0 progress sur Phase 5 sans ça (les fondations sont fragiles).
