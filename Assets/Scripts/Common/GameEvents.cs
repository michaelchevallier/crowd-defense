#nullable enable

using CrowdDefense.Entities;

namespace CrowdDefense.Common
{
    // Wave lifecycle
    public record WaveStartedEvent(int Index);
    public record WaveCompletedEvent(int Index);
    public record AllWavesCompletedEvent;

    // Tower lifecycle
    public record TowerPlacedEvent(Tower Tower);
    public record TowerSoldEvent(Tower Tower, int Refund);
    public record TowerUpgradedEvent(Tower Tower, int NewLevel);

    // Enemy lifecycle
    public record EnemySpawnedEvent(Enemy Enemy);
    public record EnemyKilledEvent(Enemy Enemy, int Reward);
    public record EnemyReachedCastleEvent(Enemy Enemy, int Damage);

    // Economy
    public record CoinsChangedEvent(int Previous, int Current, int Delta);
    public record InterestEarnedEvent(int Amount);

    // Castle health
    public record CastleHitEvent(int Damage, int RemainingHp);
    public record CastleDestroyedEvent;

    // Level lifecycle
    public record LevelStartedEvent(int LevelIndex);
    public record LevelEndedEvent(int LevelIndex, bool Victory);

    // Gameplay modifiers (from V5 EventManager scenario types)
    public record GameEventStartedEvent(string EventType, string DisplayName, float Duration);
    public record GameEventEndedEvent(string EventType);

    // Music / audio
    public record LevelThemeChangedEvent(string ThemeName);
    public record BossEncounteredEvent;
}
