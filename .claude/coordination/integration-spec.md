# Integration Spec — Stage B Hot Zones Hooks

**Date** : 2026-05-11
**Purpose** : spec exhaustive des hooks à appliquer dans les hot zones (Tower.cs, Enemy.cs, Castle.cs, WaveManager.cs, LevelRunner.cs, Economy.cs) APRÈS que tous les Stage A axes aient livré.

**Qui applique** : 1 seul Sonnet **Integrator**, séquentiel, spawned par Main Orchestrator. Aucune parallélisation sur cette étape.

**Pre-requis** : tous les axes A-G ont leur axis branch merged dans `integration/phase3-4-5`. Les classes `AudioController`, `JuiceFX`, `VfxPool`, `AnimationController`, `L`, `SettingsRegistry` existent.

---

## I1 — Tower.cs hooks

### I1.1 — Tower.Fire() (called when tower acquires target + shoots projectile)

**Position** : début de la méthode Fire(), avant le spawn projectile.

```csharp
// === BEGIN STAGE B HOOKS ===
// 1. Audio
AudioController.Instance?.Play("tower_shoot", 0.55f);

// 2. Juice : light shake to convey impact
JuiceFX.Instance?.Shake(0.05f, 100);

// 3. VFX impact at projectile origin (muzzle flash)
VfxPool.Instance?.SpawnImpact(transform.position + Vector3.up * 0.5f, cfg.Color);

// 4. Animation trigger
_animController?.TriggerAttack();
// === END STAGE B HOOKS ===

// ... existing projectile spawn logic
```

**Notes** :
- `_animController` cached en field set dans Init() via `GetComponentInChildren<AnimationController>()`.
- Si AudioController absent (early scene load) → null-safe.

### I1.2 — Tower.OnPlaced() (called when player confirms placement)

```csharp
AudioController.Instance?.Play("tower_built", 0.7f);
VfxPool.Instance?.SpawnCoinPickup(transform.position);  // gold burst feel
```

### I1.3 — Tower.UpgradeTo(level, branch)

```csharp
AudioController.Instance?.Play("tower_upgrade", 0.8f);
JuiceFX.Instance?.Flash(new Color(1f, 0.9f, 0.4f, 0.3f), 200);  // gold flash
VfxPool.Instance?.SpawnCoinPickup(transform.position);
```

---

## I2 — Enemy.cs hooks

### I2.1 — Enemy.TakeDamage(amount) — non-fatal

```csharp
// Only if !willDie
if (HP > 0) {
    AudioController.Instance?.Play("enemy_hit", 0.4f);
    // VFX impact small puff
    VfxPool.Instance?.SpawnImpact(transform.position + Vector3.up * 0.3f, cfg.BodyColor);
}
```

### I2.2 — Enemy.Die()

```csharp
// Audio : tier-based clip
string clipKey = cfg.Tier switch {
    EnemyTier.Boss => "enemy_die_boss",
    EnemyTier.Medium => "enemy_die_medium",
    _ => "enemy_die_basic"
};
AudioController.Instance?.Play(clipKey, cfg.Tier == EnemyTier.Boss ? 1f : 0.5f);

// Juice : escalate by tier
if (cfg.Tier == EnemyTier.Boss) {
    JuiceFX.Instance?.Shake(0.3f, 400);
    JuiceFX.Instance?.SlowMo(0.3f, 800);
    JuiceFX.Instance?.Flash(Color.white, 250);
}

// VFX death
VfxPool.Instance?.SpawnDeath(transform.position, cfg.BodyColor, cfg.Tier == EnemyTier.Boss);

// Animation death (avant pool release)
_animController?.TriggerDeath();
```

### I2.3 — Enemy.UpdateWalk() (per frame, isMoving flag)

```csharp
// throttle : only when state changes
if (_wasWalking != isMoving) {
    _animController?.PlayWalk(isMoving);
    _wasWalking = isMoving;
}
```

---

## I3 — Castle.cs hooks

### I3.1 — Castle.TakeDamage(amount)

```csharp
AudioController.Instance?.Play("castle_hit", 0.65f);
JuiceFX.Instance?.Shake(0.1f, 200);
JuiceFX.Instance?.Flash(new Color(1f, 0.2f, 0.2f, 0.4f), 150);  // red flash
```

### I3.2 — Castle.OnDestroyed() (game over)

