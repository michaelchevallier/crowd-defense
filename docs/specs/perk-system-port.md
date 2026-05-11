# Spec — Port du PerkSystem V5 vers Unity C#

## 1. Contexte

`Hero.ApplyRunContext(perkIds, level, xp)` (Hero.cs:250) stocke déjà la liste
d'IDs de perks mais **ne les applique pas** : le commentaire ligne 258 dit
explicitement *"Perk application deferred to Phase 4 (PerkRegistry SO)"*. Tous
les multiplicateurs (`FireRateMul`, `DamageMul`, `Fireball`, `Ricochet`, etc.)
restent donc à leur valeur de reset, et les perks n'ont aucun effet en jeu.

Source canonique V5 :
- `/Users/mike/Work/milan project/src-v3/data/perks.js` — 17 perks standard
  (`PERKS` array) + 6 set bonuses par tag (`SET_BONUSES`) + `rollPerkChoices()`
  + `getPerkById()`.
- `/Users/mike/Work/milan project/src-v3/entities/Hero.js:235-298` —
  `applyPerk(perk)` : la logique d'agrégation field-by-field (43 effets).
- `/Users/mike/Work/milan project/src-v3/data/schools.js:36-103` — 6
  `SCHOOL_PERKS` exclusifs (combustion / pyromancie / glaciation / cristal_glace
  / forteresse_perk / murs_pierre).
- `/Users/mike/Work/milan project/src-v3/main.js:1730-1773` — UI level-up flow
  (`showNextPerkChoice` : roll 3 choix, joueur clique, `applyPerk`).

Objectif : reproduire l'écosystème data-driven + apply + UI overlay en Unity,
DRY (pas de switch géant : un `PerkDef` SO décrit chaque modifier, le
`PerkSystem` lit les champs et applique sur `Hero`).

---

## 2. Fichiers impactés

### Nouveaux fichiers (à créer)

- `/Users/mike/Work/crowd-defense/Assets/Scripts/Data/PerkDef.cs` — SO
  decrivant 1 perk (mirror du JSON V5).
- `/Users/mike/Work/crowd-defense/Assets/Scripts/Data/PerkRegistry.cs` — SO
  collection (mirror du pattern `AchievementRegistry.cs`).
- `/Users/mike/Work/crowd-defense/Assets/Scripts/Data/PerkSetBonusDef.cs` — SO
  decrivant 1 set bonus (tag → threshold → effets agrégés).
- `/Users/mike/Work/crowd-defense/Assets/Scripts/Systems/PerkSystem.cs` —
  Singleton MonoBehaviour, port direct de `applyPerk()` + roll + set bonus
  resolution.
- `/Users/mike/Work/crowd-defense/Assets/Scripts/UI/PerkChoiceOverlay.cs` —
  Overlay 3-cards level-up, écoute `Hero.OnLevelUp`.
- `/Users/mike/Work/crowd-defense/Assets/Scripts/UI/HudPerkBadges.cs` — barre
  de badges en HUD montrant perks actifs + progression set bonus (3/3 par tag).
- `/Users/mike/Work/crowd-defense/Assets/ScriptableObjects/Perks/*.asset` — 17
  PerkDef + 6 PerkSetBonusDef + 6 SchoolPerkDef + 1 `PerkRegistry.asset`
  (créés via Editor script ou MCP bulk).
- `/Users/mike/Work/crowd-defense/Assets/Resources/PerkRegistry.asset` — copie
  ou lien (chargé via `Resources.Load`, mirror du pattern `AssetRegistry`).
- `/Users/mike/Work/crowd-defense/Assets/Editor/BuildPerkAssets.cs` — MenuItem
  one-shot pour générer les 23 assets depuis le source canonique (mirror du
  pattern `BuildAnimatorControllers`).

### Fichiers modifiés

- `Assets/Scripts/Entities/Hero.cs` lignes 250-262 — `ApplyRunContext` délègue
  à `PerkSystem.Instance.ApplyPerkList(this, perkIds)` au lieu du stub.
- `Assets/Scripts/Entities/Hero.cs` lignes 671-712 — `ResetPerkStats` doit
  exposer un `internal` setter ou rester appelé en interne (option : marquer
  les Mul properties `{ get; internal set; }` pour que PerkSystem puisse
  écrire dessus depuis le même namespace).
- `Assets/Scripts/Entities/Hero.cs` lignes 244-262 — supprimer le TODO ligne
  258, garder le `_perks.Add` (PerkSystem alimente via setter).
