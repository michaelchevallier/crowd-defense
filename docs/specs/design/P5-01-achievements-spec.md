# P5-01 Achievements — Integration Spec (Phase 5.B hooks TBD)

## Architecture

- `AchievementDef` (SO): id, titleKey, descKey, hidden, points, icon
- `AchievementRegistry` (SO): array of all defs, Dict cache, loaded via `Resources/AchievementRegistry`
- `Achievements` (MonoSingleton): HashSet unlock store, PlayerPrefs persistence, OnUnlocked event

## PlayerPrefs keys

| Key | Type | Content |
|-----|------|---------|
| `cd.achievements.unlocked` | string | CSV of unlocked ids |
| `cd.ach.counter.<eventKey>` | int | cumulative event counter |

## Public API

```csharp
Achievements.Instance?.Unlock("boss_killer");
Achievements.Instance?.IsUnlocked("boss_killer");     // bool
Achievements.Instance?.CompletionRatio;               // float 0..1
Achievements.Instance?.TrackEvent("enemy_killed", 1);
Achievements.Instance?.GetEventCount("enemy_killed"); // int
```

## Phase 5.B hot zone hooks (TBD)

These calls must be added in the listed methods. They are NOT in the Phase 5.A scope to avoid touching hot zones.

| Call site | Method | Code |
|-----------|--------|------|
| `Enemy.cs` | `Die()` | `Achievements.Instance?.TrackEvent("enemy_killed", 1);` |
| `Tower.cs` | `OnPlaced()` | `Achievements.Instance?.TrackEvent("tower_placed", 1);` |
| `WaveManager.cs` | `OnWaveCleared()` | `Achievements.Instance?.TrackEvent("wave_cleared", 1);` |
| `Economy.cs` | `AddGold(int amount)` | `Achievements.Instance?.TrackEvent("gold_earned", amount);` |
| `LevelRunner.cs` | `OnLevelWin()` | `Achievements.Instance?.TrackEvent("level_complete", 1, levelId); if (castleHp == castleMaxHp) Achievements.Instance?.Unlock("untouched_castle");` |
| `Synergies.cs` | `Activate(string synergyId)` | `Achievements.Instance?.TrackEvent("synergy_activated", 1);` |

## Predicate evaluation (Phase 5.B)

`Achievements.TrackEvent` currently only persists counters. Phase 5.B must add predicate checking after each increment:

```csharp
// Example in CheckCounterAchievements (to implement in Achievements.cs):
if (eventKey == "enemy_killed" && current >= 1) Unlock("first_blood");
if (eventKey == "tower_placed" && current >= 10) Unlock("tower_master");
if (eventKey == "wave_cleared" && current >= 10) Unlock("wave_clear_10");
if (eventKey == "boss_killed" && current >= 5) Unlock("boss_killer");
if (eventKey == "gold_earned" && current >= 1_000_000) Unlock("million_gold");
if (eventKey == "synergy_activated" && current >= 5) Unlock("synergy_master");
// level_complete with context: check if all W1-* cleared → world1_complete
// tutorial_done: Unlock on level_complete with context "world1-1"
// speedrun_w1_1: requires LevelRunner to pass elapsed time via context, unlock if < 60s
```

## Toast UI (Phase 5.B)

Replace the TODO stub in `Achievements.Unlock`:

```csharp
// TODO Phase 5.B: show toast notification UI (AchievementToastController.Show(id))
```

`AchievementToastController` will be a UI MonoBehaviour in `Assets/Scripts/UI/` with a queue and slide-in/out animation.
