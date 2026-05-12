# Interventions Manuelles Demandées à Mike

> Maj automatique par Opus. Si entry présente, Mike doit la traiter dès qu'il revient.
> Une fois traitée, supprimer ou marquer ✅ DONE.

---

## 🔴 EN COURS (action requise)

_Aucun item bloquant — Build #36 deployé `ec1d1aa` à 02:18 le 12 mai 2026._

URL en live : https://michaelchevallier.github.io/crowd-defense/v6/

Backup local du build : `/tmp/webgl-safe-2026-05-12-02h13/WebGL/`

---

## 🟡 PRÉPARÉS (à valider quand temps)

- **Auto-build cron** : `tools/auto-build-loop.sh` créé (6717d09). Lance avec `bash tools/auto-build-loop.sh > /tmp/auto-build.log 2>&1 &` si tu veux build/deploy auto toutes les 8 min. Non actif par défaut.

- **GhostPreviewController** (f884148) : agent recommande d'attacher manuellement le component `GhostPreviewController` à un GameObject vide dans `Main.unity` via Unity Editor. SetupMainScene devrait le faire mais à vérifier.

- **URP package** (e7759e6) : à l'ouverture Editor Unity, `[InitializeOnLoadMethod]` créera `Assets/Settings/URP_PipelineAsset.asset`. Si pas créé : `Tools/CrowdDefense/Setup URP` (menu à vérifier).

---

## ✅ DONE (récents, historique)

- Mixamo manuel auth — Mike confirme "etape mixamo fait" 2026-05-11
- Permission Chrome MCP — OK
- Unity Hub install + activation — OK

---

## 📋 NICE-TO-HAVE (long terme, pas urgent)

- Test live le jeu sur device mobile réel (iOS Safari + Android Chrome) pour valider touch UX
- Build iOS Xcode local quand iso V4 confirmé (deferred deployment per Mike instruction)
- Steam SDK setup quand on attaque le déploiement desktop (pareil)
- Manual playtest pour évaluer fun + balance après iso V4

---

## 💡 IDÉES OPPORTUNITÉS UNITY (vs Three.js V5)

> Améliorations possibles que je vois en migrant, à valider quand temps. Mike : 👍 ou ❌ chacune.

### Animations
- **Skeletal animation hero/enemies** : V5 = static meshes ou rotation manuelle. Unity Animator state machine permet Idle/Walk/Attack/Death avec blends + IK. KayKit GLBs ont déjà les clips → exploitation immédiate.
- **Tower attack anim** : V5 = projectile spawn statique. Unity peut faire turret rotate vers cible + recoil + muzzle flash particle synchronisés.
- **Castle dégâts visuels** : crack mesh swap + smoke particles + light flicker progressif selon HP%. V5 ne faisait que tint rouge.
- **Death ragdoll enemies** : Unity Physics ragdoll au lieu de fade simple. ~10-15min/enemy mais énorme impact "juice".

### Skins
- **Hero skin variants color/texture swap runtime** : Material instances per skin sans recompile shader. V5 avait swap mesh complet, Unity peut juste swap MaterialPropertyBlock (perf++).
- **Tower skin upgrade chain visuel** : L1/L2/L3 doivent avoir mesh diff visible. KayKit a déjà variants (archer normal → veteran → master). Auto-wire via AssetRegistry pattern existant.
- **Boss skin per phase** : apocalypse phase 1/2/3 = scale + color shift + emissive ramp. V5 ne le faisait pas ; Unity Shader Graph permet smooth transitions.

### Effets visuels
- **VFX Graph URP** au lieu de ParticleSystem : meshes GPU-instanced pour skill VFX massifs (Hero ult AoE, Tower frost field, Boss fire breath). Plus performant + plus stylé.
- **Post-processing URP** : Bloom léger sur emissive, vignette danger sub-30%HP castle, chromatic aberration sur slowfreeze. V5 n'avait rien de tout ça.
- **Decal projection** sur sol pour Tower range ring (au lieu de LineRenderer cercle plat). URP Decal Projector = look propre.
- **Trail Renderer** sur projectiles fast (cannon ball, ballista bolt) — V5 avait juste sprite.

### Audio
- **3D spatial audio** : ennemis tirent → son localisé. Tower fire → pan stéréo selon position screen. V5 = mono UI sounds only.
- **Adaptive music layers** : Wave calm → drums add → boss = full ensemble. Audio Mixer URP permet smooth crossfades.

### Performances Unity-only
- **GPU Instancing** matériaux towers/enemies identiques → 1 draw call N entities (au lieu de N). Critical mobile.
- **Job System Burst** pour pathing si > 100 enemies simultanés (V5 plafonne à ~30 sans lag).
- **Texture atlas baking** Editor : compresse les 832 GLTFs en 1 atlas par catégorie → -50% mémoire, -90% draw calls.

### Gameplay nouvelle dimension (Unity capabilities)
- **Cinemachine cutscenes** : caméra Boss intro / Victory zoom dramatique. V5 ne pouvait pas.
- **Cloth physics cape Hero** : KayKit Mage a une cape, simulation cloth dynamique.
- **Volumetric fog/clouds** per thème : Espace = nebula, Volcan = embers fog, Submarin = caustics underwater. URP supporte tout ça out-of-box.
- **Wind shader** sur arbres/herbe : V5 = static decor. Unity Shader Graph vertex anim sur SceneDecor props.

Mike : marque 👍 sur ce qui t'intéresse, je dispatche les agents.

---

_Dernière maj : auto par Opus. Cron V4-diff a6471edb fire chaque :13/:43._
