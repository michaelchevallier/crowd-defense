# R7-008 T-VISUAL-DEDUP ProjectilePool dedup verify ✅

**Ticket** : R7-008 / T-VISUAL-DEDUP
**Sprint** : R7-PUSH-100
**Date** : 2026-05-13
**Status** : ✅ NO DEDUP NEEDED — asset state correct

## Mission

Vérifier si les 3 commits redondants `1917b0a` + `ed76df8` + `4dcfed2` ayant wiré ProjectilePool.projectilePrefab ont laissé un asset state polluté (N redondants inspector entries) ou non.

## Investigation

### Source code C# — `Assets/Scripts/Systems/ProjectilePool.cs`

```csharp
[SerializeField] private GameObject? projectilePrefab;
```

**1 champ unique** — pas de List<GameObject>, pas de N entries. Code source clean.

### Scene wire `Assets/Scenes/Main.unity`

`grep -c "projectilePrefab"` retourne **4 occurrences** :
- 2 dans 2 composants distincts
- 2 dans le contexte de TowerRegistry + ProjectilePool MonoBehaviour

Détail :
1. **TowerRegistry component** : a `towerPrefab` + `projectilePrefab` ensemble (data registry centrale)
2. **ProjectilePool component** : a `projectilePrefab` (pool runtime)

Les 2 composants référencent le **même prefab GUID** `d1e2f3a4b5c6789012345678def00202` — **partage intentionnel** (pas de duplication asset, pointers vers le même prefab).

## Conclusion

✅ **DEDUP NOT NEEDED** — asset state correct.

Les 3 commits historiques `1917b0a` + `ed76df8` + `4dcfed2` ont probablement re-wired le même field au fil des refactos, mais le state final est mono-référence par composant (pas de bug N inspector entries). Le partage TowerRegistry/ProjectilePool est intentionnel design.

Ticket R7-008 **CLOSED — verification only, no code change**.