- `Assets/Scripts/Systems/LevelRunner.cs` ligne ~90-110 (zone `SpawnCastle`,
  juste avant ou après) — appeler `_hero.ApplyRunContext(rs.heroPerks, ...)`
  une fois `RunState` chargé (cf. point 5 wire-up).
- `Assets/Scripts/Systems/LevelRunner.cs` lignes 80-115 — branche
  `CastleHPMaxMul` : actuellement `ResolveCastleHP()` retourne `currentLevel.CastleHP`
  brut, il faut multiplier par `_hero.CastleHPMaxMul` une fois perks appliqués.
- `Assets/Scripts/Systems/SaveSystem.cs` ligne ~10 — ajouter classe
  `RunState` (heroPerks, heroLevel, heroXP, schoolId) sérialisable dans
  `ProgressData` ou en clé séparée `cd_runstate_v1`.
- `Assets/Scripts/Systems/Economy.cs` — quand un enemy meurt et qu'on add gold
  via `Enemy.OnDeath`, multiplier par `hero.CoinGainMul`. Probablement déjà
  TODO ailleurs, à vérifier dans `Enemy.cs`/`Castle.cs` (hors scope si
  centralisé via WaveManager).
- `Assets/Scripts/Systems/PlacementController.cs` — appliquer
  `hero.TowerCostMul` au cost retourné par `TowerType.Cost` + gérer
  `hero.FirstTowerFree`/`FirstTowerFreeUsed`.
- `Assets/Scripts/UI/HudController.cs` — créer ou exposer un slot pour
  `HudPerkBadges` (overlay top-right).

---

## 3. Comportement attendu

1. **Au lancement d'un run**, `LevelRunner.Start` lit `SaveSystem.GetRunState()`
   et appelle `_hero.ApplyRunContext(rs.heroPerks, rs.heroLevel, rs.heroXP)`.
2. **`ApplyRunContext`** appelle `ResetPerkStats()` puis délègue à
   `PerkSystem.Instance.ApplyPerkList(hero, perkIds)`.
3. **`PerkSystem.ApplyPerk(hero, perkDef)`** mute les Mul fields du Hero selon
   les champs renseignés sur le `PerkDef` (range → `RangeMul *= 1 + range`,
   fireRate → `FireRateMul *= 1 - fireRate`, fireball flag → `Fireball = true`
   + radius/dmgMul). C'est un mapping data-driven sans switch sur l'ID.
4. **Downsides** (`_downRange`, `_downDamage`, `_downFireRate`,
   `_downCoinReward`) sont appliqués au même endroit que les bonus.
5. **Set bonuses** : chaque PerkDef a un `tag` (foudre/sang/pierre/feu/vide/or).
   `PerkSystem` compte les occurrences par tag dans la liste appliquée ; si
   `count >= threshold (3)`, applique le `PerkSetBonusDef` correspondant et
   broadcast un `OnSetBonusActivated` event.
6. **School auto-set-bonus** : si `rs.schoolId` est set, force le set bonus
   du tag de l'école même si pas 3 perks (port `applySchoolSetBonus` V5).
7. **Level-up flow** : `Hero.OnLevelUp` event → `PerkChoiceOverlay` (Canvas
   activé), pause game (`Time.timeScale = 0`), génère 3 cards via
   `PerkSystem.RollChoices(hero, count=3, levelUpsLeft, schoolPerks)`. Joueur
   clique → `PerkSystem.ApplyPerk(hero, picked)` → save `rs.heroPerks` →
   resume game.
8. **HUD badges** : `HudPerkBadges` écoute `OnPerkApplied` et
   `OnSetBonusActivated`, affiche une row d'icônes (rounded square 32px) +
   pour chaque tag actif la progression X/3.
9. **Roll logic** : le pool = `PerkRegistry.All` (filtré stackable + maxStacks)
   + `schoolPerks` (`PerkRegistry.GetSchoolPerks(schoolId)`). Si
   `levelUpsLeft <= 1` et aucun perk `transform=true` n'a encore été pris, on
   garantit qu'au moins 1 card transform apparaît (port `lastChance` V5).
10. **Persistence** : à chaque pick, `RunState.heroPerks` (List<string>) est
    sauvegardé via `SaveSystem.SetRunState` ; au reload du même niveau le
    hero retrouve ses perks.
11. **FirstTowerFree** : `PlacementController` consulte `hero.FirstTowerFree`
    et `hero.FirstTowerFreeUsed`. Si free + !used, cost = 0 et set used=true.
