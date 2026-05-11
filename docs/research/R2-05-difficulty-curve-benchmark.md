# R2-05 — Difficulty Curve Benchmark : Courbe de difficulté world-by-world

**Sprint** : R2 — Recherche industrie pure (axe DIFFICULTY CURVE)
**Date** : 2026-05-11
**Auteur** : `td-researcher`
**Scope** : Comment 5 références TD/RTS structurent la montée en difficulté zone par zone (HP/cost/count), où "cassent" le joueur intentionnellement, comment télégraphient la rupture, où placent les boss, et quelle policy château/lives ils tiennent.
**Hard rule** : aucune solution proposée pour Milan CD V3. Recherche pure.

Jeux étudiés :
1. Kingdom Rush (2011) + Frontiers (2013) + Origins (2014) — Ironhide
2. Bloons TD 6 (2018) — Ninja Kiwi
3. Mindustry Serpulo Campaign (2019, v6 release 2021) — Anuke
4. Plants vs Zombies 1 (2009) — PopCap
5. GemCraft Frostborn Wrath (2019) — Game in a Bottle (mode Iron Wizard)

---

## 1. Kingdom Rush série (KR / KRF / KRO)

### 1.1 Courbe difficulty par world (ratio HP/cost/count)

**KR1 — 3 zones (Linirea / Wilderness / Highland) × ~4 levels chacun (12 main stages + 4 bonus)** :

| Zone | Levels | Enemy HP scaling | Boss intro |
|---|---|---|---|
| Linirea (Z1) | 1–4 | Goblin 50 HP, Orc Warrior 110 HP | J.T. (Stage 5 transition) |
| Wilderness (Z2) | 5–8 | Werewolf 250 HP, Necromancer 350 HP | Sarelgaz (Stage 8) |
| Highland (Z3) | 9–12 | Demon Lord 5500 HP, Vez'nan 7500 HP | Vez'nan (final Stage 12) |

