# Decisions arbitrées Mike — Round D1 + R3 (2026-05-11)

> Décisions post-interview consolidée Q1-Q18 après livraison des 6 specs (D1-01..D1-04 + R3-01..R3-02).
> Source de vérité stable pour le sprint MIGRATE Unity. Tout patch ultérieur de spec doit référencer ce fichier.

---

## D1-01 Économie (3 décisions)

### Q1 — Floor reward endless/W10
**Choix : B** (floor endless = **0.70**) + **fin mécanique endless**.

- Floor global du reward multiplier reste **0.5** (le reward ne descend jamais sous 50 % du baseline W1).
- Override endless : floor = **0.70** (endless sensiblement plus généreux pour tenir long).
- **Ajout Mike** : "le endless doit se finir mécaniquement à un moment". → Implémenter `difficultyMul = 1.1^(world-10)` après W10. Mur insurmontable atteint ~W20-25 (`difficultyMul ≈ 2.6-4.0`).

### Q2 — Treasure value `*`
**Choix : B** (range **50-150¢** aléatoire).

- Variance fun de loot, distribution uniforme dans la range.

### Q3 — Anti-double magnet
**Choix : A** (cap global **1 magnet/level**).

- Opt-in `allowMultiMagnet: true` autorise **2 magnets sur W7+** (multi-portail).
- Pas de BFS-distance (plus simple à valider visuellement).

---

## D1-02 Pacing (4 décisions)

### Q4 — Auto-start fail-safe
**Choix : A** (auto-start **OFF strict**, zéro fail-safe — confirme arbitrage R1 Q7).

### Q5 — Skip bonus
**Choix : A** (**+30¢ flat** fenêtre 5s post-fin-wave précédente).

### Q6 — Streak reset
**Choix : B** (streak reset **uniquement si fenêtre 5s expire** — clic hors fenêtre **conserve** le streak).

- Plus permissif que défaut spec.
- Justification Mike : pas punir le joueur qui clic sur autre chose entre 2 waves.

### Q7 — Debounce 300 ms partagé clic + N
**Choix : A** (debounce activé sur les deux inputs).

---

## D1-03 L3 hybride (3 décisions)

### Q8 — Refund pelle
**Choix : B** (**80 %** statu quo, pas de breaking change vs `BuildPoint.js:127`).

### Q9 — Ballista + portal `pierceMega`
**Choix : B** (garder **pierce non capé** — borné naturellement par la range du projectile).

- Mike note : "limité par la range de toute façon non ?" → confirmé.
- Pas de cap +5 nécessaire. Synergie iconique conservée.

### Q10 — Confirmation 2-clics au choix L3
**Choix : B** (**1-click direct**, UX plus fluide).

- L3 irréversible sauf pelle.
- Mike accepte le risque panic-click (refund 80 % via pelle = filet de sécurité acceptable).

---

## D1-04 Castle HP (4 décisions)

### Q11 — No-regen W6+
**Choix : A** (**skip total `waveRegen`** dès W6, pas de cap 0.3).

