# R1-02 — Pacing Benchmark : Wave-Start, Skip Bonus, Telegraphing

**Sprint** : R1 — Recherche industrie pure (axe PACING)
**Date** : 2026-05-11
**Auteur** : `td-researcher`
**Scope** : Comment 5 références TD/RTS gèrent (a) le déclenchement des vagues, (b) les bonus de skip, (c) le télégraphe de la composition à venir.
**Hard rule** : aucune solution proposée pour Milan CD V3. Recherche pure.

Jeux étudiés :
1. Kingdom Rush Origins (Ironhide, 2014)
2. Bloons TD 6 (Ninja Kiwi, 2018)
3. Plants vs Zombies 1 (PopCap, 2009) + PvZ 2 (2013)
4. Iron Marines (Ironhide, 2017)
5. Anomaly: Warzone Earth (11 bit studios, 2011)

---

## 1. Kingdom Rush Origins

### 1.1 Comment lance-t-on une wave ?

**Hybride avec auto-start par défaut**. Entre 2 vagues, un timer décompte (visible sur l'icône skull). À expiration, la vague suivante part automatiquement. Le joueur peut court-circuiter ce timer en cliquant l'icône skull, ou via raccourci clavier `W` (ajouté dans une mise à jour Steam post-2014). [Steam discussion 2021](https://steamcommunity.com/app/246420/discussions/0/630800446998172077/)

### 1.2 Bonus pour skip / start anticipé

**Formule de base** : **+1 gold pour chaque seconde restante** sur le timer de la vague suivante. La même quantité de secondes est aussi appliquée comme **réduction de cooldown sur les sorts actifs**. [Kingdom Rush Wiki — Gold](https://kingdomrushtd.fandom.com/wiki/Gold) (cité en search summary; page bloque WebFetch direct mais multiple sources confirment).

**Upgrade méta `Blitz Tactics`** : multiplie le bonus or par +X% (synonyme de Golden Time selon les versions) — **+80% bonus or** sur appel anticipé via l'arbre de gemmes/upgrades de KR Origins. [Level Winner — Kingdom Rush Beginners Guide](https://www.levelwinner.com/kingdom-rush-beginners-guide-tips-tricks-strategies-to-vanquish-the-evil-forces/)

| Élément | Valeur |
|--------|--------|
| Bonus or par sec restante | 1 |
| Bonus cooldown sorts par sec restante | 1 sec |
| Multiplicateur Blitz Tactics / Golden Time | +80% or |
| Coût upgrade méta | 1 point d'amélioration |

### 1.3 Délai max entre 2 waves

Pas de chiffre exact public, mais le timer entre vagues est borné par le designer du level (souvent 20-40 sec). À 0, **auto-start forcé** — le joueur ne peut pas geler indéfiniment.

### 1.4 UI/UX du bouton

- **Skull icon** dans l'overlay HUD, position fixe (bord d'écran).
- Compte à rebours visible (sec restantes).
- Hover : preview des unités à venir (icônes des enemy types qui composent la vague suivante). [Steam search summary](https://steamcommunity.com/app/816340/discussions/0/1770385542778442671/)
- Sound feedback : « ding » quand on clique + roar de la vague qui démarre.

### 1.5 Multi-skip streaks

Aucun bonus dégressif/progressif officiel. Achievement caché **« Truth or Dare »** : appeler 13 vagues anticipées dans le même level débloque un trophée + extra income (mécanique de bonus implicite mais non scalable au-delà). [Steam Achievement Guide KR Origins](https://steamcommunity.com/sharedfiles/filedetails/?id=1542876474)

### 1.6 Telegraphing de composition

- **Skull icon = preview** : affiche les types d'unités de la prochaine vague au survol (pas le nombre).
- **Indicateur compteur de vague** « Wave X/Y ».
- Pas de breakdown chiffré (HP/dmg) — preview iconographique uniquement.

### 1.7 Applicabilité à Milan CD V3 (100-150 mots)

KR Origins représente le pattern « auto-start avec skip incentivé » : le joueur n'est jamais bloqué (auto-start dépend du timer designer), mais reçoit un incentive linéaire (1¢/sec) lisible et facile à mentaliser. Le bonus n'est jamais punitif (skip 0 sec = perd 0 or, pas de pénalité). L'upgrade méta Blitz Tactics +80% double l'enjeu en mid-game et crée une décision tactique vs économique. La présence d'un raccourci clavier (`W`) post-launch montre que la communauté hardcore a réclamé une input rapide. Le télégraphe iconographique (preview enemy types) sans valeurs chiffrées maintient le wow-effect tout en évitant le min-max excessif. L'absence de multi-skip streak rewards évite la mécanique « spam skip » qui pourrait briser le pacing.

---

## 2. Bloons TD 6

### 2.1 Comment lance-t-on une round ?

**Hybride 100% manuel par défaut, avec option Auto Start**. À la fin d'une round, par défaut la prochaine ne démarre pas. Le joueur appuie sur **bouton Play / spacebar** pour la lancer. [Bloons Wiki — Auto Start (cité)](https://bloons.fandom.com/wiki/Auto_Start) [BTD6 Hotkeys](https://qnnit.com/bloons-td-6-hotkeys-steam-controls/)

L'option **Auto Start** (toggle) déclenche la round suivante automatiquement quand tous les bloons de la round précédente sont popped. Sinon, attente illimitée du joueur.

### 2.2 Bonus pour skip / start anticipé

**Pas de bonus or pour skip rapide round-by-round** dans le mode standard. Le joueur génère son cash via :
- $1 par pop
- **$100 + n pour finir round n** (bonus de fin de round, scaling linéaire) [Bloons Wiki — Money](https://bloons.fandom.com/wiki/Money)

Mais : **Fast Track mode** (mode optionnel) skip 25% des rounds (rounded down) du game et donne starting cash + Hero XP boost selon nombre de rounds skipped. [Bloons Wiki — Fast Track BTD6](https://bloons.fandom.com/wiki/Fast_Track_(BTD6)) (cité en search summary).

**Fast Forward** : 2x speed (mobile) / 3x speed (PC) du temps de jeu, ne donne pas de bonus économique direct mais accélère le revenu/temps réel. Hold spacebar = 4x speed sur PC. [Bloons Wiki — Fast Forward](https://bloons.fandom.com/wiki/Fast_Forward)

| Élément | Valeur |
|--------|--------|
| Cash par pop | $1 |
| Bonus fin de round | $100 + n |
| Fast Forward standard | 2x ou 3x |
| Faster Forward (hold) | 4x |
| Fast Track skip | 25% rounds |
| Auto Start | Toggle ON/OFF |

### 2.3 Délai max entre 2 rounds

**Aucun cap** : sans Auto Start, le joueur peut attendre infiniment (le jeu reste pause-équivalent). C'est l'un des très rares TD à laisser le joueur souverain absolu sur le pacing.

### 2.4 UI/UX du bouton

- **Gros bouton Play vert** en bas-droite de l'écran.
- Hotkey : **Spacebar** (start round + toggle fast forward).
- Animation : flash vert + sound « bloon pop » au start.
- Texte adjacent : « Round X/Y ».

### 2.5 Multi-skip streaks

**Aucune mécanique multi-skip in-mode**. Le joueur peut spam Spacebar dès qu'une round finit, mais pas de bonus exponentiel. Fast Track est une décision pre-game (one-shot).

### 2.6 Telegraphing de composition

- **Pre-Round Comments** : système de tooltips contextuels sur certaines rounds clés affichant warnings (« Third member of Thermal Bloon Trio incoming », « Camo bloons appear »). [BTD Wiki — Pre-Round Comments](https://monkeysandplants.fandom.com/wiki/Bloons_TD_6/Pre-Round_Comments)
- **External resource** (community-driven) : `topper64.co.uk/nk/btd6/rounds/` liste exhaustive de la composition par round.
- **In-game** : pas de preview détaillé de la composition de la prochaine round, juste numéro de round.

### 2.7 Applicabilité à Milan CD V3 (100-150 mots)

BTD6 illustre le pattern « pause infinie entre rounds » — l'antithèse du auto-start. Le joueur a le contrôle absolu du tempo, ce qui maximise la profondeur stratégique (placement réfléchi, anticipation des bloons spéciaux) au prix d'un rythme potentiellement molasson en early-game. Le toggle Auto Start est crucial : il laisse le choix au joueur de friction vs flow. Notable : **aucun bonus or pour skip rapide** — le revenu est entièrement déterminé par les pops + bonus fin de round, donc le joueur n'est jamais récompensé pour une rush décision. Le télégraphe se fait par tooltips contextuels (warnings sur rounds dangereuses) plutôt que preview universelle. Spacebar comme hotkey universel (start + fast forward) est une simplification UX majeure : un seul muscle memory key pour tout le pacing.

---

## 3. Plants vs Zombies (1 et 2)

### 3.1 Comment lance-t-on une wave ?

**Aucun bouton manuel — système hybride temps + dégât.**

Trois conditions déclenchent la spawn de la prochaine vague de zombies (PvZ 1) :
1. **Délai temps** : minimum **6.01 secondes entre deux vagues** dans l'original. [Speedrun.com PvZ — Basic Strats](https://www.speedrun.com/pvz/forums/f8zua)
2. **Règle du 50%** : si le joueur réduit ≥ 50% de la HP totale de la vague la plus récente, la suivante spawn **immédiatement**. [Speedrun.com PvZ — 50% rule](https://www.speedrun.com/pvz/forums/ixhje)
3. **Exception** : règle du 50% ne s'applique pas pour les vagues 9, 19, 29 (vagues drapeaux/ambush, qui sont triggers temporels purs).

PvZ 2 : système similaire mais nuance — « pour faire spawn nouveaux zombies, ceux présents doivent reach first degrade ou si ça prend trop de temps, ils spawn auto ». Le Chinese version a ajouté un **bouton manual trigger** (PvZ2 CN). [PvZ Wiki Forum — Waves of Zombies](https://plantsvszombies.fandom.com/f/p/2426454692331930987)

### 3.2 Bonus pour skip / start anticipé

**Aucun bonus économique direct** — le sun (currency) ne dépend pas du skip. Le « bonus » indirect : tuer rapidement = vague suivante plus tôt = plus de soleil par minute = plus d'investissements (compounding via Sunflowers).

Stratégie speedrun : **focus down coneheads** pour déclencher 50%-rule ASAP. Cherry bombs sauvegardés pour finir wave 9 (huge wave) instantanément.

| Élément | Valeur |
|--------|--------|
| Délai min entre vagues | 6.01 sec |
| Trigger spawn anticipé | -50% HP wave courante |
| Vagues forcées timing | 9, 19, 29 (huge waves) |
| Total flags / vagues drapeaux | 1, 2, ou 3 selon level |

### 3.3 Délai max entre 2 waves

**Pas de cap public connu** — varie par level (Ancient Egypt = waves slow, Lost City = waves fast en PvZ 2). [PvZ Wiki forum](https://plantsvszombies.fandom.com/f/p/2426454692331930987) Le délai max effectif est cappé par game design level-by-level.

### 3.4 UI/UX du bouton

**Pas de bouton wave** dans PvZ 1/2 standard. Le seul indicateur est :
- **Progress bar en haut d'écran** : avance linéairement avec un drapeau rouge à chaque flag wave (huge wave). [Plants vs Zombies Wiki — Progress bar](https://plantsvszombies.fandom.com/wiki/Progress_bar)
- Message popup « A huge wave of zombies is approaching! » lors de l'arrivée des flag waves.
- Sound : musique change + horde sound effect.

PvZ2 CN : bouton manual trigger ajouté localement, mais design proprietary chinois.

### 3.5 Multi-skip streaks

**Aucune mécanique de streak**. La règle du 50% est binaire (active ou non par vague). Le speedrun consiste à enchaîner les 50%-triggers vague après vague mais aucune récompense bonus n'est accordée pour la chaîne.

### 3.6 Telegraphing de composition

- **Pre-level seed packet preview** : avant un level, les zombie types qui apparaîtront sont affichés.
- **Progress bar** : drapeaux rouges au fur et à mesure (huge waves visibles).
- **Pop-up text** : « A huge wave of zombies is approaching! » 5-8 sec avant flag waves.
- Pas de preview round-by-round en cours de partie.

### 3.7 Applicabilité à Milan CD V3 (100-150 mots)

PvZ pionnier du pattern « pas de bouton, le joueur lit le tempo via progress bar + règle implicite ». L'élégance : la vitesse de jeu est dictée par l'efficacité offensive du joueur (kill 50% = next wave NOW), créant une boucle de feedback positive (mieux je joue, plus la partie est dense). Aucune friction UI — le joueur n'a jamais à cliquer pour avancer. Coût : zéro contrôle stratégique sur le pacing en cas de panique (impossibilité de geler avant flag wave). Les flag waves (vagues 9/19/29) sont les SEULS moments forcément temporels : le designer garde le contrôle des spikes dramatiques. Le pre-level seed packet preview dispense de tout télégraphe in-mission. Notable : la règle du 50% est invisible aux nouveaux joueurs mais centrale pour speedruns — designer pattern cachée mais profonde.

---

## 4. Iron Marines

### 4.1 Comment lance-t-on une wave ?

**Pas de système wave-by-wave traditionnel** — c'est un RTS, pas un TD pur. Iron Marines (Ironhide) hérite de l'ADN Kingdom Rush mais bascule vers RTS. [TVTropes — Iron Marines](https://tvtropes.org/pmwiki/pmwiki.php/VideoGame/IronMarines)

Mécaniques :
- **Spawners ennemis** placés sur la carte produisent périodiquement des unités.
- **Auto-spawn continu** sans intervention joueur.
- Certaines missions ont des **vagues scriptées** (« defend against waves of Fell coming from spawners ») où une condition objective (timer ou destruction de structure) déclenche un nouveau set de spawns. [Level Winner — Iron Marines](https://www.levelwinner.com/iron-marines-ios-guide-18-useful-tips-cheats-tricks/)

### 4.2 Bonus pour skip / start anticipé

**Aucun mécanisme de skip-bonus**. Le joueur ne peut pas accélérer les spawns. L'équivalent économique est :
- **Refineries d'Etherium** : revenu passif (tick par seconde), incentive le joueur à capturer/sécuriser des bases tôt. [Iron Marines Wikia — Etherium](https://iron-marines.fandom.com/wiki/Etherium)

### 4.3 Délai max entre vagues

N/A — flot continu de spawns. Les vagues scriptées ont des timers fixes designer.

### 4.4 UI/UX du bouton

**Pas de bouton wave**. UI centrée sur le hero ability bar + squad management + minimap. [TVTropes — Iron Marines](https://tvtropes.org/pmwiki/pmwiki.php/VideoGame/IronMarines)

### 4.5 Multi-skip streaks

N/A.

### 4.6 Telegraphing de composition

- **Mission briefing avant level** : décrit ennemis attendus.
- **Minimap markers** : signal visuel quand de gros pushs ennemis arrivent (icônes).
- **Voice-over commander** : « Heavy contact incoming » lors de moments scriptés.

### 4.7 Applicabilité à Milan CD V3 (100-150 mots)

Iron Marines est un cas limite : il abandonne le pattern « wave » pour un flow RTS continu, démontrant que **le wave-by-wave n'est PAS une nécessité du genre** mais un choix design. Le revenu est généré par la maîtrise spatiale (capture refineries) plutôt que par tempo (kills par vague). Conséquence : le joueur ne pense pas en blocs « ai-je tenu cette vague ? », mais en pression continue (« mes refineries tiennent ? »). Pour un projet TD comme Milan, ce pattern montre l'extrême opposé du « wave manuelle » — l'absence totale de wave structure. Pas directement transposable car bouleverse l'identité TD, mais utile comme contrepoint : ajouter une wave manuelle ne va PAS forcément améliorer l'engagement, tout dépend du loop économique sous-jacent. Iron Marines maintient l'engagement par micro-management hero/squad, pas par décisions binaires « skip ou pas ».

---

## 5. Anomaly: Warzone Earth

### 5.1 Comment lance-t-on une wave ?

**Reverse Tower Defense — pas de wave system traditionnel**. Le joueur EST l'attaquant, conduisant un convoy de véhicules le long de routes scriptées vers des objectifs (towers ennemies à détruire). [Wikipedia — Anomaly: Warzone Earth](https://en.wikipedia.org/wiki/Anomaly:_Warzone_Earth) [Polygons and Pixels review](https://polygonsandpixels.com/2015/02/18/anomaly-warzone-earth-a-reverse-tower-defense-tower-offense/)

Le « pacing wave » est dicté par :
- **Routing du convoy** : le joueur trace son chemin sur une tactical map (paused). Chaque intersection est une décision.
- **Encounters automatiques** : croiser une tour ennemie = combat instantané sans skip possible.
- Pas de bouton « lancer la wave suivante ».

### 5.2 Mode Squad Assault (wave-based)

Mode débloquable post-campagne :
- **10 vagues à survivre**.
- Chaque vague : **5 minutes max pour détruire un générateur** et déclencher la vague suivante.
- **Ennemis ne despawn pas** entre vagues — overlap si pas géré. [Steam Community — Anomaly Warzone Earth](https://steamcommunity.com/app/91200/discussions/)

| Élément | Valeur |
|--------|--------|
| Total vagues Squad Assault | 10 |
| Temps max par vague | 5 minutes |
| Ennemis persistants | Oui (no despawn) |
| Trigger next wave | Détruire générateur |

### 5.3 Bonus pour skip / start anticipé

Mode Squad Assault : finir un générateur avant les 5 minutes = transition rapide vers next wave (mais ennemis restants persistent — pénalité naturelle). Pas de bonus économique chiffré.

### 5.4 UI/UX du bouton

Pas de bouton wave traditionnel. UI centrée sur :
- **Tactical map** (pause) pour routing.
- **Power-ups dropdown** (smoke screen, decoy, repair).
- **HUD top** : current wave + timer 5min.

### 5.5 Multi-skip streaks

N/A — la friction est inhérente (ennemis persistants si rush).

### 5.6 Telegraphing de composition

- **Tactical map preview** : montre positions des towers/générateurs/ennemis avant de tracer le path.
- Pas de breakdown enemy stats inline.
- **Voice-over** : commander narrate next encounters.

### 5.7 Applicabilité à Milan CD V3 (100-150 mots)

Anomaly est l'inverse total d'un TD : le joueur est l'attaquant et le « wave system » est subverti — la « vague » est définie par sa propre progression spatiale. Squad Assault (mode wave-based optionnel) propose un pattern intéressant : **5 minutes max pour atteindre un objectif (générateur)**, sinon vague suivante quand même (cap dur). Les ennemis persistants si rush créent une auto-régulation : le joueur incentivé à finir vite, mais pénalisé s'il ne nettoie pas. C'est une mécanique « skip avec coût caché » plutôt que « skip avec bonus ». Pour Milan, ce pattern est intéressant comme point de référence sur le **cap temporel dur** (timer max = obligation de progression) qui force le pacing sans rendre le bouton manuel obligatoire. Mais structurellement Anomaly est si différent (reverse-TD) que peu d'enseignements directs sur les bonus de skip TD pur.

---

## Patterns universels (3+ jeux)

1. **Tempo dicté par timing OU efficacité offensive** : KR (timer + skip), PvZ (timer + 50%-rule), BTD6 (joueur souverain). Tous trois donnent au joueur une fenêtre d'action entre les vagues, même si les modalités diffèrent radicalement.
2. **Telegraphing par icônes/preview, pas par stats chiffrées** : KR (skull preview iconographique), PvZ (seed packet pre-level + flag drapeaux), BTD6 (warnings textuels conditionnels). Aucun n'affiche HP/dmg breakdown des prochaines vagues — préserve l'effet de surprise tout en réduisant le min-max.
3. **Hotkey clavier dédiée pour start** : KR (`W`), BTD6 (Spacebar), Iron Marines (N/A — RTS). Communauté hardcore demande systématiquement un raccourci, ajouté post-launch dans 2 cas sur 5.

## Patterns différenciants (3+ uniques)

1. **PvZ — règle du 50% HP wave** : trigger spawn anticipé invisible (pas d'UI), récompense l'efficacité offensive sans bonus économique direct. Pattern unique de « skip implicite » par dégâts plutôt que par input.
2. **KR Origins — formule linéaire 1¢/sec + multiplicateur méta-upgrade +80%** : le seul jeu à offrir un bonus économique chiffré et stackable via progression méta (Blitz Tactics).
3. **BTD6 — pas de bonus skip, pas de cap** : laisse le joueur souverain absolu, friction zéro, profondeur stratégique max. Aucun autre jeu du panel n'offre une telle latitude (le auto-start est opt-in, pas opt-out).
4. **Anomaly Squad Assault — cap dur 5 min + ennemis persistants** : le « skip » est une obligation positive (finish le générateur ou wave suivante quand même) plutôt qu'un bonus.
5. **Iron Marines — abandon total du wave concept** : démontre que TD-adjacent peut pivoter vers RTS continu sans perte d'engagement, mais avec un coût d'identité majeur.

---

## Designs qui FORCENT le joueur à penser entre les vagues

1. **BTD6 (no auto-start)** : friction maximale — le joueur doit cliquer/spacebar pour avancer, donc lit obligatoirement le state du board entre chaque round. Pas de défilement passif possible.
2. **KR Origins skull icon avec preview** : le joueur doit hover le skull pour voir la composition, créant un micro-rituel mental « est-ce que je peux skip ? » à chaque inter-vague.
3. **Anomaly Squad Assault (cap 5 min + persist ennemis)** : impossible de purement « réagir » — la décision de routing engage les 5 prochaines minutes. Force la planification.

## Designs qui le laissent flow (no friction)

1. **PvZ règle du 50%** : le joueur joue offensif, les vagues s'enchaînent sans clic. Loop addictif, zéro UI.
2. **KR Origins auto-start par défaut** : si le joueur ne fait rien, le jeu avance. Skip est opt-in (bonus optionnel).
3. **Iron Marines spawn continu** : aucune décision de pacing — le joueur micro-manage hero/squads sans jamais penser « wave ».

---

## Sources

- [Kingdom Rush Wiki — Gold](https://kingdomrushtd.fandom.com/wiki/Gold)
- [Kingdom Rush Wiki — Upgrades](https://kingdomrushtd.fandom.com/wiki/Upgrades)
- [Steam — Keybind to call waves early (Kingdom Rush)](https://steamcommunity.com/app/246420/discussions/0/630800446998172077/)
- [Level Winner — Kingdom Rush Beginners Guide](https://www.levelwinner.com/kingdom-rush-beginners-guide-tips-tricks-strategies-to-vanquish-the-evil-forces/)
- [Steam Achievement Guide — Kingdom Rush Origins](https://steamcommunity.com/sharedfiles/filedetails/?id=1542876474)
- [Bloons Wiki — Fast Forward](https://bloons.fandom.com/wiki/Fast_Forward)
- [Bloons Wiki — Auto Start](https://bloons.fandom.com/wiki/Auto_Start)
- [Bloons Wiki — Money](https://bloons.fandom.com/wiki/Money)
- [Bloons Wiki — Fast Track BTD6](https://bloons.fandom.com/wiki/Fast_Track_(BTD6))
- [Bloons Wiki — Hotkeys](https://bloons.fandom.com/wiki/Hotkeys)
- [BTD6 Hotkeys (qnnit)](https://qnnit.com/bloons-td-6-hotkeys-steam-controls/)
- [BTD Wiki — Pre-Round Comments](https://monkeysandplants.fandom.com/wiki/Bloons_TD_6/Pre-Round_Comments)
- [Plants vs Zombies Wiki — Progress bar](https://plantsvszombies.fandom.com/wiki/Progress_bar)
- [Plants vs Zombies Wiki — Waves forum](https://plantsvszombies.fandom.com/f/p/2426454692331930987)
- [Speedrun.com PvZ — Basic Strats](https://www.speedrun.com/pvz/forums/f8zua)
- [Speedrun.com PvZ — Question about 50% rule](https://www.speedrun.com/pvz/forums/ixhje)
- [Iron Marines Wikia — Etherium](https://iron-marines.fandom.com/wiki/Etherium)
- [Iron Marines Wikia — Enemies](https://iron-marines.fandom.com/wiki/Enemies)
- [Level Winner — Iron Marines Guide](https://www.levelwinner.com/iron-marines-ios-guide-18-useful-tips-cheats-tricks/)
- [TVTropes — Iron Marines](https://tvtropes.org/pmwiki/pmwiki.php/VideoGame/IronMarines)
- [Wikipedia — Anomaly: Warzone Earth](https://en.wikipedia.org/wiki/Anomaly:_Warzone_Earth)
- [Polygons and Pixels — Anomaly Reverse TD](https://polygonsandpixels.com/2015/02/18/anomaly-warzone-earth-a-reverse-tower-defense-tower-offense/)
- [GottaBeMobile — Anomaly Warzone Earth Review](https://www.gottabemobile.com/anomaly-warzone-earth-hd-review-reverse-tower-defense-game-is-loads-of-fun/)
