# API Contracts — Multi-Opus Swarm Interfaces

**Date** : 2026-05-11
**Purpose** : définir les **interfaces stables** entre axes avant que les 7 Sub-Opus ne livrent leurs Stage A. Sans contracts, deux axes peuvent inventer des APIs incompatibles et l'Integrator (Stage B) ne pourra pas connecter.

**Règle** : si un Sub-Opus modifie un contract → il documente la divergence ici + ping MO via `.claude/coordination/requests/`. MO arbitre.

---

## C1 — AudioController API (Axis B AUDIO → consumed by hot zones)

```csharp
namespace CrowdDefense.Systems;

public class AudioController : MonoSingleton<AudioController> {
    // Play a SFX by string key. Lookup via AudioClipRegistry.
    // volMul: 0-1 multiplier (default 1f).
    // Anti-replay : 28ms min between same clip plays.
    public void Play(string clipKey, float volMul = 1f);

    // Play a random clip from a list of keys (e.g., variants enemy_die_basic_1/2/3).
    public void PlayRandom(string[] clipKeys, float volMul = 1f);

    // Music separate AudioSource (loop = true).
    public void PlayMusic(AudioClip clip, float fadeMs = 500);

    // Stop music with optional fade.
    public void StopMusic(float fadeMs = 500);

    // Bind to Settings UI sliders.
    public void SetMasterVolume(float zeroToOne);
    public void SetSFXVolume(float zeroToOne);
    public void SetMusicVolume(float zeroToOne);
    public void SetMuted(bool muted);
}
```

**Clip Keys conventions** (canon — ne PAS inventer de nouveaux noms hors cette liste sans MO ack) :

| Key | Trigger gameplay | Source axe |
|---|---|---|
| `tower_shoot` | Tower.Fire() | hot zone |
| `tower_built` | Tower placement validated | hot zone |
| `tower_upgrade` | Tower.UpgradeTo() | hot zone |
| `enemy_hit` | Enemy.TakeDamage() (non-fatal) | hot zone |
| `enemy_die_basic` | Enemy.Die() tier basic | hot zone |
| `enemy_die_medium` | Enemy.Die() tier medium | hot zone |
| `enemy_die_boss` | Enemy.Die() boss tier | hot zone |
| `castle_hit` | Castle.TakeDamage() | hot zone |
| `wave_start` | WaveManager.BeginWave() | hot zone |
| `wave_clear` | WaveManager.OnWaveCleared() | hot zone |
| `level_up` | LevelRunner.LevelComplete() | hot zone |
| `coin_pickup` | Economy.AddGold() (player-triggered) | hot zone |
| `boom` | Mine explosion / boss death | hot zone |
| `boss_charge` | Boss.PreAttack() | hot zone |
| `achievement` | Achievements.Unlock() (Phase 5) | UX axis |
| `hero_shoot` | Hero.Fire() (legacy Phaser, unused Unity Phase 3) | — |
| `gem_gain` | Treasure tile pickup | hot zone |
| `perk_pick` | Perk choice modal (Phase 5) | UX axis |
| `skin_equip` | Skin selector (Phase 5) | UX axis |
| `blue_pill` | Easter egg (skip) | — |

---

## C2 — JuiceFX API (Axis A VISUAL → consumed by hot zones)

```csharp
namespace CrowdDefense.Visual;

public class JuiceFX : MonoSingleton<JuiceFX> {
    // Camera shake : random offset additif insideUnitSphere.
    // intensity : 0.05 = subtle, 0.2 = boss roar, 0.5 = catastrophic.
    public void Shake(float intensity, int durationMs);

    // Full-screen flash overlay UI Toolkit transient.
    // color : Color.white standard, red pour castle damage, gold pour level up.
    public void Flash(Color color, int durationMs);

    // Time.timeScale change avec auto-restore.
    // timeScale : 0.3 typique pour boss death.
    public void SlowMo(float timeScale, int durationMs);

    // Permet à un follow-camera de update sa position base sans casser shake.
    public void SetBaseCamPos(Vector3 pos);
}
```

**Usage conventions** :
- Tower.Fire() → `Shake(0.05f, 100)`
- Enemy.Die() boss → `Shake(0.3f, 400)` + `SlowMo(0.3f, 800)` + `Flash(Color.white, 250)`
- Castle.TakeDamage() → `Shake(0.1f, 200)` + `Flash(Color.red * 0.5f, 150)`
- WaveManager.OnWaveCleared() → `Flash(new Color(1f, 0.84f, 0f, 1f), 300)` (gold flash)

---

## C3 — VfxPool API (Axis A VISUAL → consumed by hot zones)

```csharp
namespace CrowdDefense.Visual;

public class VfxPool : MonoSingleton<VfxPool> {
    // Spawn impact VFX au point d'impact projectile.
    public void SpawnImpact(Vector3 worldPos, Color tint);

    // Spawn death VFX position enemy.
    public void SpawnDeath(Vector3 worldPos, Color tint, bool isBoss = false);

    // Continuous aura attaché à un GameObject (tower aura, boss aura).
    // Return l'instance VFX pour qu'on puisse la stopper plus tard.
    public ParticleSystem SpawnAura(Transform parent, Color tint, bool isBoss = false);

    // Spawn coin pickup VFX (gold particle burst).
    public void SpawnCoinPickup(Vector3 worldPos);
}
```

