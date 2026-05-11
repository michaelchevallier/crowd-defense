# Interventions Manuelles Demandées à Mike

> Maj automatique par Opus. Si entry présente, Mike doit la traiter dès qu'il revient.
> Une fois traitée, supprimer ou marquer ✅ DONE.

---

## 🔴 EN COURS (action requise)

_Aucun item bloquant pour le moment._

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

_Dernière maj : auto par Opus. Cron V4-diff a6471edb fire chaque :13/:43._
