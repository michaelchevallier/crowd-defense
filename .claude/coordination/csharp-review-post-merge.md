# C# Quality Review — Post-Merge Wave (12 parallel agents, ~7000 LOC)

**Scope** : `git log -30` + 8 system/visual files. Read-only audit. iso-V4 parity target.

**Verdict global** : code globalement **propre et idiomatique**. `#nullable enable` partout, ZERO TODO dans le scope audité, ZERO LINQ allocation dans hot paths. Quelques frictions P1/P2 à corriger avant lock V4. Aucun blocker P0 critique.

---

## P0 — Bloquants (à corriger avant lock V4)

**(aucun)**

Le seul candidat P0 plausible — `SaveSystem.cs:74-81` qui swallow toute exception JSON — est en réalité **acceptable** : c'est un fallback save-corruption qui retombe sur un `ProgressData` neuf. Mais voir P1#3 pour amélioration.

---

## P1 — À corriger sous 24h (frictions, latent bugs)

### 1. `PerkSystem.cs:4` — `using System.Linq` importé mais **jamais utilisé**

Le fichier importe `System.Linq` (commit `8a8c313` "add System.Linq + CrowdDefense.Visual usings") mais l'inspection révèle **zéro appel LINQ** (.Where, .Select, .ToList…) dans le fichier. L'ensemble du roll/apply tourne en `for`/`foreach` manuels — c'est canon et performant. L'import est mort.

**Fix** : retirer `using System.Linq;` ligne 4. (Bonus : retirer aussi `using CrowdDefense.Visual;` si `JuiceFX` vit ailleurs — à vérifier.)

### 2. `SaveSystem.cs:264-265` — méthode `IsStackable` **morte** (zéro caller)

```csharp
private static bool IsStackable(string id) =>
    id is "range" or "fire_rate" or "pierce" or "lifesteal" or "move_speed";
```

`grep -rn IsStackable Assets/Scripts/` ne retourne **que** la définition. Dead code, hérité d'un port V5 partiel.

**Fix** : supprimer la méthode. Si la logique est nécessaire ailleurs, elle doit vivre dans `PerkDef.stackable` (qui existe déjà).

### 3. `SaveSystem.cs:74-81` — `catch` silencieux sans logging Editor

```csharp
try { _cached = JsonUtility.FromJson<ProgressData>(json) ?? new ProgressData(); }
catch { _cached = new ProgressData(); }
```

Le `catch` swallow l'exception sans **aucun** Debug.LogWarning même en Editor. Si un joueur a un PlayerPrefs corrompu, sa progression est silencieusement wipée à chaque chargement sans diagnostic possible.

**Fix** : `catch (System.Exception ex)` + bloc `#if UNITY_EDITOR Debug.LogWarning($"[SaveSystem] Load corrompu, reset: {ex.Message}"); #endif`. Bonus défensif : sauvegarder le JSON corrompu sous `cd_progression_v1_corrupted` pour debug post-mortem.

### 4. `BossSystem.cs:73-77` + `BossSystem.cs:131-133` — risque double-publish `BossDefeatedEvent`

Le flag `_defeatedPublished` est correctement géré dans la plupart des paths, mais le branchement `ResetBoss` (ligne 131) re-publish un `BossDefeatedEvent` SI `!_defeatedPublished`, ce qui est correct… SAUF que `OnLevelEnded` appelle `ResetBoss()` même quand le boss vient d'être tué via `TryPublishDefeat` (ligne 95) avant. Le flag protège, mais le code en ligne 132 lit `_defeatedPublished` ; **pourtant** ligne 137 il est reset à `false`. Si un boss meurt en même frame qu'un LevelEnded (race), tout va bien. Mais la sémantique est fragile.

**Fix** : extraire la garde dans une méthode unique `PublishDefeatOnce()`, et n'appeler que celle-ci (LateUpdate, ResetBoss, OnEnemySpawned#73). Élimine la duplication conceptuelle.

### 5. `PerkSystem.cs:190` — magic number `1.5f` (Forteresse castle HP bonus)

```csharp
if (def.forteressePerk) hero.CastleHPMaxMul *= 1.5f;
```

Tous les autres perks pull leur valeur de `PerkDef` (def.range, def.damage…), mais Forteresse hardcode `1.5f`. Drift potentiel avec `PerkDef` qui n'a pas le champ.

