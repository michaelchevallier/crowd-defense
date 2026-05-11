# CI / Builds Setup — Phase 4 credentials prep

Setup pour activer les builds automatiques sur GitHub Actions + prep des credentials Phase 4 (Steam, Apple, Google Play). À suivre par Mike step-by-step.

## 1. Unity License pour GitHub Actions (REQUIS pour activer les workflows)

Les workflows `.github/workflows/build-matrix.yml` et `.github/workflows/deploy-webgl.yml` utilisent `game-ci/unity-builder@v4` qui requiert une licence Unity activée pour Actions.

### Génération de la licence (Personal, gratuit)

1. Dans GitHub repo → Settings → Secrets and variables → Actions → New repository secret. Ajouter d'abord :
   - `UNITY_EMAIL` = email du compte Unity Mike
   - `UNITY_PASSWORD` = password compte Unity Mike

2. Créer un workflow temporaire pour générer le fichier `.alf` (activation license file) :
   ```bash
   mkdir -p .github/workflows
   cat > .github/workflows/activation.yml <<'EOF'
   name: Acquire activation file
   on: { workflow_dispatch: {} }
   jobs:
     activation:
       runs-on: ubuntu-latest
       steps:
         - uses: game-ci/unity-request-activation-file@v2
           id: getManualLicenseFile
           with:
             unityVersion: 6000.3.15f1
         - uses: actions/upload-artifact@v4
           with:
             name: Manual Activation File
             path: ${{ steps.getManualLicenseFile.outputs.filePath }}
   EOF
   git add .github/workflows/activation.yml && git commit -m "chore(ci): activation workflow" && git push
   ```

3. Trigger le workflow manuellement (GitHub Actions tab → Acquire activation file → Run workflow) → download l'artifact `Manual Activation File` → contient `Unity_v6000.3.15f1.alf`.

4. Upload `.alf` sur https://license.unity3d.com/manual → fill form (Personal use) → download le fichier `.ulf` retourné.

5. Open `Unity_v6000.3.15f1.ulf` dans un editor texte → copier tout le contenu.

6. GitHub repo → Settings → Secrets → New secret `UNITY_LICENSE` → coller le contenu XML du `.ulf`.

7. Delete `.github/workflows/activation.yml` (one-shot file) → `git rm .github/workflows/activation.yml && git commit && git push`.

8. Vérifier : trigger `build-matrix.yml` manuellement → must succeed.

Réf officielle : https://game.ci/docs/github/activation

### Compatibilité Unity 6.3.15f1

`unityci/editor` Docker (utilisé par `game-ci/unity-builder@v4` sur Ubuntu runners) supporte officiellement jusqu'à `6000.3.14f1` à ce jour. Si la version 6000.3.15f1 n'a pas encore d'image Docker disponible :

**Option A** : downgrade temporaire à `6000.3.14f1` (Mike via Unity Hub).
**Option B** : utiliser un self-hosted runner Mac sur la machine de Mike (Unity 6.3.15f1 déjà installée).
**Option C** : attendre que game-ci publie l'image 6000.3.15f1 (~1-2 sem typique après release).

Le workflow utilise `unityVersion: auto` (lit `ProjectSettings/ProjectVersion.txt`) — si l'image n'existe pas, le workflow échouera avec un message clair.

## 2. Steamworks SDK setup (Phase 4 desktop release)

### Prérequis

- Compte Steamworks à $100 USD (Steam Direct fee, one-time, remboursé après $1000 de revenu)
- App ID assigné par Valve après payment

### Install

1. Download Steamworks SDK depuis https://partner.steamgames.com/downloads → Steamworks SDK (latest).
2. Unzip, copier `redistributable_bin/<platform>/*` dans le projet selon platform :
   - macOS : `Plugins/x86_64/libsteam_api.dylib`
   - Windows : `Plugins/x86_64/steam_api64.dll`
   - Linux : `Plugins/x86_64/libsteam_api.so`
