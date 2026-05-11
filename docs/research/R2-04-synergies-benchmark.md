# R2-04 — Synergies + Upgrade Trees : Benchmark TD industrie

**Persona** : `td-researcher`
**Date** : 2026-05-11
**Mission** : étudier 5 références TD majeures sur leurs **modèles d'upgrade** + **synergies inter-tours**, alimenter D1-03 (Milan CD V3 — passage L3 hybride paywall + choix binaire pour archer/mage/ballista/cannon).
**Out of scope** : aucune solution Milan, aucun code touché. Recherche pure.

---

## 1. Bloons TD 6 (BTD6)

### 1.1 Modèle d'upgrade — 3 paths × 5 tiers + Paragon

Chaque tour a **3 upgrade paths** distincts (Top, Middle, Bottom) qui partent du même niveau de base, chacun jusqu'à Tier 5 (5 upgrades max par path). Source : [Bloons Wiki — Upgrade Path](https://bloons.fandom.com/wiki/Upgrade_Path).

**Règle "5-2-0" (Crosspath)** : sur les 3 paths, le joueur ne peut investir au-delà du Tier 2 que dans **un seul** path. Les 2 autres sont plafonnés au Tier 2 maximum, et un seul des deux peut atteindre Tier 2 si l'autre dépasse Tier 0. Notation : `X-Y-Z` où la somme des deux non-principaux ≤ 2 et au moins l'un d'eux est 0. Exemples valides : `5-2-0`, `5-0-2`, `2-3-0`, `0-5-2`. Invalides : `3-3-0`, `2-2-1`. Source : [Bloons Wiki — Crosspathing](https://www.bloonswiki.com/Upgrade), confirmation Steam discussion BTD6.

### 1.2 Coûts par tier (medium, hors MK / discount)

| Tier | Range coût observé (USD in-game) | Exemples notables |
|---|---|---|
| T1 | 100 – 600 | Sharp Shots (Dart Monkey) 140, Quicker Shooting 170 |
| T2 | 200 – 1 500 | Razor Sharp Shots 425 |
| T3 | 600 – 3 500 | Triple Shot 1 245 |
| T4 | 2 000 – 30 000 | Crossbow Master 25 000 |
| T5 | **15 000 – 120 000** | Ultra-Juggernaut 15 000 ; MOAB Eliminator 25 500 ; Spirit of the Forest 50 000 ; Bloon Exclusion Zone 54 000 ; Flying Fortress 120 000 ; True Sun God 500 000 (cas limite, 4 sacrifices) |

