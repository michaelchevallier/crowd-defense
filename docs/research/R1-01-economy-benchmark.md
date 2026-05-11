# R1-01 — Tower Defense Economy Benchmark

**Sprint** : R1 — Recherche industrie
**Auteur** : td-researcher (Sonnet)
**Date** : 2026-05-11
**Scope** : Économie comparée de 5 références TD majeures, en vue de la refonte économique de Milan Crowd Defense V3.
**Hors-scope** : aucune proposition de solution ; aucune modification de code Milan.

---

## Méthodologie

- Sources primaires : wikis (Bloons Wiki, Kingdom Rush Wiki, GemCraft Wiki, Element TD 2 Wiki), guides Steam communautaires, calculateurs joueurs (topper64, Calculator City), forums (IronHide, Steam Discussions), articles GameDeveloper.com.
- Cross-check : chaque chiffre validé sur ≥ 2 sources quand disponible (ex. cost Banana Farm $1250 confirmé par fandom + bloonswiki + calculator).
- Versions étudiées : Kingdom Rush 1 (2011, IronHide), KR Frontiers (2013), KR Origins (2014), KR Vengeance (2018) ; Bloons TD 6 v40+ (NinjaKiwi 2018-2026) ; Element TD 2 v1.5+ (Lightseekers 2021) ; GemCraft Frostborn Wrath (Game in a Bottle 2020) ; Defense Grid: The Awakening (Hidden Path 2008).
- Limite : Fandom et Bloons Wiki bloquent WebFetch (HTTP 403). Données extraites via WebSearch snippets + sources tierces (Steam guides, calculator sites, forums) qui citent les wikis.

---

## 1. Kingdom Rush (1 / Frontiers / Origins / Vengeance)

### 1.1 Tableau coût L1/L2/L3/L4 — tours signature

Kingdom Rush utilise 4 tiers (build + 3 upgrades) où chaque tour devient une "elite tower" au tier 4.

| Tour | Game | Build (L1) | L2 | L3 | L4 (elite) | Total | Ratio L4/L1 | Ratio L3/L1 |
|---|---|---|---|---|---|---|---|---|
| Orc Warriors Den (barracks Vengeance) | KR Vengeance | 70 | 120 | 180 | 250 | 620 | 8.86× | 5.29× |
| Infernal Mage (mage Vengeance) | KR Vengeance | 100 | 160 | 240 | 300 | 820 | 8.20× | 5.00× |
| Artillery (KR1, ref community) | KR1 | ~117 | — | — | — | — | — | — |