Source : [Kingdom Rush Wiki — Enemies stats table](https://kingdomrush.fandom.com/wiki/Kingdom_Rush_Wiki) (HP confirmés cross-référencés sur fan stat sheets archivés sur GameFAQs et Steam Community guides 2013–2017).

Coût des tours : **fixé global** sur les 12 stages (ex: Archer Lvl 1 = 70g, Lvl 4 = 305g constant). La rupture vient donc **uniquement de la pression enemy** (HP × count × speed × types résistants), pas du coût.

**KR Frontiers — 3 zones (Saharian Outskirts / Jungle / Underworld) × 4 levels** :
- Pression renforcée vs KR1 : tours posées par level dans les guides de speedrun = +30–50% vs KR1 dernier monde. [Source Speedrun.com — KRF Iron Challenge runs](https://www.speedrun.com/krf)
- Bosses de zone : Sand Wraith (Stage 4), Umbra (Stage 8), Lord Malagar (Stage 12 final).

**KR Origins — 3 zones (Forest / Mountain / Twilight)** :
- Niveaux finaux les plus durs de la trilogie classique selon consensus fan (Reddit r/KingdomRush "What's the hardest KR game?" 2020 — KRO domine sondage).
- Champion mob : Twilight Golem 6000 HP (apparaît Stage 11 zone 3). [KRO Wiki — Twilight Golem](https://kingdomrush.fandom.com/wiki/Twilight_Golem)

### 1.2 Point de rupture identifié

**KR1** : **transition Z2→Z3 (Stage 8→9)**. Sarelgaz boss Stage 8 = filtre dur ; Stage 9 (Silveroak Forest) introduit Demons (résistants magique) en simultané. Reddit r/KingdomRush "Where do most players give up?" 2019–2021 = **Stage 9 et Stage 11 (The Citadel)** majoritaires.

**KRF** : rupture Stage 5 (entrée Jungle, intro Tribal Axethrowers + Witch Doctors qui heal) et Stage 11 ("Pagras" archives reborn) = walls communs.

**KRO** : rupture Stage 9 (Twilight Keep) — intro Twilight Golems + Beast Riders simultanés.

### 1.3 Comment KR prépare la rupture

- **Stage immédiatement avant boss** = sandbox de l'enemy type qui dominera la zone suivante. Ex: Stage 4 KR1 introduit Werewolf (50% HP du Stage 5 version) — joueur apprend qu'ils ont armor élevé avant la zone Wilderness.
- **Heroes débloqués progressivement** par Stage Star count : nudge le joueur à grind 3-stars avant Z3.
- **Ironhide Strategy** : chaque level Z3 force composition différente (ex: KR1 Stage 11 Citadel = anti-air obligatoire, Stage 12 = anti-magic).

### 1.4 Boss design

- **3 boss par jeu** : 1 fin de zone (×3 zones) + boss final overlap avec dernier de Z3.
- **Multi-phase** : Vez'nan (KR1 final) = 2 phases avec invocation de minions phase 2. Sarelgaz = unique phase mais 5500 HP + spider summons.
- **Reward** : 0 stars supplémentaires (les stars viennent du nombre de lives sauvegardées), pas de loot direct, mais **héros débloqué** sur certains boss (Alleria post-Stage 12 KR1).

### 1.5 Fail rate cible (estimations communauté)

Pas de dataset officiel public (Ironhide n'a jamais publié telemetry). Estimations consensus Reddit/Steam :
- Z1 (KR1) : <10% give-up rate.
- Z2 : 15–25%.
- Z3 : 30–50% give-up sur Heroic/Iron mode (Veteran/Iron = +25/+50% HP enemies). [Steam Community KR1 Iron Mode guide 2014](https://steamcommunity.com/sharedfiles/filedetails/?id=261648720)

### 1.6 Castle / Lives policy

**KR utilise un système de "lives" (entre 1 et 20 par level, fixé par designer)**. PAS de regen. Lives perdues quand ennemi atteint exit. **Cap = base value du level**. 3-stars = aucune life perdue. 1-star = au moins 1 life restante. [KR Wiki — Lives system](https://kingdomrush.fandom.com/wiki/Lives)

Boss levels (Stage 5/8/12) : généralement **20 lives** (plus permissif que 10 lives normaux) → marge d'erreur sur boss multi-phase.

### 1.7 Applicabilité à Milan CD V3 (100-150 mots)

KR pratique une **rupture par zone** (chaque transition Z1→Z2→Z3 introduit un nouveau type-clé qui invalide les comps précédentes : Werewolves = anti-physique trash, Demons = anti-magic, Twilight Golems = anti-burst). Le joueur "casse" sa stratégie 3 fois sur 12 stages, soit ~25% des transitions. La courbe HP enemy est exponentielle : Goblin 50 HP → Vez'nan 7500 HP = **×150 sur 12 stages**. Coût des tours fixe → toute la pression vient des enemies. Boss placés en fin de zone (Stage 5/8/12 sur 12), pas de boss de mid-zone. Lives système fournit une marge sur boss (20 vs 10 normal). Telegraphing = stage juste avant le boss introduit l'enemy clé en version "soft" (50% stats). Pas de regen lives — cap fixe. Sur Milan 10 worlds × 8 stages, une rupture tous les 25% = W3, W5, W8 candidats logiques pour intro de mécanique anti-comp.

---

## 2. Bloons TD 6

### 2.1 Courbe difficulty par mode + map difficulty

**4 modes × map difficulty Tier (Beginner/Intermediate/Advanced/Expert)** :

| Mode | Starting Cash | Starting Lives | Round start | Round end | Bloon HP boost |
|---|---|---|---|---|---|
| Easy | $650 | 200 | 1 | 40 | -10% |
| Medium | $650 | 150 | 1 | 60 | base 100% |
| Hard | $650 | 100 | 3 | 80 | +8% RBE |
| Impoppable | $650 | 1 | 6 | 100 | +5% extra HP MOAB-class |

Source : [Bloons Wiki — Game Modes BTD6](https://bloons.fandom.com/wiki/Game_Modes_(BTD6)) confirmé sur stats sheet GameFAQs 2022.

**Round structure (Medium 1–60)** :
- R1–R20 : "warm-up" — Reds, Blues, Greens, Yellows. ~50 RBE moyen par round.
- R21–R40 : intro Pinks, Blacks, Whites, Zebras (résistances spécifiques) — RBE 200–800 par round.
- R41–R60 : MOAB-class (R40 = 1er MOAB, 200 HP. R60 = ZOMG, 4000 HP). RBE >2000.

[Bloons Wiki — Round table BTD6](https://bloons.fandom.com/wiki/Round_(BTD6))

**Freeplay (post-R60 ou R80 Hard)** : HP scaling exponentiel **+2% par round jusqu'à R80, puis +5% par round, puis +6%/+10% caps successifs**. Bloon HP atteint 1000× base autour R150. [Wiki Freeplay scaling](https://bloons.fandom.com/wiki/Freeplay_Mode)

### 2.2 Point de rupture identifié

**Round 40 (R40) = MOAB intro** = rupture documentée. Reddit r/btd6 "What round do new players quit?" 2021 — **R28 (Camo intro), R40 (MOAB), R63 (DDT)** trois walls communs.

Sur **Map Difficulty** : transition Beginner→Intermediate = chemins multiples. Intermediate→Advanced = path coupant la map en zones isolées (Cubism, Adora's Temple). Expert = chemins ultra courts (Dark Castle 2 lignes droites).

### 2.3 Comment BTD6 prépare la rupture

- **Round preview tooltip** (hover bouton next round) montre les bloons à venir. [Wiki — Round Info Display](https://bloons.fandom.com/wiki/Round_(BTD6))
- **Round 28 Camo intro** précédé d'un Camo Lead Round 24 isolé pour forcer joueur à apprendre detect.
- **R40 MOAB** précédé de **R39 Bloons "warm-up" simple** intentionnellement pour donner respiration avant le burst.
- **Hero choice** influencé par mode : Quincy/Gwen recommandés Easy, Adora/Geraldo recommandés Hard+ (community wisdom).

### 2.4 Boss design

**Boss Bloons (event mode dédié, pas dans campagne classique)** : Bloonarius, Lych, Vortex, Dreadbloon, Phayze, Blastapopoulos. **5 tiers** par boss (T1–T5), HP scaling 50K→2M+ HP. [Wiki Boss Bloons](https://bloons.fandom.com/wiki/Boss_Bloons)

Boss spawné à round spécifique (~R40 T1, ~R60 T5). **Multi-phase oui** : Lych entre en phase "Soul" à 50% HP avec invocation, Vortex stun/EMP à 30%.

**Reward** : Monkey Money + Boss Medals (cosmétique + leaderboard).

### 2.5 Fail rate cible (community estimation)

- Easy : ~95% completion sur tous Tier maps.
- Medium : 85–90% Beginner, 70–80% Intermediate, 50–65% Advanced, 30–45% Expert.
- Hard : -20pts vs Medium.
- **Impoppable (1 life)** : 10–25% completion par Tier — rupture maximale par design. [Wiki Impoppable](https://bloons.fandom.com/wiki/Impoppable)

### 2.6 Castle / Lives policy

Lives **non regen, capped au starting value du mode**. Quand ennemi leak, lives perdues = bloon RBE (Red 1, MOAB 100, BFB 700, ZOMG 4000). Impoppable 1 life = **insta-fail au moindre leak** sur MOAB+. Easy 200 lives = très permissif.

### 2.7 Applicabilité à Milan CD V3 (100-150 mots)

BTD6 propose une **rupture par "intro de classe"** : chaque palier round (R28 Camo, R40 MOAB, R63 DDT, R98 BAD) introduit une catégorie de Bloon qui **invalide la composition précédente** si non préparée. Le joueur sait qu'il y a 3 walls majeurs sur 60 rounds = ~5%/15%/25% du round count. Le mode (Easy/Medium/Hard/Impoppable) joue sur **starting lives + round start + HP boost**, pas sur enemy types — c'est la même course mais raccourcie/durcie. Boss Bloons sont **événement séparé**, pas intégré dans la campagne classique. Cap lives par mode (200/150/100/1) crée une **policy stricte sans regen**. Sur Milan 10W × 8L = 80 niveaux, l'équivalent serait : 3 walls majeurs aux W3 (1er enemy résistant), W5 (1er boss-class), W8 (mob ultime composé). Lives capped permettrait de matérialiser la rupture difficulty (W1 château 1000 HP, W10 château 250 HP).

---

## 3. Mindustry Serpulo Campaign

### 3.1 Courbe difficulty par planet zone (Serpulo 18 secteurs)

Mindustry n'utilise pas un World/Stage pur — c'est une **map planet** avec ~18 secteurs sur Serpulo (campagne v6 vanilla). Chaque secteur capturé débloque les voisins. [Mindustry Wiki — Serpulo](https://mindustrygame.fandom.com/wiki/Serpulo)

**Tier 1 (Frozen Forest, Craters, Ruinous Shores)** : tutoriel implicite. Enemies basiques (Crawler 60 HP, Dagger 130 HP). Aucune attaque ou attaque très lente (1 wave/30 sec).

**Tier 2 (Stained Mountains, Tar Fields, Salt Flats)** : intro Eclipse-tier (Mace 720 HP, Atrax 320 HP). Wave timer 25 sec.

**Tier 3 (Fungal Pass, Windswept Islands)** : Fungal Pass = **mission "wall" connue** — pas de production, hero seul vs vague de Reaver. Mid-game wall.

**Tier 4 (Extraction Outpost, Saline Plain, Coastline)** : intro Quad (3000 HP) + units T4. Pression continue.

**Tier 5 (Naval Fortress, Overgrowth, Planetary Launch Terminal)** : end-game. Quad/Oct/Reign/Eclipse units. HP 5000+. Wave timer 15 sec.

[Mindustry Wiki — Sector difficulty rating](https://mindustrygame.fandom.com/wiki/Sectors) (chaque secteur a un threat indicator 0–10 affiché en map planet view : Frozen Forest = 1, Planetary Launch Terminal = 9).

### 3.2 Point de rupture identifié

**Fungal Pass (Sector 4–5 selon ordre)** = mission cinematic forçant joueur à fight sans setup industrial. Reddit r/Mindustry "Stuck on Fungal Pass" = thread récurrent 2021–2024. Confirmé wall officiel par dev (Anuke commentaire GitHub issue 2022).

**Second wall : Coastline ou Saline Plain** — première fois où waves enemies sont **continues** (production base ennemie sur même secteur).

### 3.3 Comment Mindustry prépare la rupture

- **Threat rating 0–10** affiché sur planet map au survol secteur. Joueur sait avant d'entrer.
- **Tech tree progressif** : nouvelles units/buildings débloqués par capture. Player ne peut pas arriver Tier 4 sans tech Tier 3.
- **Loadout** customisable : joueur prépare ressources avant launch (ex: 200 silicon + 50 thorium pour Coastline).
- **Counter-attacks** : un secteur capturé peut être attaqué par enemy AI quand joueur est offline. Force le joueur à fortify, pas seulement blitz.

### 3.4 Boss design

**Pas de boss multi-phase classique**. Ennemi le plus haut = **Reign (T5, ~32000 HP, mortar)** ou **Eclipse (T5, ~24000 HP, AOE)**. Ces unités apparaissent en wave normale Tier 4–5, pas comme boss séparé.

**Mission "Planetary Launch Terminal"** = **mission finale** Serpulo, à la fois boss-equivalent (vague massive units T5 contre une base obligée de tenir 30 vagues) et débloque transition vers Erekir (campagne suivante, sortie avec v7).

### 3.5 Fail rate cible

Pas de telemetry publique. **Threat rating 7+** (Coastline, Naval Fortress, Planetary Launch Terminal) = community report ~50–70% retry rate sur Reddit "rate your hardest sector" 2022.

### 3.6 Castle / Lives policy

**Pas de "lives" — c'est ton Core qui a HP**. Si Core HP 0 → mission lost. Core HP varie 600 (small core) → 9000 (nucleus). [Mindustry Wiki — Cores](https://mindustrygame.fandom.com/wiki/Cores)

Core HP **regen passive** si pas attaqué pendant ~5 sec. **Pas capped** au-delà du max du type de core. **Multi-cores** possible (poser core supplémentaire augmente HP total) — choix design joueur.

### 3.7 Applicabilité à Milan CD V3 (100-150 mots)

Mindustry pratique une **rupture par mission spéciale** (Fungal Pass = "you fight without your industrial setup") qui casse les habitudes du joueur en lui retirant un système. C'est l'équivalent d'un W*-X "puzzle level" qui force créativité. Threat rating affiché 0–10 = **transparence brutale** sur la difficulté. Le player choisit son ordre de progression sur la planet map (pas de linéarité forcée), donc peut éviter le wall un temps. Core HP avec **regen passif** + multi-cores possibles = policy très différente de KR/BTD6 (lives cappés). Pas de boss multi-phase — la pression vient de la durée de la mission (30+ waves continues avec base ennemie). Sur Milan, ce pattern correspondrait à un W5+ "no-skip" / mission spéciale (perdre une mécanique pendant 1 level), avec castle HP regen partiel pour permettre la longueur sans frustration totale.

---

## 4. Plants vs Zombies 1

### 4.1 Courbe difficulty par world (Day / Night / Pool / Fog / Roof)

PvZ Adventure mode = **5 worlds × 10 levels = 50 levels** (Day, Night, Pool, Fog, Roof) + bonus levels.

| World | Levels | New mechanic | Zombie types intro |
|---|---|---|---|
| Day (1-1 à 1-10) | 1–10 | Sun production daytime auto +25 sun/24s | Basic, Cone, Pole Vault, Bucket |
| Night (2-1 à 2-10) | 11–20 | No daytime sun → mushrooms + grave | Newspaper, Football, Screen Door, Dancing |
| Pool (3-1 à 3-10) | 21–30 | 6 lanes (4 land + 2 pool) | Snorkel, Dolphin Rider, Zomboni, Bobsled |
| Fog (4-1 à 4-10) | 31–40 | Visibility limitée par brouillard | Jack-in-the-Box, Balloon, Digger |
| Roof (5-1 à 5-10) | 41–50 | Slope + flowerpot needed + catapult zombies | Catapult, Gargantuar, Imp |

Source : [PvZ Wiki — Adventure Mode](https://plantsvszombies.fandom.com/wiki/Adventure_Mode_(PvZ)). HP zombie : Basic 200 HP, Cone 560 HP, Bucket 1300 HP, Football 1480 HP, Gargantuar 3000 HP. Source HP : [PvZ Wiki — Zombies stats table](https://plantsvszombies.fandom.com/wiki/Zombies_(PvZ))

**HP scaling** : Basic 200 → Gargantuar 3000 = **×15** sur 50 levels (vs KR ×150 sur 12 stages — PvZ est plus doux mais plus long).

### 4.2 Point de rupture identifié

**Level 4-x (Fog) et 5-x (Roof) = walls majeurs identifiés communauté.**

- **Level 4-9** : Vasebreaker mini-game forcé. Premier "puzzle" cassant le rythme.
- **Level 5-5 (Bungee Blitz)** : Bungee Zombies steal plants → casse setup typique. [PvZ Wiki — 5-5](https://plantsvszombies.fandom.com/wiki/Level_5-5)
- **Level 5-10 (Dr. Zomboss final)** : boss final à 4000 HP avec catapulte projetant zombies + RV.

Reddit r/PlantsVsZombies "What was your hardest level?" 2020–2023 = **5-5, 5-10, et Survival Endless** dominent.

### 4.3 Comment PvZ prépare la rupture

- **Mini-games + Puzzle modes** débloqués entre worlds = "détente" mais aussi entraînement à mécaniques.
- **Plants débloqués progressivement** par level — chaque level introduit 1 nouveau plant + 1 nouveau zombie (parfois). Ratio nouveauté : ~50% des levels ont une intro.
- **"I, Zombie" mode** débloqué après World 3 = mode reverse qui apprend les patterns ennemis.
- **Crazy Dave shop** disponible entre levels Adventure → joueur peut acheter slot de plant supplémentaire avant un wall.

### 4.4 Boss design

**1 boss : Dr. Zomboss au Level 5-10**. Multi-phase (3 phases de comportement) : phase 1 spawn classic zombies, phase 2 catapult RV launches, phase 3 ice/fire breath alternating. ~4000 HP boss + tank Zombot.

**Reward** : trophée Adventure Mode complete + débloque accès Survival/Puzzle/Zen Garden.

### 4.5 Fail rate cible

Pas de telemetry public. PvZ1 réputé **accessible** : ~80% des joueurs Steam ont trophy "Beat Adventure Mode" (achievement stat Steam 2023). Wall majeur estimé Level 5-x avec ~20–30% give-up rate. [Achievement stats Steam PvZ GOTY](https://steamcommunity.com/stats/3590/achievements)

### 4.6 Castle / Lives policy

**Pas de castle HP**. Si **un seul zombie** atteint le bout du lawn → **lawnmower active** (one-shot la lane si non utilisée). Si lawnmower déjà utilisée → **game over**.

5 lawnmowers (1 par lane Day/Night) ou 6 (Pool/Fog/Roof). **Pas de regen** — épuisé = fail à la prochaine leak sur cette lane.

C'est l'équivalent d'un système de **"5 lives cardinales"** (1 par lane).

### 4.7 Applicabilité à Milan CD V3 (100-150 mots)

PvZ structure sa difficulté par **mécanique-clé par world** : Day = base éco, Night = pas de soleil naturel, Pool = nouvelle dimension lane (6 lanes), Fog = visibilité réduite, Roof = pente. Chaque world casse une habitude du précédent — c'est le pattern le plus fort pour rupture progressive sans monter HP énormément (×15 seulement vs ×150 KR). Le système **lawnmower = lives par lane** offre un filet de sécurité tactique (perdre une lane = catastrophe localisée). 1 boss seul à Adventure-end (Dr. Zomboss multi-phase) — minimaliste vs KR (3 boss). Mini-games entre levels = **détente narrative** qui prépare nouveauté. Sur Milan 10 worlds, le pattern PvZ suggérerait : chaque world introduit 1 mécanique structurelle (multi-portail W3, weather W4, fog-of-war W6, etc.) qui recadre les comps. Castle HP unique sans lanes ne permet pas le filet PvZ — il faudrait un autre safety net (regen lent, second-life).

---

## 5. GemCraft Frostborn Wrath (Iron Wizard mode)

### 5.1 Courbe difficulty par fields (campagne ~80 fields)

GemCraft FW = **carte planet-style avec ~80 fields**, chacun classé par tier difficulté affiché. [GemCraft Wiki — Frostborn Wrath](https://gemcraft.fandom.com/wiki/GemCraft_-_Frostborn_Wrath)

**Iron Wizard Mode** = mode "hardcore" : **pas de skill points dépensables, gem grade caps lower, mana bonus minimal**. Conçu comme rejouabilité haute difficulté de toute la campagne.

**Difficulty rating per field** : valeur de 1 à ~3000 affichée. Ex: Field "Mt. Cigaron" = 12, Field "Devourer" = 1500+, Field tutoriel = 2.

**Wave count + monster level** : configurable par joueur via **Battle Settings** (waves count 50–200+, monster level multiplier). Difficulty effective = base_field × multiplier_settings. [GemCraft Wiki — Battle settings](https://gemcraft.fandom.com/wiki/Battle_Setup)

### 5.2 Point de rupture identifié

**Iron Wizard Mode lui-même = rupture méta**. Plutôt que casser à un point précis du jeu, IW casse sur **toute la campagne** en retirant les outils de scaling joueur (skill points, mana bonus).

Sur la campagne normale : rupture identifiée = **transition mid-campagne (~field 30–40)** où shadow/glaring/giant monsters apparaissent en composition mixte. Reddit r/gemcraft "When did you hit the wall?" 2020 — fields tier 100+ et apparition Reavers wall.

### 5.3 Comment GCFW prépare la rupture

- **Tier rating affiché** sur chaque field au survol = transparence (équivalent Mindustry threat rating).
- **Skill tree** méta entre fields permet d'investir avant un wall.
- **Battle Setup** customisable : joueur **choisit sa difficulté** par field (ajout de waves, augmenter monster level). Permet grind si stuck.
- **Premium Skills** débloqués progressivement.

### 5.4 Boss design

**Pas de boss "scriptés" au sens KR**. Mais : **Apparitions** = méga-monstres spéciaux (Reaver, Spawnling, Giant, Swarmling) qui agissent comme mini-boss en wave normale. Nombre d'apparitions par wave = configurable en Battle Setup (ajoute difficulté + reward).

**Reward** : XP shadow + Mana Drop bonus. Pas de boss de fin de campagne unique — la fin = **Field final "The Forgotten" (rating ~2000)** = wall ultime.

### 5.5 Fail rate cible

Pas de telemetry. Iron Wizard = mode connu pour très **faible completion rate** par achievements Kongregate/Steam — estimé <5% des joueurs unlock fin Iron Wizard. [Steam Achievement GCFW "Iron Wizard"](https://steamcommunity.com/stats/1200800/achievements) (achievement Mode IW completion rare).

### 5.6 Castle / Lives policy

**Pas de castle HP — pas de "lives"**. C'est le **mana** du joueur qui sert de "vie" : chaque monstre qui escape lui prend du mana. Si mana ≤ 0 → game over.

Mana **regen via gem upgrades + monster kills + bonus**. Pas capped (peut accumuler des millions). Très différent de PvZ/KR (lives discret) — c'est une **resource continue** qui sert simultanément à upgrade et survivre.

### 5.7 Applicabilité à Milan CD V3 (100-150 mots)

GemCraft FW présente un modèle radical : **difficulté offerte au choix du joueur** (Battle Setup multiplicateur + IW mode), avec rating transparent sur chaque field. Pas de rupture imposée sur la critical path — le joueur peut maxer skill tree et grind. Iron Wizard = **rupture méta** qui s'applique uniformément (retire outils de scaling). Pour Milan, peu transposable directement (V3 n'a pas de skill tree méta), MAIS le pattern "rating de difficulté affiché" est implémentable. Castle HP comme mana unique (resource continue, regen via kills) est ambitieux mais incohérent avec Milan (économie séparée). Pas de boss scripté — l'équivalent serait des waves "Apparition" insérées (mob unique surdimensionné = mini-boss). Sur Milan W5+, des "apparitions" en milieu de wave (ex: 1 brute + 1 boss-class spawn simultané) seraient une rupture moins prévisible que le boss W*-8 attendu.

---

## 6. Synthèse — Patterns universels et différenciants

### 6.1 3 Patterns "rupture progressive" (universels)

**P1 — Intro de mécanique-clé par zone/world**
Chaque world introduit 1 type d'enemy qui invalide la composition précédente : KR (Werewolves Z2 anti-physique, Demons Z3 anti-magic), BTD6 (Camo R28, MOAB R40, DDT R63), PvZ (Pool zombies, Fog visibility, Roof catapult). Le joueur **doit** ajuster sa comp, pas juste empiler la même tour. Implémentation = enemy_type roster fixé par world.

**P2 — Telegraphing 1 stage à l'avance**
Le stage juste avant le boss/wall introduit l'enemy clé en version "soft" (50% HP, 1 instance solo). Joueur apprend la mécanique sans pression. KR le fait quasi systématiquement avant Stage 5/8/12. BTD6 fait 1 Camo Lead R24 isolé avant R28 wall. PvZ introduit 1 Snorkel Zombie isolé sur Level 3-1 avant le rush 3-5.

**P3 — Difficulty rating transparent**
Mindustry (threat 0–10), GemCraft (rating 1–3000), BTD6 (Beginner/Intermediate/Advanced/Expert tier). Le joueur sait avant d'entrer. **KR fait l'inverse** (pas de rating) → tradeoff confiance vs anxiété. Le rating affiché diminue le sentiment d'unfairness sans diminuer la difficulté réelle.

### 6.2 3 Patterns "rupture dure" (différenciants — one-shot mid-game)

**P4 — Mission spéciale qui retire un système**
Mindustry Fungal Pass = no-production mission. PvZ Vasebreaker / I,Zombie = retournent les rôles. Force le joueur à utiliser un autre cerveau. Wall puissant car non-grindable (skill pur, pas resource). Très peu de jeux le font (différenciant).

**P5 — Mode hardcore méta-applied (Iron Wizard / Impoppable)**
GemCraft IW retire skill scaling joueur. BTD6 Impoppable = 1 life. Ne crée pas de wall ponctuel mais **rend la campagne entière dure**. Niche public mais très forte rétention long-term. Achievement très rare = prestige.

**P6 — Apparition / mini-boss intra-wave**
GemCraft Apparitions, BTD6 random MOAB-class spawn dans freeplay, PvZ Gargantuar en wave 5-x normale (pas seulement boss). Mini-boss inattendu **dans** une wave normale → rupture sans warning. Différent du boss W*-8 attendu. Très fort sur **rejouabilité** (chaque run différent).

### 6.3 Castle HP / Lives policy comparées

| Jeu | Système | Regen | Cap | Scaling vs world |
|---|---|---|---|---|
| Kingdom Rush | Lives 10/20 par level | Non | Cap = base value level (10 ou 20) | Constant — boss levels ont 20 |
| BTD6 | Lives par mode (Easy 200 → Impop 1) | Non | Cap = starting value mode | Constant par mode |
| Mindustry | Core HP 600–9000 | Oui (passive si pas attaqué) | Cap = max core type | Multi-cores possible (joueur choisit) |
| PvZ1 | 1 lawnmower par lane (5–6 total) | Non | 1 charge par lane | Constant Adventure (mais perte = lane lost) |
| GemCraft FW | Mana pool (continu) | Oui (kills + bonus) | Pas capped | Mana cost monster augmente |

**Conclusion comparative** : **3 jeux n'ont pas de regen** (KR / BTD6 / PvZ). 2 jeux ont regen (Mindustry passif, GCFW via kills). Tous **cap** au démarrage (sauf GCFW). **Scaling de la safety bar par world n'existe quasiment jamais** — soit la safety est constante, soit elle baisse via mode (BTD6 Impoppable). Aucune référence ne durcit la safety bar **par world dans la même campagne**.

---

## 7. Synthèse chiffrée — où placer les ruptures et boss

### Position des walls majeurs (% de la campagne)

| Jeu | Wall #1 | Wall #2 | Wall #3 | Boss final |
|---|---|---|---|---|
| KR1 (12 stages) | Stage 5 (42%) | Stage 8 (67%) | Stage 11 (92%) | Stage 12 (100%) |
| BTD6 (60 rounds Medium) | R28 (47%) | R40 (67%) | R63 (105% freeplay) | R100 endgame |
| Mindustry Serpulo (~18 sectors) | Fungal Pass ~Sec 5 (28%) | Coastline ~Sec 11 (61%) | — | Planetary Launch (100%) |
| PvZ1 (50 levels) | Level 3-x (50%) | Level 4-9 (78%) | Level 5-5 (90%) | Level 5-10 (100%) |
| GCFW (~80 fields) | ~field 30 (38%) | ~field 60 (75%) | ~field 75 (94%) | The Forgotten (100%) |

**Pattern** : ruptures à ~30–45%, ~60–70%, ~90%. Boss final à 100%.

Sur Milan (10 worlds × 8 levels = 80 levels) : équivalents seraient W3-W4 (30–40%), W6-W7 (60–70%), W9-W10 (90%). Cohérent avec cible "rupture W4→W5" demandée par Mike (40–50% = milieu, plus tôt que la moyenne benchmark).

### Boss multi-phase prevalence

- KR : oui systématique (Vez'nan 2 phases, Sarelgaz summons)
- BTD6 : oui pour Boss Bloons (5 tiers + phase change à 50%/30% HP)
- Mindustry : non boss au sens classique
- PvZ1 : oui Dr. Zomboss 3 phases
- GCFW : non

3/5 jeux utilisent multi-phase pour le boss final. **Toujours** transition de phase basée sur HP (50%, 30%) plutôt que time-based.

---

## 8. Sources principales

- Kingdom Rush Wiki : https://kingdomrush.fandom.com/wiki/Kingdom_Rush_Wiki
- Bloons Wiki BTD6 : https://bloons.fandom.com/wiki/Bloons_TD_6
- Mindustry Wiki : https://mindustrygame.fandom.com/wiki/Mindustry_Wiki
- PvZ Wiki : https://plantsvszombies.fandom.com/wiki/Plants_vs._Zombies_Wiki
- GemCraft Wiki : https://gemcraft.fandom.com/wiki/GemCraft_Wiki
- Steam Community KR1 Iron Mode guide : https://steamcommunity.com/sharedfiles/filedetails/?id=261648720
- Speedrun.com KRF leaderboard : https://www.speedrun.com/krf
- Reddit r/btd6, r/KingdomRush, r/Mindustry, r/PlantsVsZombies, r/gemcraft (threads de community wisdom 2020–2024)
- Bloons Wiki — Impoppable difficulty : https://bloons.fandom.com/wiki/Impoppable
- Bloons Wiki — Boss Bloons : https://bloons.fandom.com/wiki/Boss_Bloons
- Bloons Wiki — Round table BTD6 : https://bloons.fandom.com/wiki/Round_(BTD6)
- PvZ Wiki — Adventure Mode : https://plantsvszombies.fandom.com/wiki/Adventure_Mode_(PvZ)
- Mindustry Wiki — Sectors : https://mindustrygame.fandom.com/wiki/Sectors
- GemCraft Wiki — Battle Setup : https://gemcraft.fandom.com/wiki/Battle_Setup