3. **NE PAS commit ces binaires** : ajouter dans `.gitignore` :
   ```
   /Assets/Plugins/x86_64/libsteam_api*
   /Assets/Plugins/x86_64/steam_api64*
   ```
   Mike installe localement, CI build sans, ou via secret Action (base64 du dll).
4. Install `Steamworks.NET` Unity package via OpenUPM ou git URL :
   - Vérifier compat Unity 6.3 (https://github.com/rlabrecque/Steamworks.NET) avant `Packages/manifest.json` add. Due diligence requise.
5. Pour le build standalone, créer `Build/<platform>/steam_appid.txt` contenant l'App ID (texte brut, ex `123456`). Auto-loaded par Steamworks au launch.

### Distribution

- `steamcmd` upload des builds : ajout d'un step au workflow `build-matrix.yml` post-build (sur tags `v*.*.*` uniquement). Doc : https://partner.steamgames.com/doc/sdk/uploading
- Steam Pipe accounts : créer un compte builder dédié dans Steamworks (NE PAS utiliser l'admin)

## 3. Apple Developer cert + provisioning profile (iOS Phase 4)

### Prérequis

- Apple Developer Program $99/an (https://developer.apple.com/programs/)
- Xcode 15+ installé sur la machine Mike (déjà OK pour développement local)

### Génération des certs

1. Dans Xcode → Settings → Accounts → Add Apple ID Mike → Manage Certificates → "+" → Apple Distribution.
2. Dans developer.apple.com → Certificates, Identifiers & Profiles :
   - Identifiers → "+" → App IDs → App → Bundle ID `com.crowddefense.game` → Capabilities (Game Center, In-App Purchase si voulu Phase 5)
   - Profiles → "+" → Distribution → App Store → sélectionner l'App ID → cert → download `.mobileprovision`
3. Export cert distribution :
   - Keychain Access → "Apple Distribution: Mike Chevallier" → right click → Export → format `.p12` → password fort
4. Base64 encode :
   ```bash
   base64 -i CrowdDefense_Distribution.p12 -o cert_base64.txt
   base64 -i CrowdDefense.mobileprovision -o profile_base64.txt
   ```
5. GitHub Secrets :
   - `APPLE_CERT_P12_BASE64` = contenu de `cert_base64.txt`
   - `APPLE_CERT_PASSWORD` = password du `.p12`
   - `APPLE_PROVISIONING_PROFILE_BASE64` = contenu de `profile_base64.txt`
   - `APPLE_TEAM_ID` = trouvé dans Apple Developer → Membership

### Workflow iOS (extension future)

Ajout au futur `.github/workflows/build-ios.yml` (séparé car requires macOS runner avec Xcode) :

```yaml
- name: Import Apple cert
  env:
    P12_BASE64: ${{ secrets.APPLE_CERT_P12_BASE64 }}
    P12_PASSWORD: ${{ secrets.APPLE_CERT_PASSWORD }}
  run: |
    echo "$P12_BASE64" | base64 --decode > cert.p12
    security create-keychain -p "" build.keychain
    security default-keychain -s build.keychain
    security unlock-keychain -p "" build.keychain
    security import cert.p12 -k build.keychain -P "$P12_PASSWORD" -T /usr/bin/codesign

- name: Build iOS via Unity
  uses: game-ci/unity-builder@v4
  with:
    targetPlatform: iOS
    buildMethod: CrowdDefense.Build.BuildScript.BuildIOS

- name: Archive + Upload TestFlight
  run: |
    cd Build/iOS
    xcodebuild -project Unity-iPhone.xcodeproj -scheme Unity-iPhone -configuration Release archive -archivePath CrowdDefense.xcarchive
    xcodebuild -exportArchive -archivePath CrowdDefense.xcarchive -exportOptionsPlist ExportOptions.plist -exportPath CrowdDefense.ipa
    xcrun altool --upload-app -f CrowdDefense.ipa -t ios -u $APPLE_ID -p $APPLE_APP_SPECIFIC_PASSWORD
```

## 4. Google Play upload key + Service Account (Android Phase 4)

### Génération du keystore

```bash
keytool -genkeypair \
  -alias crowddefense \
  -keyalg RSA \
  -keysize 2048 \
  -validity 10000 \
  -keystore crowddefense.keystore \
  -dname "CN=Crowd Defense, OU=Games, O=Crowd Defense, L=Paris, ST=Idf, C=FR"
```

NE PAS commit `crowddefense.keystore`. Backup local + Drive privé.

### Encode + secrets

```bash
base64 -i crowddefense.keystore -o keystore_base64.txt
```

GitHub Secrets :
- `ANDROID_KEYSTORE_BASE64` = contenu keystore_base64.txt
- `ANDROID_KEYSTORE_PASSWORD` = password keystore
- `ANDROID_KEY_ALIAS` = `crowddefense`
- `ANDROID_KEY_PASSWORD` = password key (peut être identique au keystore)

### Workflow Android (extension future)

```yaml
- name: Decode keystore
  env:
    KEYSTORE_BASE64: ${{ secrets.ANDROID_KEYSTORE_BASE64 }}
  run: echo "$KEYSTORE_BASE64" | base64 --decode > crowddefense.keystore

- name: Build Android AAB
  uses: game-ci/unity-builder@v4
  env:
    ANDROID_KEYSTORE_PASS: ${{ secrets.ANDROID_KEYSTORE_PASSWORD }}
    ANDROID_KEY_ALIAS: ${{ secrets.ANDROID_KEY_ALIAS }}
    ANDROID_KEY_PASS: ${{ secrets.ANDROID_KEY_PASSWORD }}
  with:
    targetPlatform: Android
    buildMethod: CrowdDefense.Build.BuildScript.BuildAndroidAAB
    androidKeystoreName: crowddefense.keystore
    androidKeystorePass: ${{ secrets.ANDROID_KEYSTORE_PASSWORD }}
    androidKeyaliasName: ${{ secrets.ANDROID_KEY_ALIAS }}
    androidKeyaliasPass: ${{ secrets.ANDROID_KEY_PASSWORD }}
```

### Google Play Service Account (auto-upload to internal track)

1. Google Play Console → Setup → API access → Link new service account → create in Google Cloud Console.
2. Download JSON key file → base64 encode → secret `GOOGLE_PLAY_SERVICE_ACCOUNT_JSON_BASE64`.
3. Workflow upload via `r0adkll/upload-google-play@v1` action.

## 5. Trigger conventions

| Trigger | Workflow | Effet |
|---|---|---|
| `git push tags v1.2.3` | `build-matrix.yml` | Build Mac/Win/Linux → upload artifacts |
| `git push main` (Assets/Packages/ProjectSettings touchés) | `deploy-webgl.yml` | Build WebGL → push gh-pages `/v6/` |
| Manual `workflow_dispatch` UI | both | Rebuild ad-hoc, choose targets |

### Rollback

Pour un rollback WebGL : trigger `deploy-webgl.yml` via workflow_dispatch sur un commit antérieur (re-checkout SHA précédent en local + push tag + run workflow).

Pour rollback desktop : trigger `build-matrix.yml` workflow_dispatch sur un tag antérieur, télécharger les artifacts, distribuer manuellement (ou push sur Steam via steamcmd avec build prev).

## 6. Action manuelle requise IMMÉDIATE (avant 1er run CI)

1. **Mike** : ajouter `UNITY_LICENSE` secret (cf §1).
2. **Mike** : vérifier que `gh-pages` branch existe (cf STATUS.md, déjà créé).
3. **Mike** : tester avec un push manuel `git commit --allow-empty -m "ci: test deploy" && git push origin main`.
4. **Mike** : monitorer l'Action sur GitHub UI, fix errors si Unity version pas dispo Docker (cf §1).