12. **CoinGainMul** : actuellement le multiplicateur existe sur Hero mais
    aucun callsite ; appliquer dans `Economy.AddGoldFromKill(reward, hero)` ou
    dans `Enemy.OnDeath` (où le reward est appliqué).
13. **Audio** : à chaque `ApplyPerk` jouer `"perk_pick"` (clé canonique) +
    `JuiceFX.Flash` doré. Au set bonus, son spécial `"set_bonus"`.
14. **Toast** : `OnSetBonusActivated` → `HudController` montre toast "Set
    bonus actif : Tempête (+15% crit)".

---

## 4. Pseudo-code des fixes

### 4.1 PerkDef SO (~80 lignes)

```csharp
// Assets/Scripts/Data/PerkDef.cs
namespace CrowdDefense.Data
{
    public enum PerkCategory { Offensive, Economy, Mobility, Transform, Support }
    public enum PerkTag      { None, Foudre, Sang, Pierre, Feu, Vide, Or }

    [CreateAssetMenu(menuName = "CrowdDefense/PerkDef", fileName = "PerkDef")]
    public class PerkDef : ScriptableObject
    {
        // Identity
        [SerializeField] public string id = "";
        [SerializeField] public string nameKey = "";       // L10n key
        [SerializeField] public string descKey = "";
        [SerializeField] public string iconEmoji = "";     // V5 emoji fallback
        [SerializeField] public Sprite? icon;
        [SerializeField] public PerkCategory category;
        [SerializeField] public PerkTag tag;
        [SerializeField] public bool   stackable;
        [SerializeField] public int    maxStacks = 0;       // 0 = no cap
        [SerializeField] public string school = "";          // "" = standard, sinon school id
        [SerializeField] public bool   transform;            // V5 transform=true flag

        // Bonuses (any can be 0 / left default)
        [SerializeField] public float range;
        [SerializeField] public float fireRate;
        [SerializeField] public float damage;
        [SerializeField] public float moveSpeed;
        [SerializeField] public int   moveAttackPierceBonus;
        [SerializeField] public float coinGain;
        [SerializeField] public float critChance;
        [SerializeField] public float critMul;
        [SerializeField] public int   critStaggerMs;
        [SerializeField] public int   multiShot;
        [SerializeField] public int   pierceCount;
        [SerializeField] public int   lifesteal;
        [SerializeField] public float waveRegen;

        // Transform flags
        [SerializeField] public bool  fireball;
        [SerializeField] public float fireballRadius = 2f;
        [SerializeField] public float fireballDmgMul = 0.8f;
        [SerializeField] public bool  ricochet;
        [SerializeField] public int   ricochetBounces = 3;
        [SerializeField] public float ricochetDecay = 0.85f;
        [SerializeField] public bool  lightning;
        [SerializeField] public int   lightningTargets = 2;
        [SerializeField] public float lightningDmgMul = 0.7f;
        [SerializeField] public bool  pierceExplode;
        [SerializeField] public float pierceExplodeRadius = 2f;
        [SerializeField] public float pierceExplodeDmgMul = 1f;

        // Support
        [SerializeField] public float towerCostMul = 1f;
        [SerializeField] public bool  firstTowerFree;
        [SerializeField] public float towerFireRateAura = 1f;
        [SerializeField] public float towerAuraRange;

        // School perks
        [SerializeField] public bool combustion;
        [SerializeField] public bool pyromancie;
        [SerializeField] public bool glaciation;
        [SerializeField] public bool cristalGlace;
        [SerializeField] public bool forteressePerk;
        [SerializeField] public bool mursPierre;

        // Downsides (negative deltas, V5 _down*)
        [SerializeField] public float downRange;
        [SerializeField] public float downDamage;
        [SerializeField] public float downFireRate;
        [SerializeField] public float downCoinReward;
    }
}
```

### 4.2 PerkSetBonusDef SO (~25 lignes)

```csharp
// Assets/Scripts/Data/PerkSetBonusDef.cs
[CreateAssetMenu(menuName = "CrowdDefense/PerkSetBonusDef", fileName = "PerkSetBonusDef")]
public class PerkSetBonusDef : ScriptableObject
{
    [SerializeField] public PerkTag tag;
    [SerializeField] public int     threshold = 3;
    [SerializeField] public string  nameKey = "";    // "Tempête", "Carmin"...
    [SerializeField] public string  descKey = "";
    [SerializeField] public float   addCritChance;
    [SerializeField] public float   addLifesteal;
    [SerializeField] public float   castleHPMaxMul = 1f;
    [SerializeField] public float   fireRateMul = 1f;
    [SerializeField] public int     aoeOnNthProjectile;
    [SerializeField] public float   coinGainMul = 1f;
}
```

