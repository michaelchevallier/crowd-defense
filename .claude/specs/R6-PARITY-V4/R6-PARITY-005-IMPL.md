# R6-PARITY-005-IMPL — 5 enemies PARTIAL behaviors V4 → V6

**Sprint** : R6-PARITY-V4 (Batch P1-A, WT2)
**Type** : feature-dev (Sonnet, worktree)
**Priorité** : P1 #2
**Source** : `.claude/audit/R6-PARITY-005-enemy-gaps.md` (commit `f4a3744`)

## Contexte

Audit P0 a identifié 5 enemies PARTIAL (behaviors V4 manquants en V6). Implémenter ces 5 gaps. **Coordination avec R6-PARITY-014 (boss phases complete)** : ce ticket gère data + simple behaviors. R6-PARITY-014 gère systèmes multi-phase complexes (Apocalypse 4 phases). Si overlap (ex: fire breath dragon), R6-PARITY-005-IMPL implémente le flag/SO data + le behavior basique, R6-PARITY-014 enrichit phase logic.

## Task — 5 enemies PARTIAL

1. **wizard_king** (~120 LOC) — téléport périodique + projectile rain
   - Add to `EnemyType.cs` : flags `CanTeleport`, `TeleportCooldown`, `ProjectileRainCount`
   - In `Enemy.cs` : Update method `UpdateWizardKingBehavior()` (téléport vers cellule path random tous les X sec) + projectile rain (X projectiles arc vers castle)
   - Source V4 : `/Users/mike/Work/milan project/src-v3/entities/Enemy.js` cherche `wizard_king` block

2. **warlord_boss** (~15 LOC) — charge sprint déblocage
   - Le SO `warlord_boss` a `chargeMs=0` + gate `IsBrigand` block charge. Modifier soit SO data soit gate condition.
   - In `EnemyType.cs` ScriptableObject ou config : set `chargeMs > 0` (V4: ~2000 ms)
   - In `Enemy.cs` : modifier gate `IsBrigand` → check explicit `EnableCharge` flag à la place

3. **dragon_boss** (~10 LOC) — fire breath flag dédié
   - Actuellement detect via id-string fragile `enemy.id == "dragon_boss"`. Refactor : flag explicit `HasFireBreath` in `EnemyType.cs` SO.
   - Wire le check dans `Enemy.cs` au lieu de string compare.

4. **ai_hub** (~40 LOC) — drone summons burst pattern distinct
   - Actuellement utilise `summonsMinions` ordinaire. Ajouter `BurstPattern` : N drones spawn en cercle autour de ai_hub à interval Y.
   - In `EnemyType.cs` : flags `IsBurstSummoner`, `BurstCount`, `BurstAngleStep`
   - In `Enemy.cs` : Update method `UpdateAiHubBurst()` 

5. **kraken_boss** (~80 LOC) — tentacle slam (non impl V4 non plus = invention V6 contrôlée)
   - V4 intent doc dit "tentacle slam" mais pas impl. Implémenter version V6 : N tentacles spawn (ParticleSystem ou prefab simple) autour du kraken, slam au sol pour castle damage area.
   - In `EnemyType.cs` : flags `HasTentacleSlam`, `TentacleCount`, `TentacleDamage`, `TentacleRadius`
   - In `Enemy.cs` : Update method `UpdateKrakenTentacleSlam()` 

## Hard rules

- Cap 500 LOC par fichier (Enemy.cs ~2348 LOC déjà, ne pas grossir — extract si needed à `EnemyBossBehaviors.cs`)
- No feature creep (juste ces 5 gaps audit)
- Compile gate
- Self-report 100 mots max

## Deliverable

- Commits atomiques par enemy OU 1 commit `feat(parity-v4-005-impl): 5 enemies PARTIAL behaviors completed`
- Self-report : enemies impl (5/5 ?), LOC ajoutées, Enemy.cs LOC après, extract done y/n, compile OK, commit hash

## Time estimate

~4-6h (le plus complexe : wizard_king téléport + kraken tentacles)