**Color conventions** : la couleur passe correspond au `EnemyType.BodyColor` ou `TowerType.Color` SO field. Lookup via cfg.

---

## C4 — AnimationController API (Axis A VISUAL → consumed by hot zones)

```csharp
namespace CrowdDefense.Visual;

public class AnimationController : MonoBehaviour {
    public void PlayWalk(bool walking);     // bool param isWalking
    public void TriggerAttack();             // trigger attackTrigger
    public void TriggerDeath();              // trigger dieTrigger
    public bool HasState(string state);
}
```

**Attachement** : composant ajouté au prefab Enemy/Tower au runtime via `GetComponentInChildren<Animator>()`. Si pas d'Animator (asset GLTF sans clips) → méthodes no-op silent.

---

## C5 — Localization API (Axis F UX → consumed by all UI)

Unity Localization Package canon (preferred over custom).

```csharp
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

// Static helper convenience.
namespace CrowdDefense.UI;

public static class L {
    // Get localized string by key + table.
    public static string Get(string key, string table = "UI");

    // Get with format args (e.g., "Wave {0}/10").
    public static string Get(string key, params object[] args);

    // Change locale dynamically.
    public static void SetLocale(string code);  // "en", "fr", "es"...
}
```

**Tables Unity Localization** :
- `UI` : labels HUD, menus, settings, tutorials
- `Towers` : tower names + descriptions
- `Enemies` : enemy names + flavor text
- `Levels` : level briefings + names
- `Achievements` : achievement strings (Phase 5)

**Conventions key naming** :
- `hud.wave_label` → "Wave"
- `hud.gold_label` → "Gold"
- `tower.archer.name` → "Archer"
- `tower.archer.desc` → "Quick single-target shooter..."
- `level.w1_1.briefing` → "Welcome to the foire..."

**Stratégie i18n par axe** : chaque axe ajoute ses keys dans la table appropriée. Pas de touch direct au LocaleSelector logic (Axis F UX owns).

---

## C6 — SettingsRegistry API (Axis F UX → consumed by AudioController + others)

```csharp
namespace CrowdDefense.UI;

public class SettingsRegistry : MonoSingleton<SettingsRegistry> {
    // Audio
    public float MasterVolume { get; set; }   // 0-1, persisted PlayerPrefs
    public float SFXVolume { get; set; }
    public float MusicVolume { get; set; }
    public bool Muted { get; set; }

    // Graphics
    public int QualityLevel { get; set; }     // 0=mobile, 1=desktop, 2=high
    public bool VFXEnabled { get; set; }      // disable particles for perf
    public bool ShakeEnabled { get; set; }    // accessibility (motion sickness)

    // Accessibility
    public bool ColorblindMode { get; set; }
    public bool ReduceMotion { get; set; }
    public bool LargeText { get; set; }

    // Persistence
    public void Save();   // PlayerPrefs write
    public void Load();   // PlayerPrefs read on Awake
}
```

**Hook** : AudioController, JuiceFX, VfxPool lisent SettingsRegistry.Instance pour respecter user prefs.
- `JuiceFX.Shake()` no-op si `!SettingsRegistry.Instance.ShakeEnabled`.
- `VfxPool.Spawn*()` skip si `!VFXEnabled`.

---

## C7 — Build profiles API (Axis E BUILD → orchestrated by MO + CI)

```csharp
namespace CrowdDefense.Editor;

public static class BuildScript {
    public static void BuildWebGL();        // existing, used /v6/ deploy
    public static void BuildOSX();          // Mac standalone
    public static void BuildWindows();      // Win64 standalone
    public static void BuildLinux();        // Linux64 standalone
    public static void BuildIOS();          // iOS Xcode export (requires macOS host)
    public static void BuildAndroid();      // Android APK + AAB
    public static void BuildAll();          // calls all desktop targets (Mac/Win/Linux) in sequence
}
```

**Conventions** :
- Output paths : `Build/<platform>/` (NOT `Builds/` legacy).
- Bundle ID : `com.crowddefense.game` (configurable in BuildScript).
- Build options : IL2CPP, Brotli compression (WebGL), code stripping enabled.
- Version : pulled from `Application.version` set in ProjectSettings (Mike updates manually pre-release).

---

## C8 — Save System API (existing, Axis G QA validates)

```csharp
namespace CrowdDefense.Systems;

public class SaveSystem : MonoSingleton<SaveSystem> {
    public void MarkLevelCleared(string levelId);
    public bool IsLevelCleared(string levelId);
    public bool IsLevelUnlocked(string levelId);
    public string GetNextLevelId();
    public void ResetProgress();   // for testing + settings "reset" button
}
```

---

## Conformance checklist (chaque Sub-Opus doit confirmer)

Avant de spawn ses Sonnets, le Sub-Opus doit :
1. Lire ce fichier.
2. Si son Stage A définit ou consomme une API listée ici → respecter la signature exactement.
3. Si son Stage A a besoin d'une API absente d'ici → écrire dans `.claude/coordination/requests/{timestamp}-{axis}-api-add.md` pour proposer une extension. MO valide ou ajuste.
4. Ne JAMAIS inventer un clip_key, un L() key, ou une signature de method sans valider ici d'abord.