### 4.3 PerkRegistry SO (~50 lignes, mirror AchievementRegistry)

```csharp
// Assets/Scripts/Data/PerkRegistry.cs
[CreateAssetMenu(menuName = "CrowdDefense/PerkRegistry", fileName = "PerkRegistry")]
public class PerkRegistry : ScriptableObject
{
    [SerializeField] private PerkDef[] standard = System.Array.Empty<PerkDef>();
    [SerializeField] private PerkDef[] schoolPerks = System.Array.Empty<PerkDef>();
    [SerializeField] private PerkSetBonusDef[] setBonuses = System.Array.Empty<PerkSetBonusDef>();

    private Dictionary<string, PerkDef>? _byId;
    private Dictionary<PerkTag, PerkSetBonusDef>? _bonusByTag;

    public PerkDef[] Standard => standard;
    public PerkDef[] AllSchool => schoolPerks;

    public PerkDef? Get(string id)
    {
        if (_byId == null) BuildCache();
        _byId!.TryGetValue(id, out var def);
        return def;
    }
    public PerkSetBonusDef? GetBonus(PerkTag t)
    {
        if (_bonusByTag == null) BuildCache();
        _bonusByTag!.TryGetValue(t, out var b);
        return b;
    }
    public IEnumerable<PerkDef> GetSchoolPerks(string schoolId) =>
        schoolPerks.Where(p => p.school == schoolId);

    private void BuildCache()
    {
        _byId = new();
        foreach (var p in standard) if (p && !string.IsNullOrEmpty(p.id)) _byId[p.id] = p;
        foreach (var p in schoolPerks) if (p && !string.IsNullOrEmpty(p.id)) _byId[p.id] = p;
        _bonusByTag = setBonuses.Where(b => b != null).ToDictionary(b => b.tag);
    }
    private void OnEnable() { _byId = null; _bonusByTag = null; }

    public static PerkRegistry? Load() => Resources.Load<PerkRegistry>("PerkRegistry");
}
```

### 4.4 PerkSystem singleton (~120 lignes)