Sources : [Bloons Wiki — Tier 5 Upgrades](https://bloons.fandom.com/wiki/Tier_5_Upgrades), [Steam Discussion — Cheapest/Most Expensive T5](https://steamcommunity.com/app/960090/discussions/0/3162083441790648192/).

**Ratio T4→T5** : ×3 à ×10 selon la tour, avec une **rupture économique** majeure (paywall T5).

### 1.3 Spécialisation — choix triple, exclusif

À partir du Tier 3, le joueur **doit** choisir UNE direction. La règle 5-2-0 force un **path principal + path mineur (max T2)** + path tertiaire fermé. Les 3 paths d'une même tour sont **fonctionnellement très différents** (ex. Dart Monkey : Top = vitesse, Middle = puissance/explosif, Bottom = coopération/buff). Pas de double-spé possible.

### 1.4 Anti-degenerate

- **Tier 5 unique** : seulement **1 Tier 5 par path par joueur** (cap simultané) sur la map. Source : [Bloons Wiki — Tier 5 Upgrades](https://bloons.fandom.com/wiki/Tier_5_Upgrades). Sur certaines tours, c'est même 1 Tier 5 par tour TYPE pour les paths "uniques" (ex : un seul Sun Avatar path 1 actif).
- **Crosspath quantitatif** : grâce à 5-2-0, le joueur ne peut JAMAIS empiler 3 upgrades T3+. Force le choix.
- **Coût escalade non-linéaire** : T4 → T5 multiplie souvent par ×5-10. Paywall économique majeur.

### 1.5 Paragon — fusion ultime

Quand les **3 Tier 5** d'une tour sont posés, ils peuvent être **sacrifiés** pour créer un **Paragon** (1 par tour-type par game). Coût additionnel ≈ ×3 le prix d'un T5 (variable). Mécanique de **degree 1-100** alimentée par 5 catégories cappées :

| Catégorie | Conversion | Cap |
|---|---|---|
| Pops | 180 pops = 1 power | 90 000 (partagé avec income) |
| Bonus income | 45$ généré = 1 power | 90 000 (partagé) |
| Non-T5 tiers sacrifiés | 100 power/tier | 10 000 |
| Cash sacrificed | 1 power per (paragonPrice/20 000) | 60 000 |
| Extra T5 sacrifiés (v39+) | 6 000 power/T5 | 50 000 |

Source : [BTD6 Paragon Calculator](https://btd6paragoncalculator.org/), [Bloons Wiki — Paragons](https://bloons.fandom.com/wiki/Paragons).

**Effective power cap = 200 000** → degree 100. Solo max = degree 91-92 sans Geraldo, 100 en coop.

### 1.6 Synergies inter-tours

Synergies dans BTD6 = **catégorisées** par la communauté (Bloons Wiki — [Synergy](https://bloons.fandom.com/wiki/Synergy)) :
- **Mutuelles** : 2 tours se boostent réciproquement (ex. Alchemist Acidic Mixture + Sniper, ou Village + tout)
- **One For The Other** : tour buff (Village MIB, Alchemist) → tour offensive
- **Indirectes** : couverture des angles morts (Glue Gunner ralentit → Sniper achève)
- **Crosspath** : un tier dans un path mineur active une synergie spécifique. Ex. `2-0-3` Bomb Shooter (Cluster Bombs avec +pierce/+1 dmg via path top T2), versus `0-2-3` (attack speed boost mais moins de damage), source : [Crosspathing/Bomb Shooter](https://bloons.fandom.com/wiki/Crosspathing/Bomb_Shooter).

### 1.7 Lisibilité UX

- Hover tour → carte avec **3 paths côte à côte**, T1-T5 visibles, prix affiché à chaque tier.
- Path bloqué (règle 5-2-0) grisé avec popup "Cannot upgrade beyond Tier 2 — another path already at higher tier".
- Tier 5 a un **liseré doré** + portrait spécial. Paragon a un effet visuel unique.

---

## 2. Element TD 2

### 2.1 Modèle d'upgrade — fusion d'éléments (combos jusqu'à 6)

6 éléments : Light, Darkness, Water, Fire, Nature, Earth + 1 spécial Composite. Sources : [Element TD 2 Wiki — Towers](https://eletd2.fandom.com/wiki/Towers), [Element TD 2 Wiki — Elements](https://eletd2.fandom.com/wiki/Elements).

**Tower tiers** :
| Type | Éléments requis | Tower count | Coût build (gold) |
|---|---|---|---|
| Single Element | 1 | 6 | **175** (level 1) |
| Dual Element | 2 distincts | ~15 combinaisons | **500** (level 1) |
| Triple Element | 3 distincts | ~20 | **1 500** |
| Quad Element | 4 distincts | ~10 (update 1.4) | **4 000** (ou 2 500 upgrade depuis Triple) |
| Periodic | 6 + Essence | 1 | nécessite Essence (boss) |

Total ≈ 59 tours uniques. Sources : [GamingPH — Tower List](https://gamingph.com/2021/04/list-of-all-towers-in-element-td-2/), [Element TD 2 Wiki — Quad Towers Update 1.4](https://eletd2.fandom.com/wiki/Quad_Towers_Update_1.4).

### 2.2 Coûts par tier (single tower line)

| Niveau | Coût gold |
|---|---|
| Level 1 | 175 |
| Level 2 | 675 |
| Level 3 | 2 500 |
| Level 4 (nécessite Essence) | **15 000** |

Ratios : ×3.86 (L1→L2), ×3.7 (L2→L3), ×6 (L3→L4). Le **paywall L4** = essence (resource boss).

Dual : 500 → 1 300 → 3 300 (ratio ~×2.6). Triple : 1 500 → 5 000 (×3.3).

Source : [GamingPH — Element TD 2 Towers](https://gamingph.com/2021/04/list-of-all-towers-in-element-td-2/).

### 2.3 Spécialisation — choix d'élément forcé

**11 picks d'éléments au total**, 1 tous les 5 waves jusqu'à wave 50. Source : [Steam Beginner's Guide ETD2](https://steamcommunity.com/sharedfiles/filedetails/?id=2360427994).

À chaque pick (sauf le 1er) → un **boss spécifique** spawn et doit être tué. Cost de la décision = boss difficile, donc le joueur "paie" le pick en survie.

**Décision binaire récurrente** : à chaque pick, 2 ou 3 options proposées (élément ou bonus interest +2%).

### 2.4 Synergies inter-éléments — rock-paper-scissors

Cycle directionnel : chaque élément est fort vs celui à sa droite, faible vs celui à gauche dans l'UI. **Damage modifiers : ×2 vs cible faible, ×0.5 vs cible forte**. Composite = **damage type neutre** : fait 100% à tous, reçoit 90%.

Source : [Element TD 2 — Damage Types](https://eletd2.fandom.com/wiki/Elements).

**Synergies de combos** :
- Light : range buff + buff aura
- Darkness : DoT + multiplicateur
- Water : AoE + cluster bonus (40% secondary attack quand grouped)
- Nature : versatilité / poison
- Fire : sustained DPS qui scale
- Earth : shockwave / impact
- Light Tower : 5 / 30 / 180 / **2 160** dmg consécutifs sur même cible (scaling exponentiel par hit). Source : [GamingPH — Tower List](https://gamingph.com/2021/04/list-of-all-towers-in-element-td-2/).

### 2.5 Anti-degenerate

- **Spam d'un seul élément empêché** par le système d'**immunités creep** (chaque wave a un elemental tag, certaines waves immune à certains éléments).
- **Bosses qui spawn** à chaque pick = pénalité de diversifier trop vite (boss = real cost).
- **Composite / Periodic late-game** = end-game bottleneck (Essence rare).
- Stratégie "rainbow" (tous les éléments) viable mais **inférieure** aux focus dual selon les guides leaderboard ; source : [Forum EleTD — Nature/Water Guide](https://forums.eletd.com/topic/95388-leaderboardguides-naturewater-guide/).

### 2.6 Lisibilité UX

- UI top-right : icônes des 6 éléments avec compteur picks restants
- Build menu **dynamique** : seules les tours buildables (selon éléments unlock) sont visibles → réduit confusion
- Color-coding par élément (rouge = fire, bleu = water, etc.)

---

## 3. GemCraft Frostborn Wrath (GCFW)

### 3.1 Modèle d'upgrade — fusion + grade

Système **gemmes**, pas tours. Le joueur **crée des gemmes** d'un grade de base et les **combine** :
- **Combine** = fusion de 2 gemmes → 1 gemme du même grade plus forte (damage seulement)
- **Upgrade** = transforme 1 gemme G(n) en G(n+1) — augmente damage + special + range + attack rate
- **Mix** (entre couleurs) = crée gemme dual-color ou multi-color

Source : [Gemcraft Wiki — Gems](https://gemcraft.fandom.com/wiki/Gems), [Steam Discussion — Math Behind Combining](https://steamcommunity.com/app/1106530/discussions/0/2246677986020730393/).

### 3.2 Coûts (formule)

Mana cost d'une gemme assemblée à partir de N grade-1 gems :
```
mana = (A * n) + (B * (n - 1))
```
où A = coût d'une grade-1, B = coût de combinaison (variable selon skill Fusion).

**Skill Fusion** : -1% combining cost tous les 3 levels.

### 3.3 Spécialisation — couleur + grade vs combine

Stats split :
- **Damage** : scale avec combine count (illimité)
- **Special / Range / Attack rate** : scale avec **grade seulement** (combine ne les boost pas)
→ Force le choix : **build wide (combine many low-grade for damage)** vs **build tall (upgrade pour special/range)**.

**Couleur effects** :
- Pure (1 couleur) = bonus au special
- Dual (2 couleurs) = bonus damage, special réduit
- Multi (3+ couleurs) = compromis

Source : [Steam Discussion — Tips Damage Combining](https://steamcommunity.com/app/1106530/discussions/0/1745644234648161251/).

### 3.4 Synergies — couleur combinations

Chaque couleur = 1 special. Quelques exemples :
- Yellow = chain hit
- Red = bloodbound (+dmg per kill)
- Lime = poison
- Cyan = slow + freeze
- Orange = mana leech
- Black = bleed/curse

Combiner 2 couleurs = 2 specials simultanés mais réduits. Combine de 3+ = synergies puissantes mais lourdes en mana.

### 3.5 Anti-degenerate

- **Damage cap practically reached** quickly via combining → forces upgrade pour scaler la difficulté
- **Mana cost exponential** quand combine grandes quantités
- **Hacked Gem achievement** = grade 3 avec 1 200 effective damage = preuve community que combine seul finit par stagner

### 3.6 Lisibilité UX

- UI gemme = sphère colorée + grade chiffré
- Tooltip : damage, special name, range, attack rate
- Crafting bench = drag & drop visuel (combine = 2 gems → 1)

---

## 4. Kingdom Rush

### 4.1 Modèle d'upgrade — 3 paliers linéaires + 2 specials L4 (binaire)

Chaque tour : **L1 (build) → L2 → L3** linéaire forcé, puis **L4 = choix binaire** entre 2 specials thématiquement distincts.

Source : [Kingdom Rush Wiki — Upgrades](https://kingdomrushtd.fandom.com/wiki/Upgrades), confirmation : "Each tower type can be improved three times before the player must choose between two final upgrades with their own distinct abilities."

4 tower-types base : Archer, Mage, Barracks, Artillery.

### 4.2 Coûts (Archer Tower campaign 1, exemples)

| Tier | Nom | Coût gold |
|---|---|---|
| L1 | Archer Tower (build) | **70** |
| L2 | Marksmen Tower | **110** |
| L3 | Sharpshooter Tower | **160** |
| L4a | Rangers Hideout (poison) | ~570 (cumulé) |
| L4b | Musketeer Garrison (long range) | ~570 (cumulé) |

Source : [Kingdom Rush Wiki — Marksmen Tower](https://kingdomrushtd.fandom.com/wiki/Marksmen_Tower), [Sharpshooter Tower](https://kingdomrushtd.fandom.com/wiki/Sharpshooter_Tower).

**Ratio L1→L2 ≈ 1.57 ; L2→L3 ≈ 1.45 ; L3→L4 ≈ 3.5** (paywall doux). Total full upgrade ~910g.

Specials L4 (Rangers Hideout) ont leurs propres sous-upgrades : **Poison Arrows 250g chaque** (×3 levels). Total stack-up ~1 920g pour une tour fully maxed.

### 4.3 Spécialisation — choix binaire forcé à L4

C'est LE mécanisme phare de KR. À L4, le joueur voit **deux portraits côte à côte**, l'un orienté **DPS sustained (ex. Musketeer = long range single)**, l'autre **utility/DoT (ex. Rangers = poison + stealth detect)**. Choix **irréversible sauf si on vend** (récup ~70-80% du gold investi).

**4 archétypes par class** : ranged (Marksmen → Musketeer/Rangers), magic (Wizard → Arcane/Sorcerer), barracks (Barracks → Holy Order/Barbarians), artillery (Big Bertha → Bertha/Tesla).

### 4.4 Synergies inter-tours

Pas de système formel d'aura ou de buff inter-tour dans KR vanilla. Synergies sont **émergentes** :
- Barracks bloque → archers/mages tirent dessus depuis l'arrière
- Artillery splash → barracks regroupent les ennemis
- Mage anti-armor → archer scale avec less armor
- Reinforcements (spell) + Rain of Fire (spell) = combo classique

Source : [Steam Guide — Kingdom Rush Towers](https://steamcommunity.com/sharedfiles/filedetails/?id=731318283).

**Pas de synergie passive** entre 2 tours adjacentes (pas d'aura). C'est tout sur le placement / le mazing implicite + la complémentarité fonctionnelle.

### 4.5 Meta-progression — étoiles (stars)

Stars gagnées en complétant niveaux (1-3 par level). Stars dépensées dans **6 upgrade trees globaux** (1 par tower type + 1 par spell). Chaque tree = 5 upgrades persistants entre levels.

Source : [Kingdom Rush Wiki — Upgrades](https://kingdomrushtd.fandom.com/wiki/Upgrades).

### 4.6 Anti-degenerate

- **Total cost final tower (~1 900g)** = limite physique de spam, vu que budget level moyen ~3000-4000g.
- **Tower slots fixes** sur la map (placements pré-définis) = limite hard du nb de tours.
- Pas de spam possible d'1 tour-type vu que le **gold est rare** + slots imposés.

### 4.7 Lisibilité UX

- L1-L3 = bouton + visible avec coût
- **L4 = pop-up grand format** avec les 2 portraits + descriptions, joueur prend son temps. Choix HEAVILY signalé.
- Visuel tour change drastiquement à L4 (Musketeer ≠ Rangers visuellement)

---

## 5. Defense Grid 2 (DG2)

### 5.1 Modèle d'upgrade — 3 paliers linéaires (pas de branching)

Chaque tour a **3 niveaux d'upgrade linéaires** : L1 build → L2 → L3. Aucun choix, aucun branching. Source : [Steam — Defense Grid 2 Tower Item Guide](https://steamcommunity.com/sharedfiles/filedetails/?id=317480243), [DG2 Discussion — Tower Upgrades](https://steamcommunity.com/app/221540/discussions/0/613938693114013823/).

### 5.2 Coûts (DG2)

| Tour | L1 (build) | L2 | L3 |
|---|---|---|---|
| Gun | 100 | 300 | 700 |
| Cannon | 200 | 400 | 800 |
| Inferno | 150 | 300 | 600 |
| Concussion | 275 | 550 | 1 100 |
| Meteor | 250 | 500 | 1 000 |
| Tesla | 175 | 350 | 700 |
| Laser | 200 | 400 | 800 |
| Temporal | 300 | 300 | 300 |
| Command | 300 | 300 | 300 |

Ratios : la majorité = **×2 par level** (linéaire), avec quelques exceptions (Temporal/Command : flat 300, ce sont des tours **support** non-DPS).

Source : [DG2 Steam Discussion — Tower Upgrades](https://steamcommunity.com/app/221540/discussions/0/613936673492446535/).

### 5.3 Spécialisation — Tower Items (passive items rares)

Pas de spécialisation à l'upgrade, MAIS **Tower Items** drop aléatoirement entre les missions. Chaque tower-type (9 au total) a 3-6 items distincts, chacun avec **5 progression levels**. Items sont **passifs** (auto-appliqués au build, pas activés).

Exemples :
- Cannon — Chemical Ordnance : double full-bar damage
- Laser — Tachyon Beam : early-game game changer
- Inferno — Concentrated Fire : +125% damage
- Meteor — Plasmaball : +120% damage

Source : [Steam — DG2 Tower Item Guide](https://steamcommunity.com/sharedfiles/filedetails/?id=317480243).

→ La spécialisation se fait **hors-mission**, comme un meta-loadout, pas un choix in-game.

### 5.4 Synergies — mazing + boost towers

DG2 n'a pas de synergies actives entre tours. La seule mécanique "synergie" est :
- **Boost Tower** : tour qui ne fait rien, mais peut être placée et permet d'avoir une **autre tour build sur top**, ce qui débloque **upgrades supplémentaires** + **better line-of-sight**. Coût bas.
- **Mazing** : DG2 (comme DG1) repose sur le **path manipulation** par tower placement. Reroute aliens = boost score.

Le combat est **environnement-driven**, pas tower-buff-driven.

### 5.5 Anti-degenerate

- **Resource tight** : pas assez de gold pour spam tier 3 partout
- **Tower types complémentaires forcés** : Inferno = anti-shield, Cannon = anti-armor, Laser = anti-fast → besoin **mix** par mission
- **Aliens diversifiés** (Rhino fast, Walker tank, etc.) avec faiblesses spécifiques → forcent variety
- **Boost towers** : ajoutent de la profondeur sans complexifier l'upgrade tree

### 5.6 Lisibilité UX

- Hover tour → tooltip simple : DPS, range, type damage
- Upgrade L2/L3 = single bouton avec coût + nouveau visuel
- Pas de path/branche → décision **claire** mais **moins riche**

---

## 6. Patterns universels (3)

1. **Paywall sur le top tier** : tous les jeux ont une rupture économique majeure au tier ultime (BTD6 T5 ×5-10 vs T4, ETD2 L4 = essence rare boss-locked, Kingdom Rush L4 = ×3.5 cost, GCFW combine cost exponential, DG2 L3 = ×2-4 vs L2). Le top-tier est **conçu pour être l'investissement signature de la run**, pas une option par défaut.

2. **Spécialisation forcée à mid-tier** : le choix structurel intervient **avant** le top tier (BTD6 dès T3 via 5-2-0, KR à L4 binaire, ETD2 dès le pick d'éléments wave 5, GCFW combine vs upgrade choice à chaque crafting). Le joueur **ne peut pas tout avoir** — un commit est imposé entre milieu et fin de run.

3. **Anti-spam économique + structurel combiné** : tous combinent (a) coût escalant (×2 à ×10 par tier) qui rend le spam impossible budgétairement, et (b) une limite structurelle (slots fixes en KR, 1 paragon/tour-type en BTD6, Tier 5 unique en BTD6, immunités creep en ETD2, mana cap en GCFW). Le simple coût ne suffit jamais — il faut une **double barrière**.

---

## 7. Patterns différenciants (3)

1. **Réversibilité du choix de spécialisation** : très variable.
   - **Irréversible sauf vente** (avec récup partielle) : Kingdom Rush (vendre tour ≈ 70%), BTD6 (vendre ≈ 70-95%).
   - **Permanent** : ETD2 picks d'éléments (impossibles à défaire).
   - **Reset libre** : GCFW gemmes (peuvent être détruites pour récupérer mana à un coût).
   - DG2 : pas de choix donc pas pertinent.

2. **Synergie passive vs active vs implicite** :
   - **Passive aura** : pas dominant (BTD6 Village fait ça mais reste une exception). Milan v3 utilise actuellement ce modèle de manière dense.
   - **Active synergy** (combo crafted) : ETD2 (multi-element towers nécessitant réelle co-existence d'éléments unlock), GCFW (combine = create new entity).
   - **Implicite par complémentarité** : Kingdom Rush (pas de buff, juste rôles distincts), DG2 (pas de buff, juste mazing + types damage).

3. **Granularité du choix L3+** :
   - **3 paths × 5 tiers = 15+ archetypes par tour** (BTD6) — extrême profondeur, demande wiki externe.
   - **2 specials binaire** (Kingdom Rush) — lisible immédiat, faible barrière cognitive.
   - **Crafting libre** (GCFW, ETD2) — combinatoire infinie mais courbe d'apprentissage raide.
   - **Linéaire pur** (DG2) — zéro friction, profondeur reportée sur d'autres systèmes (mazing, items meta).

---

## 8. Applicabilité à Milan CD V3 (par jeu, 100-150 mots chacun)

### 8.1 BTD6 → Milan
Le modèle 5-2-0 + 1 path principal n'est **pas adapté** à Milan : 13 tours × 3 paths = 39 archétypes à designer, ingérable pour un solo dev sur jeu enfant. Ce qui est **transposable** : la règle Tier 5 unique (1 par tour-type par game) comme **anti-spam structurel** sur les tours signature L3 hybrides. Le pattern Paragon (sacrifier les 3 paths pour fusion) suggère que le L3 hybride peut être **un palier de "sacrifice"** plutôt qu'un simple upgrade — exiger le L2 acquis avant L3 paywall, ce qui est déjà le cas. Le crosspath (synergie via path mineur T2) montre qu'un **choix binaire L3** peut bénéficier d'une **micro-décoration** (ex. +pierce ou +rate à T2 sur l'autre branche). Lisibilité BTD6 (3 colonnes côte à côte) confirme la **clarté visuelle** comme priorité absolue à L3.

### 8.2 Element TD 2 → Milan
ETD2 montre qu'un **système de 6 éléments rock-paper-scissors** fait porter la **vraie diversification** sur le **type damage**, pas sur les paths d'upgrade. Le ratio L1→L4 est ×85 (175 → 15 000), et L4 nécessite une **resource boss-locked (Essence)** — pattern fort de paywall narratif/gameplay (pas juste gold). Pour Milan : intéressant si les 4 tours signature L3 hybrides demandent **un autre resource que le gold** (ex. tickets carnaval, kills milestones, boss kill, étoiles trophée). ETD2 prouve aussi que **les "rainbow" stratégies sont sous-optimales** vs focus dual — une leçon directe sur le risque que des synergies +50% (actuelles Milan) poussent au "tout-prendre" plutôt qu'à la spécialisation. Réduire à +25% libère bien room pour le choix L3, conformément au plan.

### 8.3 GemCraft Frostborn Wrath → Milan
GCFW est l'extrême opposé de ce que vise Milan : **liberté totale de fusion** = profondeur infinie mais courbe d'apprentissage très raide. **Pas adapté** à Milan (jeu enfant, lisibilité prioritaire). MAIS un insight précieux : la **séparation stricte stat-by-stat** (damage scale par combine, special/range scale par grade). Pour Milan : le choix binaire L3 peut suivre ce **principe de partition** — un choix booste le DPS pur, l'autre booste l'utility (range, slow, AoE radius, stun chance). Pas "L3a = +50% dmg / L3b = +60% dmg" mais "L3a = +DPS spécialiste single-target / L3b = +utility crowd". Cette séparation rend le choix **non-comparable numériquement**, donc le joueur ne peut pas optimiser via spreadsheet — il doit choisir selon **le contexte de la run**.

### 8.4 Kingdom Rush → Milan
**Le plus directement transposable**. Kingdom Rush a exactement ce que Milan vise : L1 → L2 → L3 linéaire forcé puis **L4 = choix binaire entre 2 specials thématiquement distincts**, irréversible sauf vente. Coûts : 70 → 110 → 160 → ~570 (Archer Tower), ratio L1→L4 ≈ ×8, paywall L4 ≈ ×3.5 vs L3. Pour Milan, transposition naturelle : L1 (×1) → L2 (×1.5) → L3 hybride (×3-4 vs L2) avec choix binaire UI **pop-up grand format à 2 portraits** (le mécanisme UX phare de KR). KR confirme aussi qu'un jeu peut fonctionner SANS synergie passive entre tours — la complémentarité naît des **rôles distincts** (anti-armor mage, anti-flying archer, etc.). Si Milan réduit ses synergies à +25%, il s'aligne sur le modèle KR où le placement et la complémentarité de **type damage** font le boulot.

### 8.5 Defense Grid 2 → Milan
DG2 illustre que **trois tiers linéaires sans choix** peuvent suffire si le **path manipulation** porte la profondeur stratégique. **Pas pertinent pour Milan** dont le scope visé inclut explicitement du choix L3 binaire. MAIS DG2 montre une chose précieuse : la **spécialisation peut migrer hors du tower upgrade tree**, vers un système **meta-progression** (Tower Items dropés entre missions, 5 progression levels chacun). Pour Milan : si le L3 hybride paywall + binaire risque d'être trop lourd à designer pour 4 tours × 2 specs = 8 nouvelles capacités, DG2 montre une **fallback** : garder L3 linéaire en jeu, et déplacer le binaire vers un **shop/équipement de skin** (déjà existant dans Milan) — chaque skin équipé = micro-buff thématique. Mais c'est une dérive du scope plan ; à éviter sauf si dev cost du L3 hybride explose.

---

## 9. Sources

### Bloons TD 6
- [Bloons Wiki — Upgrade Path](https://bloons.fandom.com/wiki/Upgrade_Path)
- [Bloons Wiki — Crosspathing](https://bloons.fandom.com/wiki/Crosspathing)
- [Bloons Wiki — Tier 5 Upgrades](https://bloons.fandom.com/wiki/Tier_5_Upgrades)
- [Bloons Wiki — Paragons](https://bloons.fandom.com/wiki/Paragons)
- [Bloons Wiki — Synergy](https://bloons.fandom.com/wiki/Synergy)
- [Blooncyclopedia — Upgrade](https://www.bloonswiki.com/Upgrade)
- [BTD6 Paragon Calculator (formules détaillées)](https://btd6paragoncalculator.org/)
- [Steam Discussion — Cheapest/Most Expensive T5](https://steamcommunity.com/app/960090/discussions/0/3162083441790648192/)
- [Crosspathing/Bomb Shooter (exemples 2-0-3 vs 0-2-3)](https://bloons.fandom.com/wiki/Crosspathing/Bomb_Shooter)

### Element TD 2
- [Element TD 2 Wiki — Towers](https://eletd2.fandom.com/wiki/Towers)
- [Element TD 2 Wiki — Elements](https://eletd2.fandom.com/wiki/Elements)
- [Element TD 2 Wiki — Quad Towers Update 1.4](https://eletd2.fandom.com/wiki/Quad_Towers_Update_1.4)
- [GamingPH — List of All Towers in ETD2](https://gamingph.com/2021/04/list-of-all-towers-in-element-td-2/)
- [Steam — Beginner's Guide to ETD2](https://steamcommunity.com/sharedfiles/filedetails/?id=2360427994)
- [Forum EleTD — Nature/Water Leaderboard Guide](https://forums.eletd.com/topic/95388-leaderboardguides-naturewater-guide/)

### GemCraft Frostborn Wrath
- [Gemcraft Wiki — Gems](https://gemcraft.fandom.com/wiki/Gems)
- [Steam — Math Behind Gem Combining](https://steamcommunity.com/app/1106530/discussions/0/2246677986020730393/)
- [Steam — Tips Damage Counting Combining](https://steamcommunity.com/app/1106530/discussions/0/1745644234648161251/)
- [Gemcraft Wiki — Fusion Chapter 2](https://gemcraft.fandom.com/wiki/Fusion_(Gemcraft_Chapter_2))

### Kingdom Rush
- [Kingdom Rush Wiki — Upgrades](https://kingdomrushtd.fandom.com/wiki/Upgrades)
- [Kingdom Rush Wiki — Archer Tower](https://kingdomrushtd.fandom.com/wiki/Archer_Tower)
- [Kingdom Rush Wiki — Marksmen Tower](https://kingdomrushtd.fandom.com/wiki/Marksmen_Tower)
- [Kingdom Rush Wiki — Sharpshooter Tower](https://kingdomrushtd.fandom.com/wiki/Sharpshooter_Tower)
- [Kingdom Rush Wiki — Rangers Hideout](https://kingdomrushtd.fandom.com/wiki/Rangers_Hideout)
- [Kingdom Rush Wiki — DPS and COST of towers](https://kingdomrushtd.fandom.com/wiki/DPS_and_COST_of_towers)
- [Steam — Kingdom Rush Towers Guide](https://steamcommunity.com/sharedfiles/filedetails/?id=731318283)
- [TheGamer — Best Upgrades In Kingdom Rush](https://www.thegamer.com/kingdom-rush-best-upgrades/)

### Defense Grid 2
- [Steam — DG2 Tower Item Guide](https://steamcommunity.com/sharedfiles/filedetails/?id=317480243)
- [Steam Discussion — DG2 Tower Upgrades](https://steamcommunity.com/app/221540/discussions/0/613936673492446535/)
- [Steam Discussion — When to use what tower](https://steamcommunity.com/app/221540/discussions/0/613939294398664698/)
- [Defense Grid 2 — Wikipedia](https://en.wikipedia.org/wiki/Defense_Grid_2)

### Synergies cross-game
- [Bloons Wiki — Synergy categorization](https://bloons.fandom.com/wiki/Synergy)
- [Universal Tower Defense — Game Mechanics](https://universal-tower-defense.com/wiki/mechanics/)
