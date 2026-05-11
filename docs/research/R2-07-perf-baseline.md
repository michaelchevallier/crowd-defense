# R2-07 — Perf Baseline Three.js (Milan CD V3 → V5 refonte)

**Date** : 2026-05-11
**Auteur** : Claude Sonnet 4.6 (analyse statique — pas de Chrome MCP disponible en session)
**Méthode** : Audit complet du code source + calcul analytique des draw calls et complexités algorithmiques. Pas de mesure live (Live URL `/v4/` non encore déployé, Chrome MCP non disponible).

---

## Contexte

- **Codebase** : `src-v3/` — Three.js r175+ + EffectComposer (outline opt-in) + WebGLRenderer avec PCFSoftShadowMap.
- **Level de référence** : `world10-8.js` (21×13 = 273 cells, 4 paths: 2P→1C×2 portails, waves jusqu'à 210 ennemis simultanés).
- **Cible design** : maps 25×19 + 4 portails cardinaux, ≥ 250 ennemis, 20 tours L3.
- **Cibles FPS plan stratégique** : desktop ≥ 55fps, mobile ≥ 40fps.

---

## FPS Table (projections analytiques)

| Scénario | Taille map | Ennemis peak | Towers | Draw calls total | CPU overhead DCs | FPS estimé desktop | FPS estimé mobile |
|----------|-----------|--------------|--------|-----------------|-----------------|-------------------|------------------|
| **Actuel world10-8** | 21×13 | 210 | 20 | ~1 762 | 14.1 ms | **45–55 fps** | **18–28 fps** |
| **Cible plan S2** | 25×19 + 4P | 250 | 20 | ~2 102 | 16.8 ms | **35–45 fps** | **12–20 fps** |
| **Safe desktop ≥55fps** | any | ≤ 150 ennemis | 20 | ≤ 1 450 | ≤ 11.6 ms | 55+ fps | 22–30 fps |
| **Safe mobile ≥40fps** | any | ≤ 70 ennemis | 15 | ≤ 700 | ≤ 5.6 ms | 60+ fps | 40+ fps |

*Budget 60fps = 16.7ms. CPU overhead draw call = 8µs/DC (estimé WebGL JS → GPU command submit en navigateur desktop). Mobile = ×2.*

Décomposition draw calls pour world10-8 à peak (210 ennemis, 20 tours) :

| Source | Draw calls | Instancié ? |
|--------|-----------|-------------|
| Ennemis (210 × 5.5) | 1 155 | Non — SkinnedMesh individuels |
| Tours (20 × 12) | 240 | Non — Group de meshes individuels |
| Particules (burst kill) | ≤ 400 | **Non — THREE.Sprite séparé par sprite** |
| Ground + path tiles | 10 | Oui — InstancedMesh |
| Barrières périmètre (64) | 64 | Non — BoxGeometry individuel |
| Décor, châteaux, portails | 80 | Non |
| Ciel + nuages (12 sprites) | 13 | Non |
| **Total** | **~1 762** | — |

---

## Bottleneck Top-3

### Bottleneck #1 — Draw calls enemies non-instanciés (impact critique)

Chaque ennemi crée un `THREE.Group` contenant :
- `SkinnedMesh` principal (modèle glTF)
- `SkinnedMesh` outline (normals inflées, `src-v3/systems/Outline.js`)
- 2 `Mesh` HP bar (PlaneGeometry, `depthTest: false`)
- 1 `Mesh` ground glow (CircleGeometry)
- optionnel : shield ring, boss aura (2 de plus)

= **5–7 draw calls par ennemi**. À 210 ennemis : **1 155 draw calls rien que pour les ennemis**. L'architecture actuelle ne permet pas d'instancer les SkinnedMesh car chaque ennemi a sa propre pose squelettale et sa propre barre de vie.

**Path vers le fix** : InstancedSkinnedMesh (Three.js r155+) + InstancedBufferAttribute pour HP/status. Complexité élevée mais gain ×5–10 sur les ennemis.

### Bottleneck #2 — THREE.Sprite pool non-instancié (400 draw calls potentiels)

`src-v3/systems/Particles.js` : pool de 400 `THREE.Sprite` ajoutés à la scène dès l'init (`scene.add(sprite)` pour chaque). Les sprites invisibles (`visible = false`) ne génèrent pas de draw call mais le CPU doit parcourir les 400 objets dans le scene graph pour la culling pass chaque frame.

En burst kill intense (≥ 14 particles/kill × 10 kills simultanés = 140 actifs), on atteint facilement 140–200 draw calls supplémentaires depuis les seules particules.

**Path vers le fix** : remplacer les `THREE.Sprite` par un seul `Points` ou `InstancedMesh` quad géré manuellement (shader attribute pour position/couleur/opacité). Gain estimé : économise 150–400 draw calls en pics.

### Bottleneck #3 — Shadow map + PCFSoftShadowMap sur tours individuelles

`renderer.shadowMap.type = THREE.PCFSoftShadowMap` + `renderer.shadowMap.enabled = true`. Chaque tour a `castShadow = true` sur ses meshes (via `cloned.traverse` dans `Tower._loadModel`). Chaque tour = ~8 meshes avec shadow → 20 tours × 8 = **160 shadow draw calls** en plus du rendu principal (totaux doublés pour le shadow pass).

Les ennemis ont `castShadow = false` (correct), mais les tours non. PCFSoft est le type le plus coûteux.

**Path vers le fix** : Désactiver les ombres des tours ou passer à une ombre baked/billboard. Économise ~160 shadow draw calls. Alternative rapide : `renderer.shadowMap.type = THREE.BasicShadowMap` (moins beau, ×2 perf shadow).

---

## Analyse algorithmes CPU (runtime)

Ces bottlenecks sont secondaires par rapport aux draw calls mais deviennent critiques à 250+ ennemis :

| Algorithme | Complexité | À 250E + 20T | Coût estimé |
|-----------|-----------|--------------|-------------|
| Tower targeting (scan linéaire) | O(T × E) | 5 000 ops | ~0.1ms |
| Projectile–enemy collision | O(P × E) | ~15 000 ops | ~0.3ms |
| Synergies.resolve (applyToEnemy) | O(T × E) | 5 000 ops | ~0.1ms |
| Synergies.resolve (crossEffect) | O(T²) | 400 ops | ~0.01ms |
| `_tickCristalGlaceAura` | O(T × E) | 5 000 ops | ~0.1ms |
| Particles.tick (pool 400) | O(400) | 400 | ~0.05ms |
| Enemy.tick (pathfinding + anim) | O(E) | 250 | ~1ms |
| **Total CPU game logic** | — | — | **~2–3ms** |

La logique de jeu reste sous le budget même à 250 ennemis. Le CPU est dominé par le submit WebGL (draw calls) et non la logique métier.

### BFS setup (ponctuel, pas par frame)

`MapPathfinder.buildPathsFromGrid` : BFS de chaque portail vers chaque château. Pour 4 portails × 1 château × 475 cells (25×19) : 4 × O(475) = **1 900 ops, exécution unique au chargement**. Pas un problème de runtime.

---

## Max grid size recommandation

### Contraintes hard

1. **Shadow frustum** : actuel `-60…+60` en X et Z (120×120 world units). À cellSize=4, 25×19 = 100×76 world units → tient dans le frustum. À 30×22 = 120×88 → tient encore (juste). À 32×24 = 128×96 → **dépasse en largeur**. Si cellSize augmente à 5, limite à 24×24 (120×120).

2. **BFS mémoire** : Map de parent strings `"col,row"` pour 475 cells × 4 portails = 1 900 entrées. Négligeable.

3. **Draw calls barrières** : périmètre 25×19 = 84 segments individuels. À 30×24 : 104 segments. Marginal.

### Recommandation

| Config | Grid | Ennemis max (wave) | FPS desktop | FPS mobile | Verdict |
|--------|------|--------------------|-------------|------------|---------|
| **Sûre actuelle arch** | ≤ 21×13 | ≤ 150 | 55+ | 22–28 | Desktop OK, mobile insuffisant |
| **Cible plan (risquée)** | 25×19 + 4P | 250 | 35–45 | 12–20 | Manque desktop ET mobile |
| **Avec fix instancing** | 25×19 + 4P | 250 | 55–60 | 38–45 | Objectifs atteints |
| **Max sans refonte** | **19×15** | **≤ 180** | **50–55** | **20–26** | Desktop borderline |

**Recommandation conservatrice (sans refonte instancing)** : maps ≤ **19×15** (285 cells = ratio ×1.04 vs actuel 273), ennemis ≤ 150 par vague, marge sécurité 20%. Cela permettrait d'atteindre les cibles desktop ≥ 55fps mais pas mobile ≥ 40fps.

**Recommandation avec refonte instancing (nécessaire pour atteindre les cibles plan)** : l'architecture actuelle est structurellement limitée à ~200 draw calls pour les ennemis si on veut tenir 55fps desktop. Il faut `InstancedSkinnedMesh` ou un rendu sprite billboard instancié (abandonne les animations squelettales 3D par ennemi).

---

## Risques plan stratégique §6.1

| Risque | Probabilité | Impact |
|--------|-------------|--------|
| Draw calls enemies → FPS < 55 desktop sur 25×19 | **Élevée** | Bloquant |
| Particles sprites → draw call burst au kill-mass | Moyenne | Dégradation perceptible |
| PCFSoftShadowMap coût × ennemis | Faible | Accepté (enemies castShadow=false) |
| BFS multi-portails → CPU spike au load | Faible | Setup only, non-frame |
| 400 sprites scene graph traversal | Faible | ~0.3ms hidden objects |

---

## Actions prioritaires (pré-E1)

1. **[Critique]** Spike `InstancedSkinnedMesh` ou billboard instancié pour les ennemis. Valider la compatibilité avec les animations walk/death. Si infaisable en Sprint E1, réduire `max_concurrent_enemies` à 120 par portail.

2. **[Haute priorité]** Remplacer le pool `THREE.Sprite` par un `Points` ou `InstancedMesh` quad dans `Particles.js`. Drop-in possible, gain 100–400 draw calls.

3. **[Moyenne priorité]** Désactiver `castShadow` sur les tours ou passer shadow en baked billboard. Ou `PCFBasicShadowMap`.

4. **[Info]** Taille de carte safe sans refonte = **19×15** avec ≤ 150 ennemis. Pour 25×19 les cibles FPS du plan ne sont pas atteignables sans instancing enemies.

---

## Limites de cette analyse

- **Pas de mesure live** : Chrome MCP non disponible, `/v4/` non encore déployé au moment de l'audit. Les FPS sont des projections analytiques basées sur les benchmarks GPU/CPU connus pour Three.js.
- **Hardware variable** : les estimations desktop supposent un GPU mid-range (GTX 1060 / M1 Pro), mobile supposent un Snapdragon 778G.
- **Script de validation proposé** : pour mesure live, charger `/v3/` en console et exécuter :

```js
// Mesure FPS sur 5s
let frames = 0, start = performance.now();
const id = setInterval(() => { frames++; }, 0);
requestAnimationFrame(function loop() {
  frames++;
  if (performance.now() - start < 5000) requestAnimationFrame(loop);
  else { clearInterval(id); console.log('FPS:', Math.round(frames / 5)); }
});

// Draw calls (Three.js)
console.log('Draw calls:', window.__cd?.scene?.userData?._renderer?.info?.render?.calls ?? 'unavailable');

// Spawn 200 brutes via loop
for (let i = 0; i < 200; i++) {
  setTimeout(() => window.__cd?.runner?._spawnEnemy?.('brute'), i * 30);
}
```

