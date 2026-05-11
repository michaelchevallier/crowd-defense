# Axis CONTENT-LEVELS — Plan évolutif

**Sub-Opus** : SO-CONTENT
**Branch** : `axis/content`
**Worktree** : `/Users/mike/Work/crowd-defense/.claude/worktrees/agent-ac54e0d25379c7ff9`
**Date** : 2026-05-11
**Mission** : auditer 80 LevelData vs specs D1 + apply fixes balance critiques + briefings W1-W10.

## Zone exclusive write

- `Assets/ScriptableObjects/Levels/*.asset` (80 LevelData)
- `Assets/Resources/LevelRegistry.asset` (rebuild post-fixes)
- `docs/specs/levels/*` (briefings + design notes)

## Hot zones interdites

- `Assets/Scripts/Entities/*` (Tower, Enemy, Castle)
- `Assets/Scripts/Systems/*` (LevelRunner, Economy, etc.)
- `Assets/Scripts/Data/LevelData.cs` (le code reste hot)
- `Assets/Scripts/Data/BalanceConfig.cs` (hot)
- `STATUS.md`, `Packages/manifest.json`

## QA-1 self-check (pre-spawn)

- [x] Lu `.claude/coordination/file-ownership.md` → zone confirmée
- [x] Lu `.claude/coordination/api-contracts.md` → pas d'API nouvelle dans cet axe
- [x] Lu D1-01 économie + D1-02 pacing + D1-04 castle HP + Q1-Q18
- [x] Lu `Assets/Scripts/Data/LevelData.cs` (struct cible)
- [x] Branch `axis/content` créée depuis HEAD courant

## Stage A — déliverables

### D.1 — Audit 80 LevelData (gap-report.md)

**Approche** : Sub-Opus lit les 80 .asset directement (volume gérable ~250 KB total). Pas besoin Sonnet pour cette tâche (lecture/analyse pure, pas d'écriture massive).

Stats à extraire par level :
- world / level
- startCoins
- castleHP effectif (override OU formule D1-04)
- castleHP attendu via formule = `round(100 + 50 × √world × difficultyMul(level))` (W1-1 floor 200)
- wave count
- total mob HP estimé (count × baseHP enemy, sans pressureMul puisque appliqué runtime)
- magnet allowMultiMagnet (W7+ legal, W1-W6 illegal)

Gap classes :
- **Critical** : castleHP < 60% formule (game-breaking — won't survive)
- **Major** : castleHP entre 60-80% formule (challenging — needs review)
- **Minor** : missing briefing, missing override flag on edge cases
- **OK** : within spec

Output : `docs/specs/levels/gap-report.md` avec tableau 80 levels + classe + recommandation.

### D.2 — Apply fixes balance (15-20 critical levels)

**Approche** : Edit YAML direct. Le format actuel a `overrideCastleHP: 1` + `castleHPOverride: N`. Pour FIX :
- Option A : ajuster `castleHPOverride` (laisser override en place pour traceabilité)
- Option B : retirer override et laisser formule (cleaner mais perd la fidélité au prototype)

**Décision** : Option A pour minimiser le diff + permettre rollback facile. La formule D1-04 reste cible de référence, mais les overrides actuels (issus du prototype Phaser, audit R2-06) restent la base.

Critères d'intervention :
1. castleHP < 80% formule + non-boss → bump to 90-100% formule
2. castleHP < 60% formule + boss → bump to 70% formule (boss = pic dur volontaire selon D1-04 §3.3)
3. startCoins < 100¢ W1-W3 → bump to formule recommandée
4. startCoins > 350¢ W1-W3 → cap (jeu trop facile early)

Backup avant modif :
```bash
cp Assets/ScriptableObjects/Levels/W*.asset /tmp/levelbackup/
```

### D.3 — Briefings W1-W10 (10 fichiers)

**Approche** : Sub-Opus écrit les 10 briefings courts. Pas besoin Sonnet.

Format par world :
- World theme (medieval/desert/forest/foire/etc.)
- Narrative hook 1 paragraphe
- Gameplay focus (mob types intro, mechanics intro)
- Boss preview (W*-8)

Files :
- `docs/specs/levels/W1-briefing.md` à `W10-briefing.md`

### D.4 — Tutorial flow spec W1-1

**Approche** : Sub-Opus écrit la spec (pas de code).

Output : `docs/specs/levels/W1-1-tutorial-flow.md` avec :
- Hint chains par wave (3 phases)
- Tooltip strings (à passer à Axis F UX pour i18n)
- État triggers (player has 0 towers / wave 1 starting / etc.)
- Localization keys recommandés (`tutorial.w1_1.hint_place_tower`, etc.)

### D.5 — Rebuild LevelRegistry

Post-fixes : Editor menu `Tools/CrowdDefense/Build LevelRegistry` via Unity-MCP. Si Unity MCP indisponible, on documente que le registre actuel reste valide (les fixes n'ajoutent/suppriment pas de levels).

## Workflow réel (pragmatique)

Vu le volume gérable (80 levels × ~80 lignes YAML = 6400 lignes total) et la nature de l'audit (lecture/analyse pure), **Sub-Opus exécute D.1 + D.2 + D.3 + D.4 directement** sans spawn Sonnet :
- Spawn de Sonnet ajoute overhead de tickets + coordination
- L'audit est principalement lecture (peu de tokens écriture)
- Les fixes sont chirurgicaux (Edit YAML ciblé)

**Anti-pattern à éviter** : sur-orchestrer pour des tâches que Opus peut faire vite. Le brief autorise jusqu'à 3 Sonnets mais ne les impose pas.

## QA discipline

- Per-commit : verify fichiers touchés tous dans zone (ScriptableObjects/Levels, docs/specs/levels)
- Pre-merge : check 0 fichiers in hot zones, briefings tous existent, gap-report exhaustif
- Pas de Debug.Log dans assets YAML (N/A)
- Compile : N/A pour data assets (sauf si LevelData.cs changé — il ne doit PAS l'être)

## Critères de fin Stage A

- [x] gap-report.md écrit (80 levels classified)
- [x] 15-20 critical levels fixés balance
- [x] 10 briefings W1-W10 dans docs/specs/levels/
- [x] W1-1 tutorial flow spec
- [x] LevelRegistry verify 80 entries (no add/remove)
- [x] QA-3 self-validation
- [x] Push axis/content
- [x] Rapport final `.claude/coordination/axis-content-report.md`

## Risques

- **Sérialisation Unity** : Edit YAML risque casser format si typo. Mitigation : preserve format exact, test sur 1 level d'abord.
- **Field `lossMode` absent du LevelData.cs** : présent dans .asset, ignoré au runtime. Pas modifier.
- **`castleHP` direct vs `castleHPOverride+overrideCastleHP`** : W1-1 utilise `castleHP: 200` direct, autres utilisent override. Probablement compat ancien format. Préserver tel quel.

## Source canonical

Source Phaser `/Users/mike/Work/milan project/src-v3/data/levels/world*.js` → JSON extracted in `Assets/Editor/LevelsRaw/` → imported via `LevelImporter.cs`.
