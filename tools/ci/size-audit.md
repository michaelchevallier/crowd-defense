# WebGL Build Size Audit

## Baseline (Phase 1 POC, avant optims Axis BUILD)

Build généré le 2026-05-11 16:53 via `BuildScript.BuildWebGL()` (ancien output `Builds/WebGL/`).

| Asset | Taille compressée | % |
|---|---|---|
| `Build/WebGL.wasm.unityweb` | 5.1 MB | 78% |
| `Build/WebGL.data.unityweb` | 1.1 MB | 17% |
| `Build/WebGL.loader.js` | 115 KB | 1.7% |
| `Build/WebGL.framework.js.unityweb` | 73 KB | 1.1% |
| `TemplateData/` | 48 KB | 0.7% |
| **Total** | **6.5 MB** | 100% |

**Compression** : Brotli (default Unity WebGL 6.x).
**IL2CPP** : enabled.
**Managed stripping** : Medium (Unity default).
**Engine code stripping** : disabled.
**Decompression fallback** : enabled (cohérent avec serveurs HTTP sans support Brotli native).

## Cible Phase 3-4

- **<30 MB compressé** : confortable pour `/v6/` hosted GitHub Pages (max 100 MB par file Pages, mais on vise UX rapide load).
- **<10 MB load <3s sur 50 Mbps** : objectif UX desktop.

## Post-Optims (à mesurer après next build via `BuildScript.BuildWebGL()` refactor)

Optims appliquées dans `BuildScript.cs` :
- `PlayerSettings.SetIl2CppCompilerConfiguration(WebGL, Master)` — production-optimized, plus lent à compile mais plus petit
- `PlayerSettings.SetManagedStrippingLevel(WebGL, ManagedStrippingLevel.High)` — strip aggressif IL inutile
- `PlayerSettings.stripEngineCode = true` — strip modules Unity inutilisés (UNet, IMGUI, Cloth, etc.)
- `PlayerSettings.WebGL.exceptionSupport = WebGLExceptionSupport.None` — disable exception stack traces (production)
- `PlayerSettings.WebGL.dataCaching = true` — assets cached browser side
- `PlayerSettings.WebGL.threadsSupport = false` — single thread, simpler runtime

**Risques** :
- `Stripping High` peut casser le runtime si du code utilise réflexion (e.g., `AnimationController.SetTrigger(string)`). Mitigation : `link.xml` whitelist nécessaire si fail. Surveillance console post-build requise.
- `IL2CPP Master` ralentit le build (~30-60s additionnel) mais petit.
- `exceptionSupport = None` cache les stack traces JS — debugging plus dur en prod mais size win significatif.

## Mesure post-build (TODO Mike après merge axis/build)

À exécuter une fois `axis/build` mergée + Unity Editor re-build WebGL :

```bash
cd /Users/mike/Work/crowd-defense
# Trigger via Editor menu : CrowdDefense > Build > WebGL
# OU batch CLI :
"/Applications/Unity/Hub/Editor/6000.3.15f1/Unity.app/Contents/MacOS/Unity" \
  -batchmode -nographics \
  -projectPath . \
  -executeMethod CrowdDefense.Build.BuildScript.BuildWebGL \
  -quit

# Mesurer
du -sh Build/WebGL/
ls -lh Build/WebGL/Build/
```

Remplir le tableau ci-dessous après cette mesure :

| Asset | Avant | Après | Delta |
|---|---|---|---|
| `Build/WebGL.wasm.unityweb` | 5.1 MB | ? | ? |
| `Build/WebGL.data.unityweb` | 1.1 MB | ? | ? |
| `Build/WebGL.loader.js` | 115 KB | ? | ? |
| `Build/WebGL.framework.js.unityweb` | 73 KB | ? | ? |
| `TemplateData/` | 48 KB | ? | ? |
| **Total** | **6.5 MB** | **? MB** | **?% reduction** |

## Recommandations futures (post Phase 3)

Une fois les 12 towers + 30 enemies + 80 levels + audio + textures intégrés, on aura probablement 30-80 MB compressed sans optims. Pour rester sous la cible :

1. **Sprite Atlas** : packer UI sprites (`Assets/UI/*`) dans atlas .spriteatlas → réduit overdraw + filesize.
2. **Addressables** : split content en bundles loadable on-demand (e.g., world levels lazy-loaded). Doc : https://docs.unity3d.com/Packages/com.unity.addressables@2.6/manual/index.html
3. **Texture compression cross-platform** :
   - WebGL : ASTC ou DXT (via Editor → Build Settings → Build Profile per platform).
   - Mobile : ETC2 (Android), PVRTC ou ASTC (iOS).
4. **Audio Vorbis Q4-Q6** : déjà OGG en source, vérifier l'import settings 22050 Hz + mono pour SFX, 44100 stereo pour musique.
5. **Asset Bundles separation** : level data en bundles séparés du core game.
6. **Mesh compression** : `ModelImporter.meshCompression = Medium/High` sur les GLTF imports → 50-80% reduction file size, légère perte précision.

## Notes

- Brotli compression ratio typique : 25-30% vs uncompressed pour wasm.
- Gzip fallback (sans Brotli serveur) : ratio 30-40%, donc bundle uncompressed peut être lourd. Hosting gh-pages supporte Brotli native depuis 2023.
- Load time first-visit : ~1.5-3s sur 50 Mbps pour 6.5 MB (acceptable). Avec assets Phase 3 ajoutés, viser <10s premier load.