```csharp
// Assets/Scripts/Systems/PerkSystem.cs
public class PerkSystem : MonoSingleton<PerkSystem>
{
    private PerkRegistry? _registry;
    public event Action<Hero, PerkDef>? OnPerkApplied;
    public event Action<Hero, PerkSetBonusDef>? OnSetBonusActivated;

    protected override void OnAwakeSingleton()
    {
        _registry = PerkRegistry.Load();
#if UNITY_EDITOR
        if (_registry == null) Debug.LogWarning("[PerkSystem] Resources/PerkRegistry.asset manquant");
#endif
    }

    public void ApplyPerkList(Hero hero, IReadOnlyList<string> perkIds)
    {
        if (_registry == null) return;
        var tagCounts = new Dictionary<PerkTag, int>();
        foreach (var id in perkIds)
        {
            var def = _registry.Get(id);
            if (def == null) continue;
            ApplyOne(hero, def, tagCounts);
        }
    }

    public void ApplyPerk(Hero hero, PerkDef def)
    {
        var tagCounts = BuildTagCounts(hero);
        ApplyOne(hero, def, tagCounts);
    }

    private void ApplyOne(Hero hero, PerkDef def, Dictionary<PerkTag, int> tagCounts)
    {
        hero.AddPerkId(def.id);  // expose internal setter or method
        // Mul / additive deltas (data-driven, pas de switch)
        if (def.range != 0f)       hero.RangeMul    *= 1f + def.range;
        if (def.fireRate != 0f)    hero.FireRateMul *= 1f - def.fireRate;
        if (def.damage != 0f)      hero.DamageMul   *= 1f + def.damage;
        if (def.moveSpeed != 0f)   hero.MoveSpeedMul*= 1f + def.moveSpeed;
        if (def.coinGain != 0f)    hero.CoinGainMul *= 1f + def.coinGain;
        hero.CritChance         += def.critChance;
        if (def.critMul > 0f)      hero.CritMul = def.critMul;
        if (def.critStaggerMs > 0) hero.CritStaggerMs = def.critStaggerMs;
        hero.MultiShot          += def.multiShot;
        hero.PierceCount        += def.pierceCount;
        hero.Lifesteal          += def.lifesteal;
        hero.WaveRegen          += def.waveRegen;
        hero.MoveAttackPierceBonus += def.moveAttackPierceBonus;

        if (def.fireball)     { hero.Fireball = true; hero.FireballRadius = def.fireballRadius; hero.FireballDmgMul = def.fireballDmgMul; }
        if (def.ricochet)     { hero.Ricochet = true; hero.RicochetBounces = def.ricochetBounces; hero.RicochetDecay = def.ricochetDecay; }
        if (def.lightning)    { hero.Lightning = true; hero.LightningTargets = def.lightningTargets; hero.LightningDmgMul = def.lightningDmgMul; }
        if (def.pierceExplode){ hero.PierceExplode = true; hero.PierceExplodeRadius = def.pierceExplodeRadius; hero.PierceExplodeDmgMul = def.pierceExplodeDmgMul; }
        if (def.towerCostMul != 1f) hero.TowerCostMul *= def.towerCostMul;
        if (def.firstTowerFree) { hero.FirstTowerFree = true; hero.FirstTowerFreeUsed = false; }
        if (def.towerFireRateAura != 1f) { hero.TowerFireRateAuraMul = def.towerFireRateAura; hero.TowerAuraRange = def.towerAuraRange > 0 ? def.towerAuraRange : 8f; }
        if (def.combustion)     hero.Combustion = true;
        if (def.pyromancie)     hero.Pyromancie = true;
        if (def.glaciation)     hero.Glaciation = true;
        if (def.forteressePerk) hero.CastleHPMaxMul *= 1.5f;
        // Downsides
        if (def.downRange != 0f)      hero.RangeMul    *= 1f + def.downRange;
        if (def.downDamage != 0f)     hero.DamageMul   *= 1f + def.downDamage;
        if (def.downFireRate != 0f)   hero.FireRateMul *= 1f - def.downFireRate;
        if (def.downCoinReward != 0f) hero.CoinRewardMul *= 1f + def.downCoinReward;

        // Set bonus tracking
        if (def.tag != PerkTag.None)
        {
            tagCounts.TryGetValue(def.tag, out int c);
            tagCounts[def.tag] = ++c;
            var bonus = _registry!.GetBonus(def.tag);
            if (bonus != null && c == bonus.threshold)
            {
                ApplySetBonus(hero, bonus);
                OnSetBonusActivated?.Invoke(hero, bonus);
            }
        }
        OnPerkApplied?.Invoke(hero, def);
    }

    private void ApplySetBonus(Hero hero, PerkSetBonusDef b)
    {
        hero.CritChance      += b.addCritChance;
        hero.Lifesteal       += b.addLifesteal;
        hero.CastleHPMaxMul  *= b.castleHPMaxMul;
        hero.FireRateMul     *= b.fireRateMul;
        hero.CoinGainMul     *= b.coinGainMul;
        // aoeOnNthProjectile : à exposer sur Hero (champ manquant actuellement)
    }

    public List<PerkDef> RollChoices(Hero hero, int count, int levelUpsLeft, string schoolId)
    {
        // Port direct rollPerkChoices() V5 lines 210-244
        // 1. Pool = standard + schoolPerks(schoolId)
        // 2. Filter stackable / maxStacks
        // 3. lastChance transform guarantee
        // 4. Fisher-Yates shuffle, slice(0, count)
        ...
    }

    public bool CanApply(Hero hero, PerkDef def)
    {
        if (!def.stackable && hero.Perks.Contains(def.id)) return false;
        if (def.maxStacks > 0)
        {
            int stacks = 0;
            foreach (var id in hero.Perks) if (id == def.id) stacks++;
            return stacks < def.maxStacks;
        }
        return true;
    }
}
```

### 4.5 Hero.cs patches (Hero.cs:244-262, ~10 lignes)

```csharp
public void ApplyRunContext(IReadOnlyList<string> perkIds, int level = 1, int xp = 0)
{
    ResetPerkStats();
    Level = level; Xp = xp;
    if (cfg != null) XpToNext = cfg.XpToNext(level);

    PerkSystem.Instance?.ApplyPerkList(this, perkIds);
}

// + expose internal helper AddPerkId used by PerkSystem
internal void AddPerkId(string id) => _perks.Add(id);

// Convert all Mul properties to `{ get; internal set; }` so PerkSystem can mutate.
```

### 4.6 LevelRunner integration (LevelRunner.cs ~ligne 90)

