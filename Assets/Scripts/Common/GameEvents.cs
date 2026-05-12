#nullable enable

using CrowdDefense.Entities;
using UnityEngine;

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

    // Combo (multi-kill streak)
    // Level = 1-based combo count (2=x1.5, 3=x2, …), Multiplier = coin reward factor
    public record ComboUpdatedEvent(int Level, float Multiplier);
    public record ComboResetEvent;

    // Music / audio
    public record LevelThemeChangedEvent(string ThemeName);

    // Synergies
    public record SynergyPairActivatedEvent(string FromType, string ToType, string Label);

    // Boss lifecycle
    public record BossWarningEvent(string DisplayName);
    public record BossEncounteredEvent(string DisplayName, float MaxHp, Color AuraColor, Vector3 BossPos, string[] CutsceneLines);
    public record BossHpChangedEvent(float Ratio);
    public record BossPhaseChangedEvent(string PhaseName, int PhaseIdx);
    public record BossDefeatedEvent(string DisplayName);
    public record BossChargeWarningEvent;

    // Hero
    public record HeroLevelUpEvent(int NewLevel, int Xp, int XpToNext);
    public record HeroDamagedEvent(int Damage);

    // Perks
    public record PerkPickedEvent(string PerkId, string HeroId);
}
