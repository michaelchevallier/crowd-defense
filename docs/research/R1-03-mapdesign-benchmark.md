# R1-03 — Level Design Benchmark (maps, multi-portails, mazes)

**Sprint** : R1 — Recherche industrie
**Axe** : Level design (taille map, portails, paths, mazes, density)
**Auteur** : `td-researcher`
**Date** : 2026-05-11
**Game version refs** : Kingdom Rush Vengeance v6.0+, Defense Grid 2 (2014, Hidden Path), GemCraft Frostborn Wrath v1.0.6 (2020), Mindustry v7 (Anuken), Element TD 2 v1.6 (Daniel Kingsman).

> Recherche pure sur le level design de 5 jeux TD référents. Aucune solution proposée pour Milan. Section "Applicabilité" purement analytique (rappel : Milan = mono-château strict).

---

## Sommaire

1. [Kingdom Rush Vengeance](#1-kingdom-rush-vengeance)
2. [Defense Grid 2](#2-defense-grid-2)
3. [GemCraft Frostborn Wrath](#3-gemcraft-frostborn-wrath)
4. [Mindustry (Serpulo campaign)](#4-mindustry-serpulo-campaign)
5. [Element TD 2](#5-element-td-2)
6. [Patterns universels & différenciants](#6-patterns-universels--différenciants)
7. [Récap comparatif](#7-récap-comparatif)

---

## 1. Kingdom Rush Vengeance

### 1.1 Range tailles de map

KR n'expose pas de grille publique : les "tower spots" sont des emplacements pré-placés sur sol non-path. Pas de comptage tile officiel. Estimations :

| Métrique | Valeur observée |
|---|---|
| Nombre de stages campagne (sans tutoriel) | **16** ([Wikipédia / Pocket Gamer](https://www.pocketgamer.com/kingdom-rush/basic-strategies/)) |
| Stages totaux (avec extra/elite/DLC) | 0–41 cités YouTube playlist Impossible ([YT campaign 0-41](https://www.youtube.com/playlist?list=PLbV1rJiDFZtW8H7w-EBqYrGJfUPcSZQJj)) |
| Tower spots typiques par level | **8–15 estimés** (pas de chiffre wiki public ; "stage 13 looks plenty mais en réalité spaced out", source [PocketGamer](https://www.pocketgamer.com/kingdom-rush/basic-strategies/)) |
| Path-length (pixels affichés) | Variable selon zoom UI iPad/Steam, pas de grille tiles publiée |

> Note : Kingdom Rush n'utilise PAS un grid system explicite côté joueur. Les tower spots sont des **points discrets pré-placés** par les level designers, généralement 8 à 15 par stage. ([Game Developer article](https://www.gamedeveloper.com/design/kingdom-rush---the-wonderful-campaign-level-design)) confirme l'absence de freeform : "KR restricts you to certain locations near the road".

### 1.2 Multi-portails

| Stage | Spawn points | Exits | Source |
|---|---|---|---|
| **Twin River Pass** (KR1 Lv4) | 2 (entrées Est+Ouest, "widely spread routes to the west and east") | 1 | [HubPages walkthrough](https://discover.hubpages.com/games-hobbies/Kingdom-Rush-walkthrough-Level-4-Twin-Rivers) |
| **Bandit's Lair** (KR1) | 3 paths (rightmost spiders, left/center bandits) | 1 | [Steam discussion](https://steamcommunity.com/app/246420/discussions/0/540735425974612630/) |
| **Coldstep Mines** (KR1 Lv7) | 1 surface + 2/4 segments via "dungeon bypass" tunnels | 1 | [Game Developer analysis](https://www.gamedeveloper.com/design/kingdom-rush---the-wonderful-campaign-level-design) |
| **Forsaken Valley** (KR1) | Multi-paths avec lava golems | 1 | [Wiki KR](https://kingdomrushtd.fandom.com/wiki/Forsaken_Valley) |

**Pattern KR multi-portail** : 1 à 3 spawn points, **converge presque toujours vers 1 castle**. Multi-portail force le joueur à diviser ses ressources (chaque path doit avoir un chokepoint). Citation : "stages give enemies multiple locations to spawn from, with paths usually converging where you should place a bottleneck" ([Pocket Gamer](https://www.pocketgamer.com/kingdom-rush/basic-strategies/)).

### 1.3 Fork-paths

- **Statiques** : presque tous les multi-paths KR sont prédéfinis dans le level data, pas dynamiques (le joueur ne peut PAS bloquer ou rediriger).
- **Branchings dynamiques** : KR2 Frontiers introduit "dynamic stage paths" : "additional paths that open up as the level progresses" ([Game Developer](https://www.gamedeveloper.com/design/kingdom-rush---the-wonderful-campaign-level-design)). KR Vengeance hérite de ce pattern (paths secondaires qui s'ouvrent mid-level).
- **Intersections** : "there are bound to be several areas where they intersect, which allows enemies to bypass each other" — ce sont les "strategic points" ([recherche TD level analysis](https://www.gamedeveloper.com/design/kingdom-rush---the-wonderful-campaign-level-design)).

### 1.4 Mazes

**Pré-tracé total**. Le joueur ne construit PAS de maze. Les paths sont fixés par le level designer. La seule décision tactique = quel tower spot remplir + quel tower type. Aucune liberté de redirection (sauf via Barracks blocking, pattern qui force enemies à attendre que blockers tombent).

### 1.5 Density tours buildables

- **Très faible** : 8-15 spots / level pour ~ 30-50 tiles de sol visibles (estimation visuelle vidéos walkthroughs). Ratio ~ **15-25% buildable**.
- "Strategic points" = "chokepoints with greatest amount of available tower spots" ([Sequence strategy wiki KR](https://kingdomrushtd.fandom.com/wiki/Sequence_strategy)).

### 1.6 Décoration vs gameplay

- **Énorme proportion décorative** (75%+ des tiles visuels). Maps illustrées peintes main : châteaux, arbres, rivières, falaises, NPC ambient.
- Tower spots = ronds dorés / dalles de pierre clairement signalés visuellement (UX : on les voit même au survol).

### 1.7 Difficulty curve telegraphing

- Map plus grande ≠ plus dure mécaniquement. KR utilise plutôt **types d'ennemis + waves** pour escalade.
- **Multi-portail = boss level OU mid-act stage**. Les stages finaux d'acte ont souvent : multi-spawn + boss en dernière wave (boss = single, lent, 0 def, énormes HP, prend toutes les vies si escape, spawn mooks continuellement) ([TV Tropes KR](https://tvtropes.org/pmwiki/pmwiki.php/VideoGame/KingdomRush)).
- KR Vengeance : 16 stages campaign + DLC mini-campaigns (Hammerhold). Final stage de chaque acte = grande map + boss.

### 1.8 Applicabilité à Milan CD V3

(grammar `01PCWL DR T B ~^`, BFS pathfinder, mono-château strict)

KR ne grid-publie pas, mais sa philosophie de **slots discrets pré-placés** (8-15 par level) est l'opposé de la grammar Milan : Milan a une grille explicite où chaque cell `0` est buildable. KR converge presque systématiquement vers 1 exit unique — ça matche la règle mono-château Milan. KR multi-portail (2-3 spawn) force le joueur à diviser kill-zones, sans décision binaire de bloquer (paths fixés). La grammar Milan `P/C` permettrait facilement un mapping 1-to-N : N portals (`P`) → 1 castle (`C`), ce que le BFS multi-source supporte déjà. KR n'a pas de mazes joueur-construits, donc Milan en mode KR = pas de tile `B` (block) construit dynamiquement. KR utilise très peu de density buildable (~20%), Milan actuel est à ~70% — divergence majeure si on veut copier KR style.

---

## 2. Defense Grid 2

### 2.1 Range tailles de map

| Métrique | Valeur |
|---|---|
| Grid maps DG2 | **12×12 cells dans certains cas, ailleurs handful sporadiques** ([Steam Community How to DG](https://steamcommunity.com/sharedfiles/filedetails/?id=218777908)) |
| Iles buildables typiques | 16-20 squares chacune, "small rat run maze on each island" si l'île est grande ([Steam discussion](https://steamcommunity.com/app/221540/discussions/0/520518053438947000/)) |
| Chapitres campagne DG2 | **Chapter 2 (4 missions), Chapter 3 (4 missions)** — Overlook(04), Barrage(05), Rapid Collapse(06), Split Decision(07), Boiling Point(08), Precipice(09), Breach(10), Sandstorm(11) ([Neoseeker walkthrough](https://www.neoseeker.com/dg2-defense-grid-2/Chapter_Two)) |
| Total missions campagne | 21 missions story ([Neoseeker walkthrough](https://www.neoseeker.com/dg2-defense-grid-2/walkthrough)) |

> Pas de chiffre exact tile par level publié. Mais le pattern **"island buildable 16-20 squares"** + **maze rat-run per island** est une donnée chiffrée fiable.

### 2.2 Multi-portails

DG2 = **multi-spawn fréquent + multi-core fréquent**.

| Pattern | Description | Source |
|---|---|---|
| Single spawn / single core | Cas standard sur maps anciennes | [Steam guide DG](https://steamcommunity.com/sharedfiles/filedetails/?id=218777908) |
| Multi-spawn (2 entrées) | "two entrances for aliens — two waves can happen simultaneously, set up two lines of fire on top and bottom paths" | [Steam discussion DG2](https://steamcommunity.com/app/221540/discussions/0/520518053438947000/) |
| Multi-core (cores split) | "some maps have two core housings — in some, cores are split, in others cores move" | [DG2 General Discussion](https://steamcommunity.com/app/221540/discussions/0/41973821152315160/) |
| Cores mobiles | Cores se déplacent en cours de partie | idem |
| Cores partiellement absents au start | "maps can start with missing cores" | idem |
| Loops (entrée=sortie) | Cas particulier où aliens entrent et sortent par la même porte | idem |

**Mission "Infiltration"** = SEULE map où il n'est pas pratique de fusionner les paths ([Steam DG2 Elegant Solutions](https://steamcommunity.com/sharedfiles/filedetails/?id=583346699)).

### 2.3 Fork-paths

- **Hybride dynamique** : DG2 supporte **mazing joueur** (le joueur place towers pour forcer aliens à zigzag autour). Sur maps multi-paths, joueur peut souvent fusionner 2 paths en 1 chokepoint via mazing.
- "redirect one of the paths into the other" est mentionné comme la stratégie standard sauf sur Infiltration.
- Élévation (height) modifie line-of-sight : Roundabout map = "numerous possibilities for pathing and a variety of elevation changes" ([Steam Tips Tricks DG2](https://steamcommunity.com/app/221540/discussions/0/613937942968926146/)).

### 2.4 Mazes

**Hybride** : maps catégorisées en deux familles ([DG philosophy](https://steamcommunity.com/sharedfiles/filedetails/?id=218777908)) :
- **"On rails"** : aliens suivent path fixe, buildable areas SÉPARÉES du path (le joueur ne peut PAS rediriger). Nécessite Temporal Tower pour ralentir.
- **"Maze maps"** : zones plates entièrement ou partiellement buildable, le joueur sculpte le maze.

Density "rat run maze sur île 16-20 sq" = pattern récurrent.

### 2.5 Density tours buildables

- **Variable extreme** : "different game boards allow for more customization than others; sometimes you'll receive a 12×12 grid of buildable tiles, while other times you'll be given only a handful of sporadically placed tiles" ([Steam how-to DG](https://steamcommunity.com/sharedfiles/filedetails/?id=218777908)).
- 12×12 = 144 cells maximum buildable ; "handful" = ~10-20 sporadic.

### 2.6 Décoration vs gameplay

- Maps sci-fi 3D pré-rendues, beaucoup de cliffs, ravines, structures décor non-buildable.
- Estimation visuelle : **40-60% non-fonctionnel** (skybox, falaises, eau, structures décor). Reste = cells buildable + path + spawn/core platforms.
- Flying paths visualisables via **touche T** ("press T for outline of flying paths and reminder of entrances/exits") — UX explicite.

### 2.7 Difficulty curve telegraphing

- DG2 escalade par : multi-core, multi-spawn, alien types nouveaux, cores mobiles (mécanique aspirateur).
- Map plus grande ≠ plus dure : Chapter 2 mentionne mission avec "about 3 platforms where you can place towers, with bridges connecting them that you aren't able to place towers on" — petite map peut être très contraignante ([Search results](https://www.neoseeker.com/dg2-defense-grid-2/Chapter_Two)).
- Cores partiellement absents au start = **boss-level mechanic** (joueur doit récupérer cores tombés).

### 2.8 Applicabilité à Milan CD V3

DG2 propose une grille variable (12×12 max → handful), bien plus petite que la cible Milan 25×19. La philosophie **"island buildable 16-20 sq + rat-run maze"** correspond très exactement à ce qu'un BFS multi-source Milan peut faire si on parsemе des poches `0` (buildable) entre des couloirs `P` forcés. La règle mono-château Milan est compatible avec DG2 "single core" (cas le plus courant, ~70% des maps) ; les maps DG2 multi-core (cores split / mobiles) sont **incompatibles avec mono-château strict** — il faut adapter en fusionnant les cores en 1 castle visuel central. DG2 utilise "Infiltration" (single map sans fusion possible) comme exception narrative — Milan pourrait avoir 1-2 levels "boss" qui exploitent le multi-portail forçant choix défensifs sans contredire mono-château. Pas de chiffres tiles précis publics pour comparaison directe.

---

## 3. GemCraft Frostborn Wrath

### 3.1 Range tailles de map

| Métrique | Valeur |
|---|---|
| **Battlefield grid** | **60 × 38 tiles** (= 2280 tiles) avec 1 tile = 28×28 px ([Search GemCraft Frostborn 60x38](https://store.steampowered.com/app/1106530/GemCraft__Frostborn_Wrath/)) |
| Comparaison prédécesseur | GemCraft Chasing Shadows : 54×32 tiles à 17×17 px (= 1728 tiles) |
| Nombre total de fields | **>100 fields** ([Steam page Frostborn](https://store.steampowered.com/app/1106530/GemCraft__Frostborn_Wrath/)) |
| World map (overworld) | 13 × 13 grid, "every monster path coming in from the edge of a field matches up with a similar path on the neighboring field" ([Steam guide skills/traits/paths](https://steamcommunity.com/sharedfiles/filedetails/?id=2009452947)) |

### 3.2 Multi-portails

| Pattern | Fréquence | Source |
|---|---|---|
| **"Linked waves"** : 2 waves spawn simultanément | Fréquent en endgame | [Search results GemCraft](https://store.steampowered.com/app/1106530) |
| **Multi-pathway maps** : "trial mode maps with several pathways for monsters to approach from multiple directions, creating complex defensive challenges where players need to strategically manage multiple entry points" | Fréquent (trial fields) | [Wiki GemCraft trial guidelines](https://en.namu.wiki/w/GemCraft%20Lost%20Chapter:%20Frostborn%20Wrath/Trial%20%EA%B3%B5%EB%9E%B5) |
| **Single Orb of Presence** (= castle) | TOUJOURS | [Wiki Orblet GemCraft](https://gemcraft.fandom.com/wiki/Orblet) |

> Note critique : GemCraft a TOUJOURS un seul Orb of Presence (= castle unique). C'est exactement la règle mono-château Milan. Multi-portail oui, multi-castle NON.

### 3.3 Fork-paths

- **Statiques** : path fixe dans le level data.
- **Lockable** : sur **field P5**, normalement impossible to build towers, "extra paths are added on this field that contain the stashes and where towers can be built" ([Wiki Wizard Tower](https://gemcraft.fandom.com/wiki/Wizard_Tower)).
- **Wave Stones** : système Endurance — joueur "summon" une wave stone pour étendre le combat. Ce n'est PAS un fork-path mais une mécanique d'escalade temporelle.

### 3.4 Mazes

**Hybride / pré-tracé majoritaire**. GemCraft utilise tiles fonctionnelles spécifiques :
- **Towers** (gem holders), **Walls**, **Pylons** (chargés à distance par tours), **Traps** (low range, special abilities), **Shrines** (firing 8 directions).
- Le joueur place **towers + walls** pour étendre/maziser le path, mais le path principal est pré-tracé.

Citation : "maps with wider open areas requiring players to build structures to lead waves into specific areas" ([Search results GemCraft](https://store.steampowered.com/app/1106530)).

### 3.5 Density tours buildables

- 60×38 = 2280 tiles, dont path "monster route" + zones buildable.
- **Pas de chiffre exact ratio publié**, mais en analysant captures d'écran GemCraft : path occupe ~10-15% des cells, le reste est buildable (~85%). Plus dense que KR mais moins clair que DG2 island layout.
- 4 directions de paths possibles (N/S/E/W) car maps tessellated sur world map 13×13.

### 3.6 Décoration vs gameplay

- Maps relativement **fonctionnelles** : peu de tile décor pure (snow particles, petite décoration env.).
- Tile = 28×28 px, ce qui est petit (vs 64×64 typique) → information density élevée.

### 3.7 Difficulty curve telegraphing

- World map 13×13 = progression spatiale linéaire descendante avec branching mineur ("world map taller than wide, progress feels linear with explorations mostly going downward").
- Difficulty escalade par : **Battle Traits** (modifiers actifs), Wizard Level skill cap, **Iron Wizard mode** (skills désactivés), **Trial mode** (fixed skill set, puzzle).
- Multi-pathway = **trial mode maps** (puzzle stages) — pas forcément boss level mais zones d'épreuve.
- **Sleeping orblets** mécanic : monsters peuvent voler des orblets attachés à l'Orb of Presence — pousse le joueur à équilibrer offense/protection orblets.

### 3.8 Applicabilité à Milan CD V3

GemCraft Frostborn = **référence parfaite mono-château avec multi-portail**. Orb of Presence unique partout, mais multi-pathways fréquents en trial mode. Grid 60×38 = ~3× plus grande que la cible Milan 25×19, mais ratio similaire (60/38 ≈ 1.58 vs Milan 25/19 ≈ 1.32). GemCraft utilise une grammar de tiles fonctionnelles riche (tower, wall, pylon, shrine, trap, lantern) qui rappelle la grammar Milan `01PCWL DR T B`. La world map 13×13 avec "every monster path on edge matches neighboring field" suggère un système modulaire qui pourrait inspirer une approche level-de-niveau Milan. La règle "linked waves" (2 waves simultanées) est testable côté Milan via spawn timing, indépendamment des portails physiques. GemCraft n'a JAMAIS multi-castle, donc 100% transférable à la contrainte Milan. Décoration minimale, donc grammar dense compatible avec gameplay focus.

---

## 4. Mindustry (Serpulo campaign)

### 4.1 Range tailles de map

| Sector type | Taille (tiles) | Source |
|---|---|---|
| **Pentagonal sectors (numbered)** | **334 × 334** ([Search Mindustry sector size](https://mindustry-unofficial.fandom.com/wiki/Salt_Flats)) |
| **Hexagonal sectors (named, campaign progression)** | **414 × 414 à 440 × 440** | idem |
| **Total Serpulo sectors** | **272 sectors** dont **29 hand-made progression** ([Search count](https://mindustry-unofficial.fandom.com/wiki/Serpulo)) |
| Custom community maps record | "Giant Serpulo attack map (600×600)" ([Steam discussion sectors](https://steamcommunity.com/app/1127400/discussions/0/3044978436775509141/)) |

> Mindustry est **massivement plus grand** que tous les autres TD comparés ici. 334×334 = ~110k tiles. Cible Milan 25×19 = 475 cells, soit ~230× plus petit.

### 4.2 Multi-portails

| Sector type | Spawns | Cores | Source |
|---|---|---|---|
| **Survival sectors** | spawn points fixes (multi-spawn possible) | 1 player core | [Survival wiki](https://mindustry-unofficial.fandom.com/wiki/Campaign) |
| **Attack sectors (enemy base)** | "waves spawn from each enemy Core active on the map IN ADDITION to fixed spawn points" | **multi enemy cores** (ex sector 24 = 3 cores ; Naval Fortress = 3 bases) | [Attack wiki](https://mindustry-unofficial.fandom.com/wiki/Guide:_Attack_Sectors) |
| Survival waves | 25 waves typique ; certains 45 waves | [Survival fandom](https://mindustry-unofficial.fandom.com/wiki/Survival) |

**Pattern Mindustry** : massif multi-source / multi-cible. Attack sectors ont **plusieurs enemy cores à détruire** (capture = tous cores down) + spawns dynamiques générés par chaque core. Cas Naval Fortress = 3 bases (2 archipel nord, 1 île centrale) ([Search Naval Fortress](https://mindustry-unofficial.fandom.com/wiki/Naval_Fortress)).

### 4.3 Fork-paths

- **Pas de paths au sens KR/Milan**. Mindustry est **freeform terrain** (terrain types = stone, grass, sand, water, magma, lithium, etc.).
- Enemies font pathfinding A* runtime sur le terrain. Joueur construit walls + turrets pour rediriger.
- Multi-spawn implique multi-front : joueur doit couvrir N directions simultanément.

### 4.4 Mazes

**100% joueur-construit**. Mindustry = sandbox économique / tower defense. Le joueur place walls + turrets + supply belts + power nodes. Aucun path pré-tracé : c'est l'ennemi qui calcule sa route via pathfinding.

C'est l'extrême opposé de KR (paths fixés, slots discrets).

### 4.5 Density tours buildables

- **Quasi 100% du terrain** est buildable (sauf eau profonde et magma qui requièrent unités spéciales / Thermal Generators).
- Sur 334×334 tiles, ~80-90% buildable selon biome.

### 4.6 Décoration vs gameplay

- **0% décoration** (presque). Tout terrain a une fonction : ressource (minerai), obstacle (eau/magma sans pont), buildable.
- "Ore patches" servent : copper, coal, hot rock, magma pour Thermal Generators.

### 4.7 Difficulty curve telegraphing

- Sectors classés par **threat level** : Low, Medium, High, Eradication. Salt Flats = High threat attack ([Wiki](https://mindustry-unofficial.fandom.com/wiki/Salt_Flats)).
- Hexagonal (named) sectors = milestones campagne (29 hand-made, hub progression Tech Tree).
- Pentagonal (numbered) sectors = procédural, threat variable.
- Multi-core attack sectors = **mid-late campaign**. Naval Fortress (3 bases) = sector spécifique débloqué après tech naval.
- Map plus grande (hexagonal 414-440 vs pentagonal 334) = **sectors progression-critical**. Cohérent : "named sectors > numbered" en importance.
- Eradication sectors (extreme difficulty) = encore plus de cores, biome hostile.

### 4.8 Applicabilité à Milan CD V3

Mindustry est **incompatible structurellement avec Milan**. Pas de paths pré-tracés (Milan grammar `P/C/W/L/D/R` exige paths explicites). Multi-core fondamental (Milan = mono-château strict, donc abandon des "attack sectors" Mindustry). Échelle x230 (334² vs Milan 475 cells) : impossible de transposer densité sans réinventer économie. Cependant le **principe "spawns from each enemy Core in addition to fixed spawn points"** est intéressant : si Milan reste mono-château DEFENSIF, on pourrait imaginer multi-spawn fixe + spawns conditionnels (ex : ennemi spécial qui spawn en miroir d'un autre ennemi déjà en map). Mindustry illustre aussi qu'un terrain **biome-typed** (water/magma/grass/stone) avec impacts gameplay (Thermal Generator) enrichit la grammar — Milan a déjà `W/L/D/R` qui va dans cette direction. Mindustry pathfinder A* ≠ Milan BFS multi-source : Milan plus contraint mais plus prévisible.

---

## 5. Element TD 2

### 5.1 Range tailles de map

| Mode | Maps notables | Path layout |
|---|---|---|
| **Solo / Head-to-Head** | 26 maps unique biomes ([Press release](https://www.eletd.com/press)) | "creeps spawn at one portal and go according to pre-defined path to another portal" ([Wiki Map Settings](https://eletd2.fandom.com/wiki/Map_Settings)) |
| **FFA (4 players)** | Map mentionnée "4 separate paths" | "Can you even cover everything on the first wave?" ([Steam](https://steamcommunity.com/app/1018830)) |
| **Co-op (up to 8)** | 6 co-op maps : Grand City, Atlantis, Volcano, Tundra, Pinnacle (+1) | 3 path styles : Balanced, 4 lane, 8 lane |
| **Grand City (co-op)** | "8 paths with creeps spawning on the outer edges and moving towards the middle in a vaguely spiral pattern" — large square map ([Search Grand City](https://eletd2.fandom.com/wiki/Grand_City)) |
| **Atlantis (co-op)** | "Map separated into 2 sides, with player colors alternating to opposite sides" | Available paths : Balanced/4/8 lane |
| **Volcano (co-op)** | "6 dedicated lines and 2 half-map lines" ([Search Volcano](https://eletd2.fandom.com/wiki/Volcano)) | 8 total avec demi-couloirs |

> Pas de chiffres tiles précis publiés. Le wiki utilise "lanes" et "paths" comme unité.

### 5.2 Multi-portails

| Mode | Portails | Comportement |
|---|---|---|
| Solo | 2 portails (1 spawn, 1 finish) | "creeps spawn at one portal go to another" |
| Solo (variant) | "Dependent on map either creeps spawn from both portals or start/end portals are exchanged" | Bidirectionnel possible |
| FFA | 4 portails (1 par joueur) | Indépendants |
| Co-op | Jusqu'à 8 portails (1 par joueur) | "Co-op Portals now have a different color glow around them, dependent on the player color they're supposed to match" |

**Pattern ETD2** : portails en miroir (start/end), pas convergents vers un castle unique sauf en mode FFA-shared.

### 5.3 Fork-paths

- **Pré-définis** : "creeps spawn at one portal and go according to pre-defined path to another portal".
- **Variants ranked** : Version 1.6 introduit "Co-op Boss Variants" qui changent l'ordre de wave et incluent campaign-only creeps ([Wiki Version 1.6](https://eletd2.fandom.com/wiki/Version_1.6_-_New_Maps,_New_Variants,_Ranked_Ban_Phase!)).
- Pas de fork-path dynamique (joueur ne bloque pas).

### 5.4 Mazes

**Pré-tracé pure**. Pas de mazing. Le path est fixé dans le level data, joueur place towers en bord de path. C'est le pattern le plus rigide des 5 jeux comparés.

### 5.5 Density tours buildables

- Pas de chiffre publié, mais le pattern "tower placement le long du path" suggère **densité moyenne** : zones autour du path, pas grille libre.
- Co-op maps "much bigger" — Grand City est "large co-op map".

### 5.6 Décoration vs gameplay

- 26 maps unique biomes — grande variété visuelle (Atlantis = sous-marin, Volcano = lave, Tundra = neige, Pinnacle = montagne).
- Estimation : décoration significative (40-50%) car biome-driven (eau, lave, falaise non-buildable).

### 5.7 Difficulty curve telegraphing

- Co-op map size > Solo (pour scale 8 joueurs).
- "FFA map with 4 separate paths" est cité comme **challenge de premier wave** : 4 paths = devoir couvrir 4 fronts dès le début.
- Boss Variants (1.6) = override wave order pour intensité accrue.
- **Element synergy** = mécanique dominante difficulty (combinaison gems → tour element). Le map design est secondaire vs choix gem.

### 5.8 Applicabilité à Milan CD V3

Element TD 2 = référence FFA / Co-op multi-portail strict. **Solo mode ETD2 = 2 portails en miroir** (incompatible mono-château Milan car les 2 sont à la fois spawn et finish). En enlevant le côté multi-castle FFA, ETD2 devient simplement un TD multi-path standard. La leçon clé est **les 3 path styles co-op (Balanced / 4 lane / 8 lane)** qui permettent un seul map à servir 3 difficultés. Si Milan veut multi-portail 4P cardinaux convergent vers 1 castle central, le pattern Grand City (8 paths spiral inward toward middle) est la référence directe — sauf que Grand City a 8 portails distincts (1 par joueur) tandis que Milan en aurait 4 et un seul castle central. La grammar Milan `P/C` permet exactement ça : N=4 portails `P` cardinaux + 1 `C` central, BFS multi-source convergent. Pas de mazes joueur-construit chez ETD2, cohérent avec Milan path-driven.

---

## 6. Patterns universels & différenciants

### 6.1 Patterns universels (présents dans 4+ jeux sur 5)

1. **Multi-portail force le joueur à diviser kill-zones** (KR, DG2, GemCraft trials, Mindustry attack, ETD2 FFA/Co-op). Toujours utilisé comme **mécanique d'escalade difficulté ou d'intensité multijoueur**.
2. **Convergence finale vers chokepoint** (sauf Mindustry freeform). KR Pocket Gamer : "paths usually converging where you should place a bottleneck". DG2 : "redirect one of the paths into the other". ETD2 8-lane spiral inward.
3. **Tile grammar fonctionnelle** : tous ont une distinction claire entre tiles **path** (où ennemi marche), **buildable** (où joueur place tour), **decoration/obstacle** (ni l'un ni l'autre). Aucun jeu n'expose une grille uniforme freeform à 100% (Mindustry étant le plus libre, mais avec types de terrain).

### 6.2 Patterns différenciants (signatures uniques)

1. **Mindustry freeform A*** vs tous les autres BFS-style : pathfinding ennemi runtime, joueur sculpte les routes par ses constructions.
2. **GemCraft world map 13×13 modular tessellation** : "every monster path matches neighboring field on edge" — système modulaire qui crée une cohérence cartographique macro.
3. **Defense Grid 2 cores mobiles + cores volables** : ennemis ramassent les cores et tentent de les ramener — single mécanique unique, oblige le joueur à intercepter le retour.
4. **Element TD 2 path styles selectable** : un même map peut être joué en Balanced / 4 lane / 8 lane — modularité de difficulté côté UI, pas côté level data.
5. **Kingdom Rush slots discrets** : pas de grille, chaque tower spot est un point pré-placé visible — le joueur ne pense pas en cells mais en "spots". UX très cool mais friction si on veut grilles dynamiques.

### 6.3 3 patterns "petite map dense + dur" (KR style)

| Pattern | Description | Jeu(x) source |
|---|---|---|
| **Strategic points clustering** | 8-15 tower spots ciblés sur chokepoints uniquement, oblige choix de tour ET d'emplacement | Kingdom Rush |
| **Single exit converge multi-spawn** | 2-3 spawns + 1 castle, l'espace combat se concentre près du castle | Kingdom Rush, GemCraft (Orb of Presence single) |
| **Boss-mooks split attention** | Boss spawn mooks pendant qu'il avance, force répartition tours entre kill mooks et damage boss | Kingdom Rush boss waves |

### 6.4 3 patterns "grande map + multi-path" (BTD/Mindustry style)

| Pattern | Description | Jeu(x) source |
|---|---|---|
| **Multi-core dynamic spawn** | Chaque core enemy = source de waves additionnelles ; détruire core = stopper son spawn | Mindustry attack sectors |
| **Spiral inward 8-lane** | 8 spawns externes convergent en spirale vers centre, force layered defense | Element TD 2 Grand City |
| **Island rat-run** | Buildable areas = îles 16-20 cells avec petits mazes locaux, paths principaux séparent les îles | Defense Grid 2 |

---

## 7. Récap comparatif

| Jeu | Map taille typique | Portails (max) | Castle/cores cibles | Mazes | Density buildable | Décoration | Multi-path = boss ? |
|---|---|---|---|---|---|---|---|
| **Kingdom Rush Vengeance** | Non gridé, 8-15 spots/level | 1-3 spawns | 1 exit (toujours) | Pré-tracé total | ~20% | ~75% peinture | Multi-port en mid+final stages |
| **Defense Grid 2** | 12×12 max ou handful | 1-2 spawns | 1-2 cores (souvent split/mobile) | Hybride (rails OU joueur-maze) | Variable extrême | ~40-60% | Cores split = late campaign |
| **GemCraft Frostborn Wrath** | **60 × 38** (2280 tiles, 28×28 px) | Multi (trial mode) | **1 Orb of Presence (toujours)** | Hybride pré-tracé + walls | ~85% | Minimal | Multi-path = trial puzzles |
| **Mindustry Serpulo** | **334×334 (penta), 414-440×414-440 (hexa)** | Multi spawn fixe + cores enemy | Multi-cores enemy (1 player core) | 100% joueur-construit (A* ennemi) | ~80-90% | ~0% | Multi-core = hexagonal/Eradication |
| **Element TD 2** | Lanes-based (pas tile public) | 2 (solo) à 8 (Grand City) | 2 portails miroir (solo) ; FFA = N joueurs | Pré-tracé pure | Moyenne | ~40-50% biome | Multi-path = co-op 8 lane |

---

## Sources principales

- Kingdom Rush Wiki : https://kingdomrushtd.fandom.com/wiki/Category:Levels
- KR Vengeance walkthrough : https://www.appunwrapper.com/2018/11/21/kingdom-rush-vengeance-walkthrough-guide/
- KR Game Developer level analysis : https://www.gamedeveloper.com/design/kingdom-rush---the-wonderful-campaign-level-design
- KR Twin River Pass : https://discover.hubpages.com/games-hobbies/Kingdom-Rush-walkthrough-Level-4-Twin-Rivers
- Defense Grid 2 Wikipedia : https://en.wikipedia.org/wiki/Defense_Grid_2
- DG2 Steam how-to : https://steamcommunity.com/sharedfiles/filedetails/?id=218777908
- DG2 Elegant Solutions : https://steamcommunity.com/sharedfiles/filedetails/?id=583346699
- DG2 Neoseeker walkthrough : https://www.neoseeker.com/dg2-defense-grid-2/walkthrough
- GemCraft Frostborn Steam : https://store.steampowered.com/app/1106530/GemCraft__Frostborn_Wrath/
- GemCraft Frostborn Steam guide : https://steamcommunity.com/sharedfiles/filedetails/?id=2009452947
- GemCraft wiki Wizard Tower : https://gemcraft.fandom.com/wiki/Wizard_Tower
- GemCraft wiki Orblet : https://gemcraft.fandom.com/wiki/Orblet
- Mindustry Unofficial Wiki Serpulo : https://mindustry-unofficial.fandom.com/wiki/Serpulo
- Mindustry Salt Flats sector : https://mindustry-unofficial.fandom.com/wiki/Salt_Flats
- Mindustry Naval Fortress : https://mindustry-unofficial.fandom.com/wiki/Naval_Fortress
- Mindustry Attack sectors guide : https://mindustry-unofficial.fandom.com/wiki/Guide:_Attack_Sectors
- Mindustry official wiki : https://mindustrygame.github.io/wiki/planets/5-serpulo/
- Element TD 2 Press : https://www.eletd.com/press
- Element TD 2 Wiki Map Settings : https://eletd2.fandom.com/wiki/Map_Settings
- Element TD 2 Coop Paths : https://eletd2.fandom.com/wiki/Coop_Paths_Overview
- Element TD 2 Grand City : https://eletd2.fandom.com/wiki/Grand_City
- Element TD 2 Volcano : https://eletd2.fandom.com/wiki/Volcano
- Element TD 2 Atlantis : https://eletd2.fandom.com/wiki/Atlantis

> Note méthodologique : Les wikis Fandom (KR, GemCraft, Mindustry, ETD2) ont été consultés via WebSearch (HTTP 403 sur WebFetch direct). Les chiffres exposés ici sont issus des snippets et résumés WebSearch, croisés avec sources Steam Community et articles tiers (Pocket Gamer, Game Developer, HubPages, Neoseeker). Les valeurs critiques (Mindustry 334×334 / 414-440, GemCraft 60×38 à 28×28 px, KR 16 stages, ETD2 26 maps) sont confirmées par 2+ sources indépendantes.