```csharp
private void Start()
{
    if (WaveManager.Instance != null)
        WaveManager.Instance.OnAllWavesCompleted += OnVictory;

    SpawnCastle();
    SpawnHero();   // nouveau — ou existant ailleurs
    ApplyRunStateToHero();
}

private void ApplyRunStateToHero()
{
    if (_hero == null) return;
    var rs = SaveSystem.GetRunState();
    _hero.ApplyRunContext(rs?.heroPerks ?? new List<string>(), rs?.heroLevel ?? 1, rs?.heroXP ?? 0);

    // Castle HP rescale après perks (forteresse_perk, etc.)
    if (PrimaryCastle != null && _hero.CastleHPMaxMul > 1f)
    {
        int bonus = Mathf.RoundToInt(PrimaryCastle.HPMax * (_hero.CastleHPMaxMul - 1f));
        PrimaryCastle.GrantBonusHP(bonus);  // helper à ajouter sur Castle
    }
}
```

### 4.7 PerkChoiceOverlay (~60 lignes UI)

```csharp
public class PerkChoiceOverlay : MonoBehaviour
{
    [SerializeField] private Canvas canvas;
    [SerializeField] private RectTransform cardContainer;
    [SerializeField] private GameObject cardPrefab;

    private int _queue;
    private Hero? _hero;

    private void Start()
    {
        _hero = LevelRunner.Instance?.Hero;
        if (_hero != null) _hero.OnLevelUp += OnLevelUp;
        canvas.enabled = false;
    }

    private void OnLevelUp(int lvl, int xp, int next)
    {
        _queue++;
        if (!canvas.enabled) ShowNext();
    }

    private void ShowNext()
    {
        if (_queue <= 0 || _hero == null) return;
        var rs = SaveSystem.GetRunState();
        var choices = PerkSystem.Instance!.RollChoices(_hero, 3, _hero.MaxLevel - _hero.Level, rs?.schoolId ?? "");
        if (choices.Count == 0) { _queue = 0; return; }

        foreach (Transform c in cardContainer) Destroy(c.gameObject);
        foreach (var def in choices)
        {
            var card = Instantiate(cardPrefab, cardContainer);
            card.GetComponent<PerkCardView>().Bind(def, _hero, OnPicked);
        }
        Time.timeScale = 0f;
        canvas.enabled = true;
    }

    private void OnPicked(PerkDef def)
    {
        PerkSystem.Instance!.ApplyPerk(_hero!, def);
        SaveSystem.AppendRunPerk(def.id);
        AudioController.Instance?.Play("perk_pick", 0.9f);
        canvas.enabled = false;
        _queue--;
        Time.timeScale = 1f;
        if (_queue > 0) StartCoroutine(NextAfterDelay());
    }
}
```

### 4.8 BuildPerkAssets editor tool

```csharp
[MenuItem("CrowdDefense/Build/Generate Perk Assets")]
public static void Generate()
{
    // 17 entrées hardcodées dérivées de V5 perks.js — chaque PerkDef.asset
    // créé via AssetDatabase.CreateAsset puis injecté dans PerkRegistry.standard.
    // Idem 6 SCHOOL_PERKS → PerkRegistry.schoolPerks et 6 SET_BONUSES → setBonuses.
    // Copie PerkRegistry.asset dans Resources/.
}
```

---

## 5. Critères de succès

- [ ] `Assets/Scripts/Data/PerkDef.cs` + `PerkRegistry.cs` + `PerkSetBonusDef.cs`
      compilent sans erreur (`mcp__unity-mcp__read_console`).
- [ ] `Assets/Scripts/Systems/PerkSystem.cs` compile, `PerkSystem.Instance`
      accessible depuis Hero + LevelRunner.
- [ ] `Assets/Editor/BuildPerkAssets.cs` MenuItem présent et runnable une fois.
- [ ] Après run : 23 assets (17 standard + 6 school) PerkDef + 6 PerkSetBonusDef +
      1 PerkRegistry.asset présents dans `Assets/ScriptableObjects/Perks/` et
      `Assets/Resources/PerkRegistry.asset`.
- [ ] `PerkRegistry.Get("damage").damage == 0.50f`,
      `PerkRegistry.Get("damage").downRange == -0.15f` (V5 mapping fidèle).
- [ ] Play mode : hero spawn → `_hero.Perks.Count == 0` au tick 0.
- [ ] Trigger debug `_hero.ApplyRunContext(new[]{"damage","range"})` → log
      `DamageMul ≈ 1.5 * 0.85 = 1.275` et `RangeMul ≈ 1.35` (cumul down × bonus).