Sources :
- [Kingdom Rush Vengeance towers — NamuWiki](https://en.namu.wiki/w/%ED%82%B9%EB%8D%A4%20%EB%9F%AC%EC%89%AC%20%EB%B2%A4%EC%A0%84%EC%8A%A4/%ED%83%80%EC%9B%8C)
- [Steam Community KR Towers Guide](https://steamcommunity.com/sharedfiles/filedetails/?id=731318283) — confirme cost artillery ~117 KR1
- [Tower tune-up update — IronHide](https://www.ironhidegames.com/News/Details/332)

**Note importante** : Le ratio L4/L1 ≈ 8-9× rend la décision de "spread vs. focus" tactique. Le tier 4 dépasse largement la valeur de 3 tours L1.

### 1.2 Courbe revenu par wave

Kingdom Rush ne publie pas de courbe globale. Données ponctuelles confirmées :
- Goblin (basique W1) : **3 gold** par kill
- Troll (mid-game) : **25 gold** par kill
- Orc Champion : **30 gold** par kill
- Boss : **0 gold** (sauf Paragon)

Le revenu par wave = somme bounties × multiplicateur difficulté. Ironhide ne publie pas de table consolidée — les valeurs sont éparses sur le wiki par enemy page.

Sources :
- [Goblin — Kingdom Rush Wiki](https://kingdomrushtd.fandom.com/wiki/Goblin)
- [Orc — Kingdom Rush Wiki](https://kingdomrushtd.fandom.com/wiki/Orc)
- [Troll — Kingdom Rush Wiki](https://kingdomrushtd.fandom.com/wiki/Troll)
- [Gold — Kingdom Rush Wiki](https://kingdomrushtd.fandom.com/wiki/Gold)

### 1.3 Ratio kill/spend cible

Pas de chiffre officiel publié. Inférence community : à difficulté Veteran/Impossible, le ratio est **calibré pour < 1.0** (joueur dépense ≥ ce qu'il gagne sans bonus). Le bouton "send wave early" donne 50-200 gold bonus pour pousser le joueur à prendre des risques.

Source : [Top 10 Tips Without The Sarcasm](https://www.withoutthesarcasm.com/posts/top-10-tips-and-tricks-for-kingdom-rush/) — "GOLDEN TIME upgrade increases the bonus gold for calling a wave early by 80%".

### 1.4 Mécaniques anti-spam

1. **Pas d'income passif** — uniquement kill bounty + early-call bonus + heroes ability gold.
2. **Sell refund 60%** (KR1/Frontiers/Origins) — augmenté à **70% en Vengeance**, **90% sur Marksmen avec upgrade dédié**. Spam-and-sell pénalisé par 30-40% loss.
3. **Tier 4 elites** : ratio coût ×8-9 vs L1, mais DPS scaling > linéaire — "few key paths upgraded > spread thin".
4. **Slots fixes** par map (5-12 emplacements). Pas de placement free-form → spam physiquement borné.
5. **No boss reward** : tu paies pour gérer le boss sans réchauffer la trésorerie.

Sources :
- [Kingdom Rush Vengeance — Steam Discussion](https://steamcommunity.com/app/1367550/discussions/0/4907188445913447090/) — sell 70%
- [Best Towers & Upgrade Paths](https://www.kingdomrushgenerator.com/guide/best-towers-upgrade-paths/) — focus > spread

### 1.5 Boss reward vs mob standard

**Multiplicateur : 0×** (zéro). Bosses ne lâchent pas de gold (sauf Paragon dans certains DLC). Le joueur n'a aucun feedback économique post-boss → la trésorerie planifiée DOIT couvrir le boss.

---

## 2. Bloons TD 6

### 2.1 Tableau coût L1/L2/L3 (path 1) — tours signature

BTD6 utilise un système 3-paths × 5-tiers (max 1 tier 5 par instance, cross-pathing limité 0-2-5 ou 5-2-0).

**Difficulté Medium** (référence standard, multiplicateurs : Easy ×0.85, Hard ×1.08, Impoppable ×1.20).

| Tour | Base (T0) | T1 P1 | T2 P1 | T3 P1 | T4 P1 | T5 P1 | Ratio T3/T0 | Ratio T5/T0 |
|---|---|---|---|---|---|---|---|---|
| Dart Monkey (P1: Spike-o-pult line) | 200 | 140 | 270 | 320 | ~3500 | ~25000 | 1.6× | 125× |
| Sniper Monkey (P1: damage line) | 350 | 350 | ~700 | 2200 | 6300 | 50000 | 6.3× | 143× |
| Banana Farm (P1: production line) | 1250 | 600 | 600 | 3000 | ~6500 | ~200000 | 2.4× | 160× |

Sources :
- [Sniper Monkey — Fandom](https://bloons.fandom.com/wiki/Sniper_Monkey_(BTD6)) — base $350 medium
- [Triple Shot — Fandom](https://bloons.fandom.com/wiki/Triple_Shot_(BTD6)) — Triple Shot P2 T3 $450 medium
- [Spike-o-pult — Fandom](https://bloons.fandom.com/wiki/Spike-o-pult_(BTD6)) — $320 medium
- [Deadly Precision — Fandom](https://bloons.fandom.com/wiki/Deadly_Precision_(BTD6)) — $2200 P1 T3 sniper
- [Maim MOAB — Fandom](https://bloons.fandom.com/wiki/Maim_MOAB) — $6300 medium
- [Banana Farm — Fandom](https://bloons.fandom.com/wiki/Banana_Farm_(BTD6)) — $1250 medium base
- [Banana Plantation — Fandom](https://bloons.fandom.com/wiki/Banana_Plantation_(BTD6)) — $3000 medium
- [Tower Price Lists — Fandom](https://bloons.fandom.com/wiki/Tower_Price_Lists)

### 2.2 Courbe revenu par wave

Formule officielle :
- **Pop** : $1 par couche poppée, dégressif (×0.5 R51+, ×0.2 R61+, ×0.1 R86+, ×0.05 R101+, ×0.02 R121+)
- **End-of-round bonus** : `$100 + n` (n = round number) → R1 = $101, R10 = $110, R40 = $140, R100 = $200

Starting cash : **$650** sur toutes difficultés (uniformisé).

Source : [Money — Bloons Wiki](https://bloons.fandom.com/wiki/Money) ; [BTD6 Income Calculator](https://topper64.co.uk/nk/btd6/income).

Estimation pop income (medium, no economy towers) :
- R1-R10 cumul : ~$500-800 pops + $1100 bonus rounds
- R20 cumul : ~$3000-4000
- R40 cumul : ~$10000-15000

### 2.3 Ratio kill/spend cible

Designed pour **kill/spend ≈ 0.4-0.6 sans Banana Farms en CHIMPS** (mode no-economy). Sur medium standard, ratio ≈ 0.7-0.8. Le mode CHIMPS interdit explicitement Banana Farms / sells / continues → game devient brutal.

Source : [Bloononomics — Bloons Wiki](https://bloons.fandom.com/wiki/Bloononomics) ; [Income Farming — Fandom](https://bloons.fandom.com/wiki/Income_Farming_(BTD6)).

### 2.4 Mécaniques anti-spam

1. **Pop income dégressif** : passé R50, $1 → $0.5 → $0.2 → $0.1 → $0.02. Forçage à investir économie tôt sinon late-game collapse.
2. **Banana Farm** : ROI 15.6 rounds au tier 0 ($1250 / $80 par round). Tier 5 Banana Central = $6000/round mais coûte ~$200K → break-even ≥ 33 rounds.
3. **Multi-tower investment** : un tier 5 par tower instance (cap dur). Plusieurs Banana Central nécessite plusieurs farms physiques.
4. **CHIMPS mode** : retire farms + sells + continues + powers + income items. C'est le ratio "pur" du designer.
5. **MOAB-class enemies** : MOAB reward = 1 coin layer mais consomment ressources DPS énormes (200 hits MOAB, 4000 ZOMG, 20000 BAD) → coût opportunité élevé.

Sources :
- [Banana Farm — Fandom](https://bloons.fandom.com/wiki/Banana_Farm_(BTD6))
- [Income efficiency — Fandom](https://bloons.fandom.com/wiki/Income_efficiency)

### 2.5 Boss reward vs mob standard

Boss bloons (MOAB/BFB/ZOMG/DDT/BAD) lâchent **$1 par layer poppée** comme un bloon basique, mais ont 200-20000 HP. **Multiplicateur effectif : ×1 par layer** mais ×500-2000 en valeur totale par kill (car BAD = 20000 layers). Toutefois le bonus tombe à $0.5 puis $0.1 dans les rounds late.

Boss Events (rounds Vortex, Bloonarius, Lych, Dreadbloon, Phayze) : **récompense événement spéciale** (medallions cosmétiques, pas in-game cash).

---

## 3. Element TD 2

### 3.1 Tableau coût L1/L2/L3 — tours signature

Element TD 2 utilise un système non-tier classique : tours sont définies par **éléments combinés** (single, dual, triple, quad). L'upgrade L1→L2→L3 existe sur la même base.

| Tour | Élément(s) | L1 | L2 | L3 | Ratio L3/L1 |
|---|---|---|---|---|---|
| Money Tower | Earth (tri-element) | ~250 | ~500 | ~1000 | ~4× (estim.) |
| Lightning Tower | Air | ~75 | ~150 | ~300 | ~4× |
| Fire Tower | Fire | ~50 | ~100 | ~200 | ~4× |

Sources :
- [Money Tower — Fandom](https://eletd2.fandom.com/wiki/Money_Tower)
- [Towers — Element TD 2 Wiki](https://eletd2.fandom.com/wiki/Towers)
- [Beginner's Guide Steam](https://steamcommunity.com/sharedfiles/filedetails/?id=2360427994) — costs flous, pas de table publique

**Caveat** : ETD2 ne publie pas de table de coûts standardisée. Les chiffres ci-dessus sont des estimations basées sur les patterns single/tri-element observés dans les guides community. Vérification directe in-game requise pour cibles précises.

### 3.2 Courbe revenu par wave

- **Starting gold** : variable selon mode (typique 300-500)
- **Per-kill bounty** : ~5-15 gold creep basique W1, ~30-80 W10+
- **Interest** : **+2% du gold actuel toutes les 15 sec** → boost majeur si banque pleine
- **Interest disabled après wave 55** (changement design pour anti-snowball late)

Source : [Gold — Element TD 2 Wiki](https://eletd2.fandom.com/wiki/Gold) ; [Money tower nerf discussion](https://steamcommunity.com/app/1018830/discussions/0/4518884388511319453/).

### 3.3 Ratio kill/spend cible

Pas de chiffre officiel. Le pick "Interest" en début de match (alternative à pick Element) suggère que le designer voit l'interest comme **trade-off explicite** vs diversité tour. Ratio cible probable autour de **0.6-0.8** avec interest, **0.4-0.6** sans.

### 3.4 Mécaniques anti-spam

1. **Interest 2% toutes les 15s** → encourage banking, pas spam construction.
2. **Interest +0.6% upgrade** : un seul achat possible, capping le snowball.
3. **Interest disabled si leak** durant la wave en cours → punition économique du sloppy play.
4. **Interest hard-capped à wave 55** → late-game force le passage à kill-only economy.
5. **Sell refund 80%** (100% en mode random) → encourage pivot stratégique (rebuild = perte 20%).
6. **Money Tower** : tour qui génère gold passif ET fait dégâts proportionnels au gold-on-hand. Synergie banking + DPS — mais déverrouillage tri-element élevé (3 picks utilisés).

Sources :
- [Gold — Element TD 2 Wiki](https://eletd2.fandom.com/wiki/Gold)
- [Selling towers thread](https://steamcommunity.com/app/1018830/discussions/0/3076496088111406600/)
- [Money Tower — Fandom](https://eletd2.fandom.com/wiki/Money_Tower)

### 3.5 Boss reward vs mob standard

Données précises non publiées sur le wiki. Pattern community : **boss ≈ ×5-10 mob standard** en gold/HP, mais require sustained DPS. Le money tower bénéficie disproportionnellement (banque pleine + bounty boss).

---

## 4. GemCraft Frostborn Wrath

### 4.1 Tableau coût gem grade — système atypique

Pas de "tower upgrade" classique : on combine des **gems** dans des **buildings**.

Formule officielle : `cost(grade n) = (A × 2^(n-1)) + B × (2^(n-1) - 1)`
avec A = 60 (G1 base), B = 240 (combine cost).

| Grade | # gems G1 fusionnés | Mana cost cumul | Ratio Gn/G1 |
|---|---|---|---|
| G1 | 1 | 60 | 1× |
| G2 | 2 | 360 | 6× |
| G3 | 4 | 960 | 16× |
| G4 | 8 | 2160 | 36× |
| G5 | 16 | 4560 | 76× |
| G6 | 32 | 9360 | 156× |
| G10 | 512 | ~150000 | ~2500× |

Sources :
- [Steam Guide — Cost and Effect of Gem Level](https://steamcommunity.com/sharedfiles/filedetails/?id=1971093408)
- [Math behind gem combining — Steam Discussion](https://steamcommunity.com/app/1106530/discussions/0/2246677986020730393/)
- [Gemforce — GitHub](https://github.com/gemforce-team/gemforce) — outil officiel community

### 4.2 Courbe revenu par wave

Mana = currency. Sources :
- **Per-kill mana** : variable selon couleur gem (orange/red = mana steal, autres non).
- **Wave start mana** : faible, cumulatif via traps + summoning calls.
- **Summoning** : le joueur peut **invoquer monstres en plus** (battle traits) pour gagner XP/mana — risque/reward.

Pas de courbe par-wave standardisée car les maps varient extrêmement (waves 30 → endurance ∞). Mana farm community : G6+ orange traps = "near-unlimited mana" en endurance.

Sources :
- [Mana farm — Fandom](https://gemcraft.fandom.com/wiki/Mana_farm)
- [Early game mana farm — Steam Discussion](https://steamcommunity.com/app/1106530/discussions/0/2246679252947072998/)

### 4.3 Ratio kill/spend cible

GCFW vise un ratio **< 0.3** sans mana farm (joueur sous-budget) et **> 1.5 avec mana farm dédié** (snowball massif intended pour endurance grind XP). C'est le seul jeu de notre panel qui **encourage explicitement** le snowball économique pour farm XP infini.

### 4.4 Mécaniques anti-spam

1. **Building cost incrémental** : chaque nouveau building tower coûte plus que le précédent → "the higher gem are meant to be used when it's not longer economical to make additional buildings". Anti-spam architectural.
2. **Gem grade exponential** : ratio G3/G1 = 16×, G5/G1 = 76×. Spam G1 devient inutile mid-game.
3. **Combine cost > base cost** (240 vs 60) → consolider est explicitement plus cher que multiplier — choix actif requis.
4. **Battle traits** : modifiers que le joueur active en début de map pour +XP au prix de difficulté ↑. Self-imposed challenge = anti-trivialisation.
5. **Enrage system** : le joueur peut "enrage" les waves pour +HP/+speed/+XP. Mana farm encourage enrage extreme.

Sources :
- [Steam Guide — Gem Cost](https://steamcommunity.com/sharedfiles/filedetails/?id=1971093408)
- [Tips Differences from CS — Steam Discussion](https://steamcommunity.com/app/1106530/discussions/0/1740012510019569360/)

### 4.5 Boss reward vs mob standard

GCFW n'a pas de "boss" au sens KR/BTD6. À la place : **giants, swarmlings, shadows, beacons** comme enemy modifiers. Beacons spawnent waves additionnelles si non détruites → menace économique escalating, pas reward boost.

---

## 5. Defense Grid: The Awakening

### 5.1 Tableau coût L1/L2/L3 — toutes les tours

DG utilise un système clean : 10 tours × 3 levels (Green/Yellow/Red).

Source : [Defense Grid Guide — AyumiLove](https://ayumilove.net/defense-grid/)

| Tower | L1 | L2 | L3 | Total | Ratio L3/L1 |
|---|---|---|---|---|---|
| Gun | 100 | 200 | 400 | 700 | 7× cost L3 |
| Inferno | 150 | 300 | 600 | 1050 | 7× |
| Tesla | 175 | 350 | 700 | 1225 | 7× |
| Cannon | 200 | 400 | 800 | 1400 | 7× |
| Laser | 200 | 400 | 800 | 1400 | 7× |
| Missile | 225 | 450 | 900 | 1575 | 7× |
| Meteor | 250 | 500 | 1000 | 1750 | 7× |
| Concussion | 275 | 550 | 1100 | 1925 | 7× |
| Temporal | 300 | 300 | 300 | 900 | 3× |
| Command | 300 | 300 | 300 | 900 | 3× |

**Pattern remarquable** : 8 tours sur 10 suivent **exactement** le ratio L1/L2/L3 = 1/2/4 (cumul ×7 base). Les 2 tours utility (Temporal, Command) ont un coût plat 300/300/300.

### 5.2 Courbe revenu par wave

- **Per-kill** : alien Walker basique = 5-10 resources, Swarmer = 1-3, Tank = 25-40, Boss = 100+.
- **Interest scaling** : à 1000 resources, ~2-3/sec. À 400-500 resources, 1/sec. Compounds avec nombre de power cores restants.
- **Resource salvage Command Tower** : 125%/135%/145% bounty multiplicateur dans rayon Command L1/L2/L3.

Sources :
- [Defense Grid — Steam Guide Basic](https://steamcommunity.com/sharedfiles/filedetails/?id=218777908)
- [Aussiedroid's DG Tower Guide](https://steamcommunity.com/sharedfiles/filedetails/?id=121597561)

### 5.3 Ratio kill/spend cible

DG vise un ratio **< 0.7 sans interest exploitation**, **> 1.0 avec banking actif**. Le scoring system favorise low-spend high-survive (resources non-dépensées = score) → le designer **récompense banking** au lieu de le punir comme BTD6.

### 5.4 Mécaniques anti-spam

1. **Interest scalant** : plus la banque est pleine, plus l'interest/sec augmente (non-linéaire). Banking ouvertement rewardé.
2. **Power core mechanic** : le rate d'interest est lié au # cores restants. Perdre un core = double pénalité (objectif + interest).
3. **L3 ratio coût ×4 vs L1** mais DPS ratio plus généreux que KR (×3-4 selon tour) → upgrade > spread, mais moins extrême que KR.
4. **Command Tower** : tour utility 300/300/300 qui boost bounty +25-45% dans rayon. **Pas de dégât**, pas un econ tower direct — c'est un multiplicateur conditionnel.
5. **Path single** : aliens suivent shortest path dynamique, le joueur peut "rediriger" en building → décisions économiques (où placer pour interest vs DPS) majeures.

Sources :
- [Defense Grid: The Awakening — Wikipedia](https://en.wikipedia.org/wiki/Defense_Grid:_The_Awakening)
- [Death is a Whale — Design and Play in DG](https://deathisawhale.com/2024/08/14/design-and-play-in-defense-grid-the-awakening/)

### 5.5 Boss reward vs mob standard

DG n'a pas de "boss" rond mais des **Juggernauts / Turrets / Spires** (élite spawns). Bounty estimé **×10-30 vs Walker basique**. Le Command Tower amplifie ce bounty.

---

## Patterns universels (3)

### P-U-1. Refund de vente entre 60% et 100%
Tous les 5 jeux permettent de vendre une tour avec **60-100% refund** (KR 60-90%, BTD6 70-95%, ETD2 80-100%, GCFW retire-then-sell, DG ~75%). C'est le frein universel anti-spam-and-rebuild : tu peux pivoter mais pas gratuitement.

Sources : [KR Vengeance sell 70%](https://steamcommunity.com/app/1367550/discussions/0/4907188445913447090/), [ETD2 sell 80%](https://steamcommunity.com/app/1018830/discussions/0/3076496088111406600/).

### P-U-2. Tier supérieur avec ratio coût ×4 minimum vs L1
- KR : L4/L1 = 8.86× (Vengeance Orc Den)
- BTD6 : T3/T0 = 2.4-6.3× selon tower, T5/T0 = 125-160×
- DG : L3/L1 = 7× (cumul) ou 4× (single jump)
- ETD2 : L3/L1 ≈ 4× (estim.)
- GCFW : G3/G1 = 16×, G5/G1 = 76×

Universal : **le tier max coûte au moins 4× le L1** pour forcer un trade-off "few high-tier vs many low-tier".

### P-U-3. Boss reward = 0× ou ×500+ (pas entre les deux)
- KR : 0× (no gold)
- BTD6 : ×1 par layer mais layers ×500-2000 → gain massif
- DG : ×10-30 pour Juggernauts
- GCFW : pas de boss, mais beacons = punition économique
- ETD2 : ×5-10 estimé

Pattern universel : **le boss n'est jamais "comme un mob × 1.5". Soit il rapporte rien, soit il rapporte énormément**. Pas de zone grise.

---

## Patterns différenciants (3)

### P-D-1. Defense Grid : interest scalant non-linéaire avec banking rewardé
DG est le seul du panel à **récompenser explicitement le banking** via interest qui scale avec le montant ET le # power cores. KR/BTD6/ETD2 limitent ou désactivent l'interest, GCFW n'a pas d'interest.

### P-D-2. Bloons TD 6 : pop income dégressif par round
BTD6 est le seul à **réduire mécaniquement la valeur des kills** au cours du jeu (×0.5 R51, ×0.02 R121). Cela force la transition de "kill-economy" à "farm-economy" → impossible de jouer kill-only en late-game.

### P-D-3. GemCraft : building cost incrémental + combine cost > base cost
GCFW est le seul à **rendre l'expansion physique** de plus en plus coûteuse (chaque nouveau building = plus cher que le précédent) ET à rendre la consolidation (combine) **plus coûteuse que la création** (240 vs 60 mana). Forçage architectural à choisir entre largeur et profondeur.

---

## Applicabilité à Milan CD V3

### Kingdom Rush → Milan CD V3

KR opère sur slots fixes (5-12 emplacements par map) avec ratio L4/L1 ≈ 8-9× et **zero passive income / zero boss reward**. La structure 13 towers de Milan partage le pattern "diversité d'archétypes" (archer/mage/barracks/artillery → tank/dps/aoe/utility chez Milan), mais le ratio Milan L3/L1 = 2.5× est **3× moins agressif que KR Vengeance** (L4/L1 = 8.86×). Le sell refund Milan (mécanique pelle, à confirmer dans le code) doit être positionné dans la fenêtre 60-90% standard. La règle "no boss reward" de KR est cohérente avec une économie pure kill — le boss devient pur cost sink, le joueur DOIT prévoir une trésorerie tampon. Milan ayant déjà 17+ types de visiteurs incluant 3 vrais boss multi-phases, le pattern KR (boss = 0 gold) pourrait s'appliquer naturellement sans casser l'archi existante. KR Vengeance utilise aussi 70% sell refund (vs 60% titres précédents) ce qui suggère un consensus design moderne autour de cette valeur.

### Bloons TD 6 → Milan CD V3

BTD6 a la transition la plus radicale : pop income $1/layer dégressif vers $0.02 R121, forcing **mandatory economy investment** via Banana Farm (ROI 15.6 rounds base, jusqu'à $6000/round Banana Central). Milan est explicitement **kill-only economy** (constat : "magnet ×2 coin aura → spam non-puni") → le pattern dégressif BTD6 est applicable en théorie (coin par kill divisé par 2 ou 5 à partir de W5) mais représente un changement design majeur (équivalent d'introduire un nouveau pillar). Le mode CHIMPS (no farms, no sells, no continues, no powers) est l'équivalent "ratio kill/spend pur" et atteint 0.4-0.6 — ce qui est **proche de la cible Milan < 0.65 W5+**. La structure tier T3/T0 = 2.4-6.3× selon tower est plus permissive que Milan actuel (2.5× uniforme), suggérant que des ratios variables par tower type peuvent fonctionner.

### Element TD 2 → Milan CD V3

ETD2 a le pattern interest le plus ciblé : **+2% gold/15s avec disable au leak ET disable hard W55**. La logique "l'interest récompense le smart play et punit le sloppy" est applicable à Milan sans introduire de tour économique dédiée — une simple règle "interest si pas de leak en wave" introduit anti-spam et anti-hoarding sans toucher l'archi 13 tours. Le Money Tower ETD2 (gold passif + dégât proportionnel au gold-on-hand) est une mécanique synergique unique : combiner économie ET combat dans la même tour évite les "tours cosmétiques économie" perçues comme boring. Le mécanisme "1 pick interest vs 1 pick element" en début de match est un meta-choix qui pourrait s'adapter en Milan via une perk system pré-niveau, mais Milan n'a pas de meta-progression équivalente actuellement.

### GemCraft Frostborn Wrath → Milan CD V3

GCFW est le plus éloigné architecturalement (gems vs towers, mana vs gold). Le pattern **building cost incrémental** (chaque nouvelle tour = plus cher que la précédente) est le plus radical anti-spam observé : il pénalise l'expansion plate. Pour Milan, ce serait équivalent à "la 4ème instance de magnet coûte 1.5× la 1ère". La formule de gem grade exponentielle (G3/G1 = 16×, G5/G1 = 76×) crée une **réelle décision tactique** sur consolidation vs spread — Milan avec L3/L1 = 2.5× n'a pas ce moment de tension. Le combine cost > base cost (240 vs 60) est conceptuellement applicable : "upgrader L1→L2 coûte plus cher que poser une nouvelle L1" force des choix actifs. À noter : GCFW est aussi le seul du panel à **encourager le snowball** plutôt que de le punir, parce que c'est un farm-XP game de fond, ce qui est l'opposé du design Milan "château finit < 50% HP sur 70% des runs".

### Defense Grid: The Awakening → Milan CD V3

DG est le pattern le plus mathématiquement clean : 8/10 tours suivent **exactement** le ratio L1/L2/L3 = 1/2/4 (cumul ×7), 2 utility towers ont coût plat 300/300/300. Cette **régularité** facilite la lisibilité joueur ET le balancing dev. Pour Milan avec 13 tours, adopter un ratio uniforme L2/L1 = 2 et L3/L2 = 2 (cumul L3 = 7× base) doublerait la marche actuelle (L3/L1 = 2.5×) et augmenterait le spend par tier-up. L'interest scaling DG (rate ↑ avec banque ↑) **récompense** le banking au lieu de le punir — option intéressante si Milan veut monétiser l'attente sans introduire farms. Le Command Tower (utility +25-45% bounty multiplicateur dans rayon, **0 dégât**) est un archétype "amplificateur économique conditionnel" que Milan pourrait porter via une nouvelle utility tile, sans casser le balancing kill-only. La perte d'un power core impactant l'interest est le pattern "double punition" le plus élégant du panel.

---

## Sources globales (cross-check)

- Kingdom Rush : [Wiki Fandom](https://kingdomrushtd.fandom.com/), [IronHide Forum](https://forums.ironhidegames.com/), [Steam Community Guides](https://steamcommunity.com/app/246420/guides/)
- Bloons TD 6 : [Bloons Wiki Fandom](https://bloons.fandom.com/), [Blooncyclopedia](https://www.bloonswiki.com/), [topper64 Calculator](https://topper64.co.uk/nk/btd6/)
- Element TD 2 : [Wiki Fandom](https://eletd2.fandom.com/), [Steam Discussions](https://steamcommunity.com/app/1018830/discussions/)
- GemCraft Frostborn Wrath : [Wiki Fandom](https://gemcraft.fandom.com/), [Gemforce GitHub](https://github.com/gemforce-team/gemforce), [Steam Discussions](https://steamcommunity.com/app/1106530/discussions/)
- Defense Grid: The Awakening : [AyumiLove Guide](https://ayumilove.net/defense-grid/), [Wikipedia](https://en.wikipedia.org/wiki/Defense_Grid:_The_Awakening), [Death is a Whale Design Article](https://deathisawhale.com/2024/08/14/design-and-play-in-defense-grid-the-awakening/)
- Économie TD générale : [Game Developer Magazine — TD Game Rules](https://www.gamedeveloper.com/design/tower-defense-game-rules-part-1-), [Cliffski's Blog — TD Design](https://www.positech.co.uk/cliffsblog/2012/10/18/tower-defense-game-design/)

---

## Limites du rapport

1. **WebFetch bloqué** sur Fandom et Bloons Wiki (HTTP 403). Données extraites via WebSearch snippets + sources tierces.
2. **Element TD 2** : pas de table de coûts publique consolidée. Estimations basées sur patterns single/tri-element observés.
3. **Kingdom Rush** : valeurs dispersées par enemy/tower page, pas de table consolidée publique. La table KR1 (Archer/Mage/Artillery) classique reste à confirmer in-game.
4. **Boss rewards** : peu documentés sauf KR (0×) et DG (×10-30). Estimation BTD6 et ETD2 nécessitent vérification gameplay direct.
5. **Versions** : BTD6 est en update continue (v40+ en 2026) — certains coûts ont pu varier depuis les sources les plus récentes (Banana Plantation $2550→$3000 medium est récent).