**Fix** : ajouter `[SerializeField] public float forteresseCastleHpMul = 1.5f;` dans `PerkDef.cs` et lire `def.forteresseCastleHpMul` ici. Aligne avec le pattern des autres perks et permet rebalancing data-driven.

### 6. `PerkSystem.cs:185` — magic number `8f` (tower aura range fallback)

```csharp
hero.TowerAuraRange = def.towerAuraRange > 0f ? def.towerAuraRange : 8f;
```

Hardcoded fallback. Devrait être `BalanceConfig.DefaultTowerAuraRange` ou un `const float` en haut du fichier.

**Fix** : `private const float DefaultTowerAuraRange = 8f;` + référence.

---

## P2 — Améliorations canon C# (qualité, pas blocker)

### 7. `== null` / `!= null` partout sur types `class` non-Unity → préférer `is null` / `is not null`

Sites où **safe** à migrer (types managed, pas Unity.Object) :

- `PerkSystem.cs:38, 90, 233, 237` (vs `PerkDef`/`PerkRegistry` — SO Unity, **NE PAS** migrer)
- `DoctrineSystem.cs:40, 65, 94` (vs `DoctrineDef` — SO Unity, NE PAS migrer)
- `BossSystem.cs:31, 39, 49, 63` (mix EventManager singleton + Unity.Object — NE PAS migrer)
- `MetaUpgradeSystem.cs:36` (`MetaUpgradeRegistry` — SO Unity, NE PAS migrer)

**Conclusion** : la quasi-totalité des `== null` ciblent des `UnityEngine.Object` (ScriptableObject, MonoBehaviour, GameObject), qui ont leur **propre operator overload** `==` pour détecter "destroyed but not GC'd". Migrer vers `is null` **changerait le comportement** (bypass Unity null-check) et est **dangereux**. **Garder `== null` ici est correct.**

Seuls candidats légitimes :
- `SaveSystem.cs:66, 87, 169` (`_cached`, `_cachedRun` = POCO, pas Unity.Object) → peut migrer `is null` proprement.

### 8. `MetaUpgradeSystem.cs:11-23` — `RunBonuses` : champs `public` mutables → préférer `record` ou structure immuable

C'est un DTO de calculs purs. Soit garder en `class` avec `internal set`, soit en C# 9 `record` immuable + `with` expression. Actuellement chaque caller peut muter `ActiveBonuses.heroDamageMul` from-the-outside.

**Fix optionnel** : convertir en `public record class RunBonuses(float CastleHPMul = 1f, …)`. Pas bloquant V4 mais lock canonical pattern pour suite.

### 9. `MetaUpgradeSystem.cs:48-69` — `switch` sur `string` magic keys → string-typed enum

```csharp
case "castleHPMul":         b.castleHPMul         *= 1f + v; break;
case "heroDamageMul":       b.heroDamageMul       *= 1f + v; break;
// …10 cases…
```

Et même pattern dans `DoctrineSystem.cs:73-89`. Magic strings = drift assuré entre ScriptableObject inspector et code. Si quelqu'un tape "CastleHpMul" au lieu de "castleHPMul" dans l'inspector, fail silencieux (aucun warning Editor dans MetaUpgradeSystem ; il y a un warning dans DoctrineSystem ligne 86 — pattern cohérent à appliquer aux deux).

**Fix** : enum `MetaUpgradeEffectKey { CastleHPMul, HeroDamageMul, … }` sérialisée via `[SerializeField]` sur `MetaUpgradeEffect`. Élimine 10 cases sans risque typo.

### 10. `DoctrineSystem.cs:92-98` — `GetModifierValue` itère linéaire à chaque call → cache si appelé en hot path

```csharp
public float GetModifierValue(string key)
{
    if (ActiveDoctrine == null) return 1f;
    foreach (var mod in ActiveDoctrine.modifiers)
        if (mod.key == key) return mod.value;
    return 1f;
}
```

OK si appelé 1× au run start. Si appelé par Tower.Update à 60Hz × 100 tours = 6000 string comparisons/sec. Vérifier le callsite avant d'optimiser (Dictionnary cache si hot).

### 11. `VfxPool.cs:175` — `StartCoroutine(AutoReleaseRoutine(...))` par appel SpawnX → allocations IEnumerator