- [ ] Trigger `_hero.ApplyRunContext(new[]{"range","fire_rate","crit"})` (3 ×
      tag foudre) → `OnSetBonusActivated` fire et `CritChance >= 0.45` (0.30 +
      0.15 set bonus).
- [ ] Level-up → overlay s'affiche, `Time.timeScale == 0`, 3 cards cliquables.
- [ ] Clic sur card → overlay disparaît, perk dans `_hero.Perks`, timescale = 1.
- [ ] Sauvegarde `RunState.heroPerks` persist entre redémarrages level.
- [ ] HUD badges montrent icônes des perks actifs + "1/3 ⚡" pour tag progression.
- [ ] Aucune référence `Debug.Log` non guardée par `#if UNITY_EDITOR`.
- [ ] 8-10 commits atomiques, footer Co-Authored-By Claude.

---

## 6. Effort estimé

**8 commits**, ~6-8 h.

1. `feat(data): PerkDef + PerkSetBonusDef SO + PerkRegistry collection`
   — créer les 3 SO classes vides.
2. `feat(editor): BuildPerkAssets MenuItem generates 23 perk SO from V5 source`
   — dump des 17 + 6 perks + 6 set bonuses.
3. `refactor(entities): Hero expose internal setters for Mul fields + AddPerkId`
   — minimal change Hero.cs pour permettre mutation depuis PerkSystem.
4. `feat(systems): PerkSystem.ApplyPerk applies all V5 modifiers data-driven`
   — port `applyPerk` JS → C#.
5. `feat(systems): PerkSystem.RollChoices port V5 rollPerkChoices logic`
   — Fisher-Yates + lastChance transform guarantee.
6. `feat(systems): PerkSystem set bonus auto-apply at threshold + event`
   — `OnSetBonusActivated` fire + apply tag bonuses.
7. `feat(systems): LevelRunner ApplyRunStateToHero on Start + CastleHPMaxMul`
   — wire-up Hero ← RunState ← SaveSystem + castle HP rescale.
8. `feat(ui): PerkChoiceOverlay 3-cards level-up + HudPerkBadges show actives`
   — port `showNextPerkChoice` flow + HUD row.

Optionnel 9. `feat(systems): PerkSystem hooks PlacementController TowerCostMul +
FirstTowerFree` — si pas fait dans le commit 7.

**Ordre recommandé** : 1 → 2 → 3 → 4 → 5 → 6 → 7 → 8. Les commits 1-2 sont
indépendants et permettent de valider le data avant de coder la logique. Le
commit 3 (Hero refactor) doit précéder le commit 4 sinon PerkSystem ne compile
pas.

---

## 7. Risques & mitigations