### Q12 — Override `castleHP:100` dans 80 JSONs
**Choix : B** (garder l'**override JSON optionnel** — formule = défaut).

- Permet level designer d'override la formule par level si besoin gameplay spécifique.

### Q13 — UI HUD HP couleur
**Choix : A** (mon arbitrage — Mike : "prend une décision").

- **Reporté à D1-09** (passe UI dédiée hors balance pure).
- Justification : D1-04 = spec balance. HUD couleur touche `index.html` + thème + accessibilité → mieux dans passe UI consolidée.

### Q14 — Floor castleHP W1-1
**Choix : B** (**200 HP** au W1-1, plus permissif tutoriel).

---

## R3-01 Tooling (3 décisions sprint TE)

### Q15 — In-game overlay vs standalone webapp
**Choix : A puis C** (mon arbitrage).

- **Phase 1** : overlay Tweakpane (`?debug=1`, 8-12 h ROI immédiat).
- **Phase 2** : standalone webapp si besoin post-feedback.
- Évite engagement 40-80 h sur level editor avant validation.

**NOTE PIVOT UNITY** : R3-01 entièrement déprécié par Q18=B. Unity fournit ScriptableObjects + Inspector + Scene editor natifs → annule besoin tooling custom.

### Q16 — Source-of-truth des data
**Choix : B** (JSON séparés — Mike lean confirmé).

- `data/towers.json`, `data/enemies.json`, levels déjà JSON-ish.
- Plus tooling-friendly + git diff propre.

**NOTE PIVOT UNITY** : Q16 devient ScriptableObjects assets Unity (sérialisation YAML/binary native).

### Q17 — Save-back workflow
**Choix : A** (mon arbitrage — Vite plugin custom).

- Zero friction dev local, repo solo = pas d'enjeu sécu.
- Workflow le plus court possible (tweak → write file → commit).

**NOTE PIVOT UNITY** : Q17 deprecé. Unity Editor sauvegarde direct dans `Assets/`.

---

## R3-02 Portability — Q18 LA décision majeure

### Q18 — Engine future
**Choix : B** (**MIGRER UNITY 6 LTS** direct, plan 8 sem Phaser annulé).

- Mike validation : "Go sur B direct mais faut que tu me pilote sur les installs sauf si tu peux le faire en autonomie."
- Stack cible : Unity `6000.0.74f1` LTS + C# + Unity-MCP (CoplayDev 5800★).
- Niveau autonomie estimé Opus : ~70-80 % sur la migration tech pure.
- Repo nouveau : `crowd-defense` (privé, `michaelchevallier/crowd-defense`, `/Users/mike/Work/crowd-defense/`).
- Mike skills Unity/C# : **C — zéro** (apprentissage on-the-fly via mes vulgarisations Unity-MCP-piloté).
- Durée estimée : **7-12 sem** (vs 12-18 industry avg).
- Plateformes shipping cibles : Steam (Mac/Win/Linux) + iOS + Android + WebGL `/v6/`.

---

## Impact sur les specs D1 ramenées dans `crowd-defense`

| Spec | Statut post-pivot | Notes |
|---|---|---|
| D1-01 économie | ✅ valide | Target design moteur-agnostique. À ré-implémenter en C# ScriptableObjects. |
| D1-02 pacing | ✅ valide | UI réimplémenté en Unity UI Toolkit (UI Builder + USS). |
| D1-03 L3 hybride | ✅ valide | API `Tower.UpgradeTo(level, branch)` en MonoBehaviour ou ScriptableObject. |
| D1-04 castle HP | ✅ valide | Formules portables en C# `Balance.cs` static helpers. |
| R3-01 tooling | ⚠️ dépréciée | Unity Editor remplace le custom tooling. Garder R1-R2 patterns pour ScriptableObject Inspector custom uniquement. |
| R3-02 portability | ✅ archivée | Décision actée, plus de relecture nécessaire. |

---

## Process post-pivot

1. ✅ Commit ces decisions dans `milan project/` (référence stable).
2. ✅ Tag `v5.0-pre-pivot-unity` sur HEAD `milan project/`.
3. ✅ Copy specs + research + decisions vers `crowd-defense/docs/`.
4. ✅ Setup `crowd-defense` repo Unity-ready (gitignore + gitattributes Unity standards).
5. ⏳ Mike login Unity Hub + activate license Personal (5 min, manuel).
6. ⏳ Install Unity 6 LTS via Hub CLI (autonome).
7. ⏳ Install Unity-MCP server + plugin (autonome).
8. ⏳ Init projet Unity dans `crowd-defense/` (autonome).
9. ⏳ `/plan` formel migration via ExitPlanMode (Opus dans `crowd-defense` repo).
10. ⏳ Sprint MIGRATE Phase 1 démarre (POC 1 niveau).