Chaque spawn alloue un coroutine state machine + `WaitForSeconds` (1 alloc heap chacun). À 30 spawns/sec en hot fight = 60 GC pressure / sec. Le port canon devrait soit (a) gérer un `Update` central qui ticke un buffer de `(ParticleSystem, releaseTime)`, soit (b) utiliser `ParticleSystemStopAction.Disable` + `OnParticleSystemStopped` callback (Unity natif, zero alloc).

**Fix** : ligne 210 il y a déjà `main.stopAction = ParticleSystemStopAction.Disable` — bien ! Mais le `Disable` désactive le GameObject sans le release au pool. Solution canonique : utiliser un script `VfxAutoRelease : MonoBehaviour, IPoolable` qui hook `OnParticleSystemStopped`. Pas bloquer V4, mais ticket follow-up.

### 12. `VfxPool.cs:369-376` — magic `Material` strings hors `const`

```csharp
mat.SetFloat("_Surface", 1f);
mat.SetFloat("_Blend", 3f);
mat.SetInt("_ZWrite", 0);
mat.SetInt("_SrcBlend", 1);
mat.SetInt("_DstBlend", 1);
```

URP shader property names en strings. Préférer `Shader.PropertyToID("_Surface")` cachés en `static readonly int`.

### 13. `RunContext.cs:21` — `UnityEngine.Object.DontDestroyOnLoad(gameObject)` qualifié inutilement

`DontDestroyOnLoad` est dispo directement (héritage MonoBehaviour). Qualifier `UnityEngine.Object.` est verbeux et inhabituel. Probablement résidu d'un fix CS0104 raté. Simplifier en `DontDestroyOnLoad(gameObject);`.

---

## Synthèse par check-list

| Check | Status |
|---|---|
| 1. `#nullable enable` compliance | OK partout (8/8 fichiers ont `#nullable enable` ligne 1) |
| 2. Pattern matching (`is null`, switch expr) | OK — `PerkSystem.cs:244-250` et `VfxPool.cs:77-82` utilisent switch expressions. `is null` n'est PAS recommandé sur Unity.Object (P2#7). |
| 3. LINQ allocations hot paths | OK — zéro LINQ dans les 8 fichiers audités (en dehors de `PerkRegistry.GetSchoolPerks` qui n'est appelé qu'au roll, froid). |
| 4. Defensive antipatterns (retry forever, swallow) | 1 catch silencieux (P1#3). Pas de retry loops. |
| 5. MonoBehaviour vs static | `MetaUpgradeSystem` et `RunContext` pourraient être pure statics, mais MonoSingleton offre lifecycle/event hooks — choix défendable. |
| 6. Magic numbers | 2 cas (P1#5, P1#6) + URP shader strings (P2#12). |
| 7. TODO comments dans scope | **ZERO** dans les 8 fichiers audités. (5 TODOs ailleurs dans le repo — voir ci-dessous.) |

---

## TODOs hors scope (mais visible dans le grep)

À tracker dans le backlog pour ne pas oublier :

- `/Users/mike/Work/crowd-defense/Assets/Scripts/UI/ShopController.cs:10` — WorldMapController hookup post-victory
- `/Users/mike/Work/crowd-defense/Assets/Scripts/Visual/WeatherController.cs:120-121` — PlayAmbientAudio / StopAmbientAudio (Phase 4)
- `/Users/mike/Work/crowd-defense/Assets/Scripts/Systems/Achievements.cs:65, 94` — Toast UI + predicate evaluation (Phase 5.B)

---

## Recommandation

**Ship V4 maintenant** : aucun P0. Les P1 sont 2h de cleanup max. Les P2 sont du polish post-V4.

**Priorité immédiate suggérée** : un seul ticket Sonnet `quality-maintainer`, ~1h, qui adresse en bulk :
1. `PerkSystem.cs` retire `using System.Linq` mort
2. `SaveSystem.cs` retire `IsStackable` morte + ajoute warning Editor sur catch
3. `PerkSystem.cs` extrait magic numbers (1.5f, 8f) en const/PerkDef field
4. `BossSystem.cs` consolide `_defeatedPublished` dans `PublishDefeatOnce()`

Files critiques : tous lus, aucun nécessite reécriture structurelle.