### R1 — Mul fields actuellement `{ get; private set; }` sur Hero
Hero.cs lignes 48-87 ont tous des setters privés. Soit on les passe
`internal set;` (besoin d'être dans le même assembly Unity, ce qui est le cas
puisque tous les `.cs` sont dans Assembly-CSharp par défaut), soit on ajoute
une méthode `Hero.ApplyPerkValues(perkDef)` qui fait la mutation côté Hero
(garde l'encapsulation mais bloat Hero.cs). **Choix recommandé** : `internal
set;` — c'est explicite, minimal-change, et déjà le pattern utilisé dans
Synergies.cs (cf. lignes 41-60 qui muent `t._buffMul`, `t._pierceBonus` directement
via champs `internal`).

### R2 — Hero.AoeOnNthProjectile manque
Le champ `aoeOnNthProjectile` (set bonus tag "vide" V5) n'existe pas sur Hero.cs.
**Mitigation** : ajouter `public int AoeOnNthProjectile { get; internal set; }`
dans le commit 3 + l'utiliser dans `Hero.Fire()` pour incrémenter un compteur
et trigger AoE 1u tous les N projos. Sinon ignorer le set bonus "vide" en V1.

### R3 — Set bonus "feu" (`+20% cadence globale`) modifie aussi towers
V5 `SET_BONUSES.feu.apply` fait `h.fireRateMul *= (1 - 0.20)` mais le commentaire
dit "cadence globale". En V5 c'est juste le hero (les towers ont leur propre
système). **Mitigation** : appliquer uniquement à `hero.FireRateMul`, documenter
dans le PerkSetBonusDef.feu asset que ça concerne le hero seulement. Si besoin
tour : ajouter `globalTowerFireRateMul` plus tard.

### R4 — SaveSystem RunState absent
Le `SaveSystem` actuel n'a que `ProgressData` (clearedLevels / unlockedLevels).
Pas de `RunState` (heroPerks, heroLevel, heroXP, schoolId). **Mitigation** :
créer une seconde clé PlayerPrefs `cd_runstate_v1` avec `RunState` classe
sérialisable, méthodes `SaveSystem.GetRunState() / SetRunState() /
AppendRunPerk(id) / ClearRunState()`. Le run state se reset au début d'un
nouveau niveau (sauf cas resume).

### R5 — Hero.OnLevelUp event existe déjà mais ApplyRunContext est appelé avant Subscribe
Si `LevelRunner.Start` fait `ApplyRunContext` avant que `PerkChoiceOverlay.Start`
ne souscrive à `OnLevelUp`, les level-ups initiaux (cas hero respawn niveau 5)
ne déclenchent rien — c'est OK car le pool de perks est déjà sauvé dans RunState.
**Mitigation** : `PerkChoiceOverlay` ne lit que les level-ups *futurs*,
les anciens sont rejoués via `RunState.heroPerks` au load.

### R6 — Unity 6 nullable et SerializeField
PerkDef avec ~40 champs sérialisés est gros, mais OK (mirror de l'objet JS V5).
Pas de `Dictionary<>` sérialisable directement Unity, donc `tagCounts` reste
runtime-only dans PerkSystem (pas stocké).

### R7 — UI Canvas + Time.timeScale = 0
Si game pause via `Time.timeScale = 0`, les coroutines pause aussi. Utiliser
`WaitForSecondsRealtime` ou `Time.unscaledDeltaTime` dans les animations
overlay. **Mitigation** : checklist QA.

### R8 — Asset generation idempotence
`BuildPerkAssets` doit pouvoir tourner plusieurs fois sans recréer/dupliquer.
**Mitigation** : check `AssetDatabase.LoadAssetAtPath<PerkDef>(path)` avant
`CreateAsset`, mettre à jour les champs si l'asset existe déjà (pattern V5
BuildAnimatorControllers).

### R9 — Perks "transform" mutuellement exclusives
V5 garantit qu'un seul transform (fireball OU ricochet OU lightning OU
pierceExplode). Le `rollPerkChoices` filtre les transforms si un existe déjà.
**Mitigation** : `PerkSystem.CanApply` retourne false si def.transform et
hero.Perks contient déjà un autre transform — port direct du `hasTransform`
V5 ligne 222.

### R10 — Pas de tests unitaires Unity actuellement
Pas de framework de test installé. **Mitigation** : créer
`Assets/Tests/Editor/PerkSystemTests.cs` avec NUnit si Test Framework est
disponible, sinon valider via play mode scripté + `Debug.Log` (commit 8 ou
suivant). Out-of-scope pour le port initial.

---

## Annexe — Mapping V5 → Unity

| V5 (perks.js)            | Unity                        | Note                |
|--------------------------|------------------------------|---------------------|
| `PERKS` array            | `PerkRegistry.standard[]`    | 17 SO assets        |
| `SCHOOL_PERKS`           | `PerkRegistry.schoolPerks[]` | 6 SO assets         |
| `SET_BONUSES`            | `PerkRegistry.setBonuses[]`  | 6 SO assets         |
| `rollPerkChoices()`      | `PerkSystem.RollChoices()`   | fn singleton        |
| `getPerkById()`          | `PerkRegistry.Get(id)`       | dict lookup         |
| `Hero.applyPerk(perk)`   | `PerkSystem.ApplyPerk(hero,def)` | data-driven, no switch |
| `Hero.applyRunContext()` | `Hero.ApplyRunContext()` (already) + delegate | line 250 |
| `applySchoolSetBonus()`  | `PerkSystem.ApplyFreeSetBonus(hero, tag)` | wire-up RunState |
| `crowdef:hero-levelup` JS event | `Hero.OnLevelUp` C# event | already exists      |
| `crowdef:perk-picked`    | `PerkSystem.OnPerkApplied`   | C# event            |
| `crowdef:set-bonus-activated` | `PerkSystem.OnSetBonusActivated` | C# event       |
| `showNextPerkChoice()`   | `PerkChoiceOverlay.ShowNext()` | UI MonoBehaviour  |
| `_perkQueue`             | `PerkChoiceOverlay._queue`    | int                 |
| `rs.heroPerks`           | `RunState.heroPerks` (List<string>) | SaveSystem    |