```csharp
AudioController.Instance?.Play("enemy_die_boss", 1f);  // dramatic
JuiceFX.Instance?.SlowMo(0.2f, 1500);
JuiceFX.Instance?.Flash(Color.black * 0.7f, 1000);
```

---

## I4 — WaveManager.cs hooks

### I4.1 — WaveManager.BeginWave(int waveIndex)

```csharp
AudioController.Instance?.Play("wave_start", 0.85f);
```

### I4.2 — WaveManager.OnWaveCleared(int waveIndex)

```csharp
AudioController.Instance?.Play("wave_clear", 0.7f);
JuiceFX.Instance?.Flash(new Color(0.4f, 1f, 0.4f, 0.25f), 300);  // green flash
```

---

## I5 — LevelRunner.cs hooks

### I5.1 — LevelRunner.OnLevelStart()

```csharp
// Music ambient start (Phase 3 = 1 track unique for all levels)
if (BalanceConfig.Instance?.AmbientMusic != null) {
    AudioController.Instance?.PlayMusic(BalanceConfig.Instance.AmbientMusic, 500);
}
```

### I5.2 — LevelRunner.OnLevelComplete()

```csharp
AudioController.Instance?.Play("level_up", 1f);
JuiceFX.Instance?.Flash(new Color(1f, 0.84f, 0f, 0.4f), 500);  // gold
JuiceFX.Instance?.SlowMo(0.5f, 1200);
AudioController.Instance?.StopMusic(800);
```

---

## I6 — Economy.cs hooks

### I6.1 — Economy.AddGold(amount) (player pickup, NOT wave reward)

```csharp
if (source == GoldSource.PlayerPickup) {
    AudioController.Instance?.Play("coin_pickup", 0.45f);
    VfxPool.Instance?.SpawnCoinPickup(pickupPosition);
}
```

### I6.2 — Economy.OnSpendFailed(reason) (no gold)

```csharp
AudioController.Instance?.Play("blue_pill", 0.5f);  // legacy "denied" cue, can swap
```

---

## I7 — BalanceConfig.cs additions

Le SO `BalanceConfig` doit exposer un field music + (optionnel) un AudioClipRegistry pointer si SO-AUDIO le choisit. Champs à ajouter :

```csharp
[Header("Audio Phase 3")]
[SerializeField] private AudioClip? ambientMusic;
public AudioClip? AmbientMusic => ambientMusic;
```

---

## Ordre d'application (Integrator)

```
1. Add using statements aux 6 hot zone files
2. Cache _animController dans Init() (Tower.cs + Enemy.cs)
3. Apply I1.1 Tower.Fire — verify compile
4. Apply I1.2/I1.3 Tower.OnPlaced/UpgradeTo
5. Apply I2.1/I2.2/I2.3 Enemy hooks
6. Apply I3 Castle
7. Apply I4 WaveManager
8. Apply I5 LevelRunner + I7 BalanceConfig
9. Apply I6 Economy
10. Compile : mcp__UnityMCP__refresh_unity → 0 errors
11. PlayMode test 30s : tirer 1 tour, voir 1 enemy die, voir 1 wave clear
12. Commit chain : `feat(integ): audio/juice/vfx/anim hooks Tower.cs` + `feat(integ): ... Enemy.cs` + etc.
```

---

## Coordination avec Axis F (UX SettingsRegistry)

Tous les calls aux singletons (`AudioController`, `JuiceFX`, `VfxPool`) doivent respecter user settings. Si SettingsRegistry n'est pas encore live au moment du hook → tolérer (null-safe). Les singletons eux-mêmes (cf C6) lisent SettingsRegistry pour respect (e.g., JuiceFX.Shake no-op si !ShakeEnabled).

---

## Coordination avec Axis G (QA)

Integrator triggere QA-4 (Post-Integration gate) à la fin. Si fail → Integrator revert ou fix-forward selon report.

---

## Notes Sub-Opus

Aucun Sub-Opus ne doit toucher aux hot zones. Si un Sub-Opus pense qu'un hook devrait être placé différemment :
- Document dans `.claude/coordination/requests/{axis}-integration-feedback.md`
- MO arbitre + update ce fichier

Cette spec est **vivante** : elle peut être ajustée jusqu'à ce que l'Integrator soit spawned. Après cela, freeze.
