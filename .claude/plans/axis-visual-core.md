# Axis VISUAL-CORE — Plan évolutif Sub-Opus

**Date** : 2026-05-11
**Branch** : `axis/visual-core` (worktree `agent-ac66eb553ebaa57db`)
**Sub-Opus** : SO-VISUAL
**Budget** : 8-12h work total

## Mission

Finaliser éléments visuels Phase 3 manquants :
1. **A.1** VfxPool.cs (singleton + 4-pool ObjectPool ParticleSystem)
2. **A.2** 4 prefabs VFX (Impact / Death / Aura / CoinPickup) via UnityMCP
3. **A.3** Boss shaders Jellyfish (kraken) + Hologram (cosmic) — optionnel
4. **A.4** Migration ToonShader HLSL → Shader Graph URP — optionnel

## QA-1 Pre-Spawn checklist

- [x] Lu `file-ownership.md` → zone confirmée (`Assets/Scripts/Visual/`, `Assets/Shaders/`, `Assets/Materials/`, `Assets/Prefabs/VFX/`)
- [x] Lu `api-contracts.md` → C3 VfxPool signature canon
- [x] Plan écrit ici
- [x] Branche `axis/visual-core` créée depuis main HEAD du worktree

## Décisions architecturales

### A.1 VfxPool design

- Singleton héritant de `MonoSingleton<VfxPool>` (pattern AudioController/JuiceFX).
- 4 `ObjectPool<ParticleSystem>` séparés (Impact, Death, Aura, CoinPickup), API `UnityEngine.Pool`.
- Préallocation 50 par type, MaxSize 200 (cf brief).
- Prefab refs via 4 `[SerializeField] GameObject` exposés Inspector.
- Tinting via `MainModule.startColor` (1-time set après Get du pool).
- Auto-release via coroutine "wait lifetime, then release" pour les bursts. Pour Aura (parented continuous), retourne le ParticleSystem pour stop manuel.
- Respect `SettingsRegistry.Instance?.VFXEnabled` → guarded `false` = no-op (le SettingsRegistry n'existe pas encore, donc check via reflection ou null-safe `(SettingsRegistry.Instance?.VFXEnabled ?? true)`).

**Note SettingsRegistry absence** : Axis F UX doit livrer `Assets/Scripts/UI/SettingsRegistry.cs`. En attendant, VfxPool fait un null-safe check qui retourne true par défaut.

### A.2 Prefabs

- ParticleSystem standard (Shuriken, pas VFX Graph — WebGL compat).
- Pas d'Audio dépendance (VFX visuel pur, l'audio est sous AudioController contract C1).
- Layer `Default`, no collider.

| Prefab | Burst/Cont | Particles | Lifetime | Shape | Notes |
|---|---|---|---|---|---|
| Impact | burst | 10-15 | 0.3s | Cone, angle 45 | upward expand |
| Death | burst | 20 | 0.5s | Sphere, radius 0.3 | radial outward |
| Aura | continuous | 10/s | 1.0s | Circle, radius 0.5 | upward gentle |
| CoinPickup | burst | 8 | 0.6s | Cone narrow, angle 25 | upward gold |

### A.3 / A.4 (optionnel)

Si time permet. Skip si A.1+A.2+QA passent à ~6h.

## Workflow planifié (séquence + parallèle)

```
T0  : Pre-Spawn QA-1 done ✓
T1  : Spawn Sonnet A1 (VfxPool.cs)        ← pure C#, no Unity needed initial
T2  : Spawn Sonnet A2 (4 prefabs VFX)      ← UnityMCP, parallèle avec A1 OK
T3  : Wait A1 + A2 complete → QA-2 per commit
T4  : Spawn Sonnet QA-2 (Sonnet QA gate validation)
T5  : (optionnel) Spawn Sonnet A3 (Jellyfish + Hologram shaders)
T6  : (optionnel) Spawn Sonnet A4 (Toon Shader Graph migration)
T7  : QA-3 pre-merge gate Sonnet
T8  : Push axis/visual-core sur origin
T9  : Rapport final
```

**Parallélisme** : A1 (pure C#) et A2 (UnityMCP) peuvent runner en parallèle car ils touchent des zones différentes. A3+A4 (shaders) peuvent également runner en parallèle entre eux après A1+A2.

**Note worktree vs main repo** :
- Le worktree agent-ac66eb553ebaa57db a sa propre copie de fichiers.
- Unity Editor tourne sur `/Users/mike/Work/crowd-defense` (main repo).
- Sonnet A2 (UnityMCP) doit créer les prefabs dans le main repo via mcp__UnityMCP__, puis copier les .prefab + .meta vers le worktree path pour commit sur axis/visual-core.

## Risks

- **R1** : SettingsRegistry absent → null-safe check VfxPool. OK.
- **R2** : Worktree vs main repo files diverge si Unity importe automatiquement dans main. Mitigation : Sonnet A2 sait copier les artefacts depuis main repo path vers worktree path.
- **R3** : ObjectPool<ParticleSystem> recyclage : doit Stop+Clear avant Release pour ne pas leak particules anciennes. Pattern à implémenter explicitement.
- **R4** : Aura continuous parent transform : si parent destroyed avant Release, on doit re-parent à VfxPool root pour éviter NullRef.

## Acceptance criteria Stage A

- [ ] `Assets/Scripts/Visual/VfxPool.cs` compile + matches C3 signature
- [ ] 4 prefabs dans `Assets/Prefabs/VFX/` visibles Inspector
- [ ] Aucun Hot Zone modifié (Tower.cs, Enemy.cs, etc.)
- [ ] Console clean après `mcp__UnityMCP__refresh_unity`
- [ ] QA-3 ship-gate pass
- [ ] axis/visual-core pushed sur origin
- [ ] Rapport final écrit `.claude/coordination/axis-visual-core-report.md`
