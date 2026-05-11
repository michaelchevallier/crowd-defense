# QA Gates — Multi-Opus Swarm Validation Protocol

**Date** : 2026-05-11
**Purpose** : checkpoints automatiques pour garantir qualité des Stage A en parallèle avant que Stage B Integrator n'agrège tout.

**Principe** : un Sub-Opus ne merge JAMAIS dans `main` directement. Il push sur `axis/{name}`. À chaque commit majeur (milestone), il déclenche un QA Sonnet. MO arbitre les merges.

---

## QA-1 : Pre-Spawn (1× par axe, avant le 1er Sonnet)

**Quand** : à la création du Sub-Opus, avant qu'il spawne ses Sonnets.
**Qui** : Sub-Opus lui-même (self-check).
**Quoi** :
- [ ] Sub-Opus a lu `.claude/coordination/file-ownership.md` → confirme sa zone
- [ ] Sub-Opus a lu `.claude/coordination/api-contracts.md` → confirme respect des contracts
- [ ] Sub-Opus a écrit son plan évolutif `.claude/plans/axis-{name}.md`
- [ ] Sub-Opus a créé sa branche `axis/{name}` depuis main HEAD

**Si fail** : Sub-Opus stop, escalade à MO via `.claude/coordination/requests/`.

---

## QA-2 : Per-Commit (à chaque commit pushed sur axis/*)

**Quand** : déclenché par Sub-Opus après chaque commit avant de continuer.
**Qui** : Sonnet QA spawned par Sub-Opus (BG, courte vie).
**Quoi** :

```
# QA Sonnet brief template

1. git diff axis/{name} main → liste files touched
2. Check : aucun file dans HOT ZONES (cf file-ownership.md)
   - Tower.cs, Enemy.cs, Castle.cs, WaveManager.cs, LevelRunner.cs, Economy.cs, BalanceConfig.cs
3. Check : tous les files touched sont dans la zone ownership de l'axe
4. Compile : `mcp__UnityMCP__refresh_unity` + `read_console errors` → 0 errors
   - Si compile fail : report stacktrace au Sub-Opus, block merge
5. Tests : si Assets/Tests/ a des tests touchés → run `mcp__UnityMCP__run_tests` → pass
6. API contracts : si nouveau public method ajouté à un singleton (AudioController, JuiceFX, VfxPool, AnimationController, SettingsRegistry) :
   - Confirm signature matches api-contracts.md
   - Sinon : block + propose extension request
7. Lint : grep `Debug.Log` sans #if UNITY_EDITOR wrap → warning
```

**Si fail** : Sonnet QA report dans `.claude/coordination/qa-reports/{commit-sha}.md` + revert le commit OR fix-forward selon sévérité. Sub-Opus est notifié.

**Si pass** : QA report short, Sub-Opus continue.

---

## QA-3 : Pre-Merge axis → main (à la fin de Stage A axis-level)

**Quand** : Sub-Opus déclare "Stage A terminé" pour son axe.
**Qui** : Sonnet QA "ship-gate" spawned par MO.
**Quoi** :

```
1. Toutes les QA-2 precedents = pass
2. Build full Unity : `mcp__UnityMCP__manage_build target=webgl` → success
3. (Optionnel) Build other targets si Axis E ready : Mac/Win/Linux → success
4. Console clean : 0 errors, warnings whitelisted (font Roboto OK puisque Axis F en cours)
5. Smoke test : `mcp__UnityMCP__manage_editor play` 10s + screenshot scene_view_frame → vérifier rendu sain
6. Coverage : si axis a des tests, % pass = 100
7. Git history clean : commits atomic + messages conventional + footer Co-Authored-By
```

**Si pass** : MO merge axis branch dans `integration/phase3-4-5`.
**Si fail** : Report `.claude/coordination/qa-reports/{axis}-pre-merge.md` + Sub-Opus fix.

---

## QA-4 : Post-Integration (après Stage B Integrator hot zones)

**Quand** : Integrator Sonnet a appliqué tous les hooks dans Tower.cs, Enemy.cs, etc.
**Qui** : Sonnet QA "integration-gate" spawned par MO.
**Quoi** :

```
1. Build WebGL → success
2. Compile all targets if Axis E ready
3. PlayMode test : load W1-1, advance 3 waves (audio + juice + VFX should fire)
4. Screenshot avant/après chaque feature : 
   - Tower fire sound + shake + impact VFX visible
   - Enemy die sound + death VFX
   - Wave clear flash
5. Mobile-frame test : screenshot at iPhone SE viewport (375×667), HUD readable
6. Console clean
```

**Si fail** : MO révise integration-spec.md + relance Integrator.
**Si pass** : MO merge `integration/phase3-4-5` dans `main`, deploy /v6/.

---

## QA-5 : Smoke Test E2E (production /v6/)

**Quand** : après deploy /v6/ post-integration.
**Qui** : qa-tester agent (Chrome MCP).
**Quoi** :

```
1. Navigate https://michaelchevallier.github.io/crowd-defense/v6/
2. Wait load complete (~5s WebGL)
3. Click "Play" button
4. Verify HUD pills affichent texte (font Roboto OK after Axis F)
5. Place 1 tower (Archer)
6. Click "Lancer la vague (1)"
7. Verify 30+ enemies spawn + audio plays (tower_shoot, enemy_hit, enemy_die_basic)
8. Verify camera shake on first tower fire
9. Verify VFX particle on enemy death
10. Verify wave cleared → flash + audio wave_clear
11. Score : critical = 100% pass ; soft (juice intensity) = LLM judge accept
```

**Si fail** : MO triage. Si critical → revert deploy, fix forward.
**Si pass** : Phase ship-done. STATUS.md updated.

---

## Sonnet QA agent template

Pour spawner un QA Sonnet :

```
Agent({
  description: "QA {axis} commit {sha}",
  subagent_type: "general-purpose",
  model: "sonnet",
  prompt: """
  QA gate pour {axis} commit {sha}.
  
  Lire .claude/coordination/qa-gates.md QA-{N} pour le protocol exact.
  
  Étapes :
  1. git diff axis/{axis} main → liste fichiers
  2. Check ownership : tous in zone, aucun in hot zones (cf file-ownership.md)
  3. Compile Unity batch : mcp__UnityMCP__refresh_unity + read_console errors
  4. API contracts respectés : grep public method signatures vs api-contracts.md
  5. Tests pass si présents
  
  Output : `.claude/coordination/qa-reports/{axis}-{sha}.md` avec :
  - Status : PASS / FAIL / WARN
  - Issues : liste avec sévérité + fix recommandé
  - Files changed : liste
  - Compile status : OK / errors stacktrace
  
  Court (50-100 lignes max). NE PAS spawn d'autres agents. NE PAS modifier code source.
  Push ton report sur axis/qa-reports/{sha} branch.
  """
})
```

---

## Cas d'arbitrage MO

Si Sub-Opus QA report = FAIL et Sub-Opus dit "but my code is correct" :
1. MO read report + diff
2. Si Sub-Opus right → MO ajuste contracts/ownership docs + revalide
3. Si QA right → MO insiste, Sub-Opus fix-forward
4. Pas de "freeze" : décision dans 5 min max, on continue

Si conflict inter-axis (Sub-Opus X et Y veulent même fichier) :
1. Premier-arrivé-premier-servi (timestamp commit)
2. Le 2e écrit dans `.claude/coordination/requests/` pour proposer ajustement
3. MO arbitre

**Pas de blocking absolu** : on accepte 80% qualité now, 20% polish Phase 4. La vélocité est plus précieuse que la perfection.
