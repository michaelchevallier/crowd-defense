# Remaining V4→V6 Gaps — Recon + Top 5 Next Tickets (2026-05-13 00h)

> Audit lecture-seule post wave-1/2/3/4 + sister sessions late (`b05115c`..`142b01c`). Source : audits `ad3804d`, `adb68ee`, `2026-05-12-scene-wires-audit`, `2026-05-12-23h30-perf-10waves`. HEAD `142b01c`.

## 1. Status courant V6

- **Code-level parity** : **98-99%** (post `f6eeff4` wizard_king + `cf71283` Achievements + `c36b580` cutscenes + `acf4865` textures-wire + `b94d990` audio fallback + `79db3e0` player-loop BuildPoint + `b05115c` Hero ULT R-key + `276cd00` 5-wave QA PASS)
- **User-facing visible parity** : **75-82%** (post WAVE-4 + sister sessions). Mike audit `adb68ee` honest était 45-65% → +25-30% delta progressé sprint.
- **Gap résiduel** : (a) perf bottlenecks 3 fixes audit `1ab7216` jamais shippés, (b) juice/polish (screenshake/tweens cues), (c) animator state machines (T-pose risk runtime), (d) SFX clips per-tower (`b94d990` fallback procédural en place mais clips réels manquent), (e) particle VFX visible at wave-time.

## 2. Remaining gaps par catégorie

| Catégorie | Gap résiduel | Effort | Source audit |
|---|---|---|---|
| **Perf** | EnemyPathingSystem.Tick() jamais appelé + MuzzleFlash new/Destroy/s + HasAnimatorParam.parameters allocation/frame | 30 LOC | perf-10waves bottlenecks #1, #2, #3 |
| **Audio** | SFX clips wiring (towerShoot/enemyHit/enemyDie/boom/ult/noGold/cancel) — code 100% mais clips non-assigned | scene-only | adb68ee §1 (Audio 0-30%) |
| **Animator** | Hero/Enemy AnimatorController state machines T-pose risk + transitions broken | runtime test | adb68ee §5 (Anim 10-20%) |
| **Polish** | Screenshake event-based wired ? + Tweens UI (toolbar pulse, gold counter ease) + Boss intro 2s music swap test | 50 LOC + test | adb68ee §6 + JuiceFX présent code |
| **Visual** | Tower/Enemy materials per-theme (AssetVariants.ApplyTheme) — code existe (`19` Status FULL) mais runtime verify ? | runtime test | v4-parity-gap §2.1 |

## 3. Top 5 NEW tickets candidates (piochage continu)

| # | Title | Scope LOC | Priority | Parallel-safe | File conflict | Effort |
|---|---|---|---|---|---|---|
| **N1** | perf-3fix : EnemyPathingSystem.Tick() activation + MuzzleFlash pool 6 lights + HasAnimatorParam cache | 30 | **P0** | YES (LevelRunner+Tower.Combat+Enemy.Init isolés) | low (3 files distincts) | 1-2h |
| **N2** | audio-sfx-clips-import : import 8 real SFX clips (towerShoot/enemyHit/die/boom/ult/noGold/cancel/waveStart) replacing procedural fallback `b94d990` | clips+wire YAML | **P0** | YES (AudioController + Resources/Audio) | low (scene Main.unity) | 1h |
| **N3** | animator-debug-validate : Hero+Enemy AnimatorController state-machine runtime test + transitions verify via Play mode | test+fix | **P1** | NO (touche AnimatorController.controller assets) | medium (`Assets/Animators/*.controller`) | 2-3h |
| **N4** | juice-screenshake-validate : JuiceFX events wiring confirm (enemy death + tower fire + boss spawn) + tweens UI Toolbar pulse/gold ease | 50 LOC test | **P1** | YES (JuiceFX + Toolbar isolés) | low | 1-2h |
| **N5** | sfx-completeness-audit : Tower.Combat.cs `GetClip(fireSfxKey)` lookup verify per-tower-type 13 keys exist + warn-once registry | 20 LOC | **P2** | YES (AudioController isolé) | low | 1h |

## 4. Recommandation priorité piochage suivant

1. **Pioche N1 perf-3fix EN PREMIER** : 3 bottlenecks identifiés audit perf-10waves `1ab7216` jamais shippés, impact mesurable wave 5-10 (50+ mobs). Petit, parallel-safe, P0.
2. **Pioche N2 audio-sfx-wire EN PARALLÈLE** : scene-wiring T0-CRITICAL audit `adb68ee` (visible parity 0-30% audio). Mike a déjà wired MusicManager (`cdfe829`) mais SFX clips manquent. Scene-only, fast win.
3. **Différer N3 animator-debug** : requiert Play mode validation + UnityMCP session (blocked `23h30-perf-10waves`). À pioche post-N1+N2 quand Unity Editor open + UnityMCP réactivé.
4. **N4 + N5** : polish, peuvent attendre. N4 = juice good-feel ; N5 = audit completeness (catch missing SFX keys post-N2 wiring).

**Reco track final** : **N1 + N2 swarm parallèle** (1 Sonnet perf-track + 1 Sonnet audio-track worktrees isolés). Aucun conflit fichiers. ~2-3h pour passer **75-82% → 87-92% visible parity** post-merge.

---
*Audit lecture-seule, ~25 min, HEAD 142b01c.*
