# Mixamo Auto-Downloader — Crowd Defense ASSET-GEN

**Status** : prep stub. Login Adobe non automatisé (SSO complexe), Mike doit faire le login manuel 1× puis fournir le cookie session.

## Goal

Automatiser le téléchargement d'animations humanoid Mixamo (`walk`, `run`, `attack`, `die`, etc.) pour les enemies Quaternius/Kenney qui n'ont pas de clips embedded dans leurs GLTF.

**Target output** : `Assets/Animations/Mixamo/{character}_{anim}.fbx`, importables Unity via FBX importer standard. Phase 4 : retarget sur Avatar humanoid Unity + Animator Controllers existants (`Assets/Animators/`).

## Sources réseau

Pas d'API publique officielle Mixamo. Approches connues :

| Approche | Pro | Con |
|---|---|---|
| Adobe API privée + cookie session reverse-engineered | rapide, scriptable | Adobe peut bloquer/changer schema |
| Playwright headless + login auto | robuste UI | Adobe SSO + captcha possibles |
| Manual download via UI Adobe | aucun risk | scale 0, fastidieux |
| Fork existant : https://github.com/loveletter/mixamo-auto-downloader | testé | maintenance variable, vérifier last commit |

**Décision pour Phase 3** : utiliser fork `loveletter/mixamo-auto-downloader` (cf STATUS.md backlog T3). Stub `download_anims.py` ci-dessous sketche l'invocation.

## Setup manuel Mike (à faire 1× avant que le script tourne)

1. Créer un compte Adobe gratuit si pas déjà : https://account.adobe.com
2. Login sur https://www.mixamo.com — accepter conditions Adobe Cloud
3. Capturer le cookie session :
   - Ouvrir Chrome DevTools → Application → Cookies → `www.mixamo.com`
   - Copier la valeur de `XSRF-TOKEN` + `mixamo2-session-id` (ou ce que le fork demande)
4. Sauvegarder dans `~/.mixamo-session.json` (gitignored) :
   ```json
   {
     "XSRF-TOKEN": "...",
     "mixamo2-session-id": "..."
   }
   ```
5. Vérifier : `python3 tools/mixamo/download_anims.py --check-auth` (à implémenter)

## Animations cibles Phase 3

Pour chaque enemy humanoid (~17 mobs : goblin, knight, pirate, soldier, wizard, zombie, mob_alpaking, mob_armabee, mob_cactoro, mob_dino, mob_espace_astronaut, mob_frog, mob_ninja, mob_orc, mob_squidle, mob_warlock, mob_cyberpunk_character) :

| Animation Unity state | Mixamo source | Trigger gameplay |
|---|---|---|
| `Idle` | "Idle" (basic) | spawn, between actions |
| `Walk` | "Walking" (basic) | path follow speed < threshold |
| `Run` | "Running" (basic) | path follow speed > threshold |
| `Attack` | "Attacking" (any combat) | melee/ranged tick |
| `Die` | "Dying Backwards" | HP <= 0 |

Total : ~85 anim files = ~17 chars × 5 anims = potentiellement large. Mixamo permet 100 free downloads per session, devrait passer.

## Limitations connues

- **Adobe SSO** : si Adobe force MFA → script doit pause et demander code à Mike (out-of-scope POC).
- **Mixamo retarget engine** : Mixamo permet de download anim "pour un T-pose model spécifique" → uploader chaque enemy mesh 1× pour binding. Stub ne couvre pas l'upload, juste le download d'anim "with skin" basique.
- **Phase 4 retargeting Unity** : conversion vers Avatar humanoid Unity sera fait via `ModelImporter.animationType = ModelImporterAnimationType.Human` en Editor script séparé, hors scope ASSET-GEN axis.

## Status livré ASSET-GEN

- [x] `tools/mixamo/README.md` (ce fichier)
- [x] `tools/mixamo/download_anims.py` stub avec interface CLI esquissée
- [ ] Login Adobe (manuel Mike)
- [ ] Implémentation fetch réelle (Phase 3 post-merge ou Phase 4)
- [ ] Retarget Unity Avatar Humanoid (Phase 4)

Si Adobe SSO trop complexe en pratique → fallback : Mike download manuellement 5-10 anims les plus importantes via UI Mixamo, drop dans `Assets/Animations/Mixamo/`, accepter scope réduit Phase 3.
