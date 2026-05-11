# Spec — Port V5 BossSystem vers Unity C#

## 1. Contexte

Le pivot Phaser → Unity a déjà migré les SO `EnemyType` boss (10 assets dans `Assets/ScriptableObjects/Enemies/`), les flags `isBoss` / `isApocalypseBoss` / `isBrigand` sur `EnemyType.cs:39-46`, et les mécaniques runtime (charge, summons, AoE blast) sur `Enemy.cs:293-351`. Il manque la **couche système** qui orchestre l'expérience boss :

- **Event bus** : `BossEncounteredEvent` est déclaré dans `GameEvents.cs:40` mais aucun emetteur le `Publish`. Seul `MusicManager.OnBossEncountered` y est abonné — silencieux faute d'émetteur.
- **UI overlay** : pas de barre HP boss à l'écran, pas de cutscene 2 s "⚠ NOM ⚠", pas de vignette danger. La V5 (`BossUI.js`) gère tout via DOM listeners.
- **Détection spawn** : `WaveManager.SpawnEnemy` (l. 199-223) instancie chaque ennemi sans tester `cfg.IsBoss`. Le banner V5 polle `runner.enemies` à chaque frame pour trouver `e.isBoss && !e.dead` (`BossUI.js:19-22`).
- **Phase enraged** : V5 `Enemy.js:750-760` déclenche `crowdef:boss-enraged` à 50 % HP avec +40 % speed. Le port Unity n'a pas ce hook.
- **Boss-rush level** (`boss_rush.js`, 4 boss back-to-back) absent de la migration.

Le port doit centraliser ces signaux dans un singleton `BossSystem`, exposer une UI réactive (UI Toolkit, pattern HudController), et brancher Music/JuiceFX/Audio sur la lifecycle boss. Le mapping 1 boss / monde est déjà imposé par les SO Levels W*-8 existants (vagues finales `{ <boss>: 1, ... }`).

## 2. Fichiers impactés

### Création (nouveaux)
- `/Users/mike/Work/crowd-defense/Assets/Scripts/Data/BossDef.cs` (~80 l) — SO `[CreateAssetMenu]`, **wrapper** autour d'un `EnemyType` existant + champs UI (displayName local, phase thresholds, aura color override).
- `/Users/mike/Work/crowd-defense/Assets/Scripts/Systems/BossSystem.cs` (~180 l) — `MonoSingleton<BossSystem>`, écoute `WaveStartedEvent` + `EnemySpawnedEvent`, gère `_currentBoss` ref, polle HP ratio en `LateUpdate`, publish phase events.
- `/Users/mike/Work/crowd-defense/Assets/Scripts/UI/BossUI.cs` (~150 l) — `MonoBehaviour` lié à `UIDocument` du HUD, gère banner top + cutscene + danger vignette.
- `/Users/mike/Work/crowd-defense/Assets/ScriptableObjects/Bosses/Boss_<World>_<Name>.asset` × 10 — un par monde (data only, créés via menu `CrowdDefense/BossDef` puis liens vers SO Enemies existants).

### Modification
- `/Users/mike/Work/crowd-defense/Assets/Scripts/Common/GameEvents.cs:40` — étendre records :
  - `BossEncounteredEvent(string DisplayName, float MaxHp, Color AuraColor)`
  - `BossHpChangedEvent(float Ratio)`
  - `BossPhaseChangedEvent(string PhaseName, int PhaseIdx)` (0=normal, 1=enraged, 2=desperate)
  - `BossDefeatedEvent(string DisplayName)`
  - `BossChargeWarningEvent`
- `/Users/mike/Work/crowd-defense/Assets/Scripts/Entities/Enemy.cs:243-353` — remplacer `cfg.IsBoss` checks par publication `EventManager.Publish(new BossHpChangedEvent(...))` après `TakeDamage` ; ajouter détection 50 % HP → `BossPhaseChangedEvent("enraged", 1)` + speed×1.4 (lignes 367-376).
- `/Users/mike/Work/crowd-defense/Assets/Scripts/Systems/WaveManager.cs:199-223` — après `enemy.Init(...)`, publier `EnemySpawnedEvent` (déjà déclaré `GameEvents.cs:18` mais non utilisé). BossSystem s'abonne et filtre `cfg.IsBoss`.
- `/Users/mike/Work/crowd-defense/Assets/Scripts/Systems/LevelRunner.cs` — sur `Cleanup` : `EventManager.Publish(new BossDefeatedEvent(""))` pour reset UI (analogue `BossUI.js:88-100` level-restart / level-loaded).
- `/Users/mike/Work/crowd-defense/Assets/UI/HUD.uxml:7-27` — ajouter sous `top-bar` un bloc `<VisualElement name="boss-banner" class="boss-banner hidden">` avec Label nom + bar fill, et `<VisualElement name="boss-cutscene" class="boss-cutscene hidden">` plein écran.
- `/Users/mike/Work/crowd-defense/Assets/UI/HUD.uss` — styles `.boss-banner`, `.boss-banner.show`, `.boss-banner.charging` (animation pulse rouge), `.boss-cutscene`, `.danger-vignette`.
- `/Users/mike/Work/crowd-defense/Assets/Scripts/UI/HudController.cs:43-104` — instancier `BossUI` comme sous-composant ou attacher `BossUI` MonoBehaviour à la même GameObject que `UIDocument`.

## 3. Comportement attendu

Quand le joueur lance une vague qui contient un boss, dès que l'`EnemyType` boss spawn (W*-8 final wave OU boss_rush W2/4/6/8), `BossSystem` détecte via `EnemySpawnedEvent` puis `Publish(BossEncounteredEvent)`. `BossUI` réagit en parallèle : (1) affiche un overlay cutscene 2 s plein écran "⚠ DRAGON DE LAVE ⚠" en typo rouge gras, (2) `LevelRunner.SetPaused(true)` pendant la cutscene (analogue `BossUI.js:46-50`), (3) affiche le banner top permanent avec barre HP rouge custom (couleur = `BossDef.AuraColor`), (4) `MusicManager.Play("boss")` crossfade vers le boss theme.

Pendant le combat, `BossSystem.LateUpdate` poll `_currentBoss.HpRatio` chaque frame et publish `BossHpChangedEvent` si delta > 0.005 (debounce zero-alloc). `BossUI` met à jour la width du fill en `%`. À 50 % HP, `BossSystem` détecte le seuil et publish `BossPhaseChangedEvent("enraged")` une seule fois : `Enemy.cs` applique `speed *= 1.4f` + `summonCooldownMs *= 0.6f`, `JuiceFX.Shake(0.3f, 600)` + danger vignette flash 800 ms + `MusicManager.Play("intense")`.

Quand le boss déclenche son charge (`cfg.ChargeMs > 0`, brigand_boss), `Enemy.cs:801-810` doit publish `BossChargeWarningEvent` → `BossUI` applique classe `.charging` (pulse rouge 90 frames) + danger vignette 1.5 s + SFX `boss_charge` (déjà branché `main.js:1440`). À la mort du boss : `Enemy.TakeDamage` détecte `IsDead && cfg.IsBoss` → publish `BossDefeatedEvent` → `BossUI` cache banner + cutscene, `MusicManager.Play("calm")`. Au restart / level-end, `LevelEvents.RaiseLevelEnd` propage `BossDefeatedEvent` defensive pour reset l'état UI résiduel.

## 4. Pseudo-code des fixes

### 4.1 `BossDef.cs` (nouveau SO, wrapper léger)

```csharp
[CreateAssetMenu(fileName = "BossDef", menuName = "CrowdDefense/BossDef")]
public class BossDef : ScriptableObject
{
    [SerializeField] private EnemyType? enemyType;      // ref vers Assets/ScriptableObjects/Enemies/*.asset
    [SerializeField] private string displayNameFr = ""; // ex: "Dragon de Lave"
    [SerializeField] private int world = 1;             // 1..10
    [SerializeField] private Color auraColor = Color.red;
    [SerializeField] private string cutsceneSubtitle = "";
    [Header("Phase thresholds (HP ratios)")]
    [SerializeField, Range(0f, 1f)] private float enragedAt = 0.5f;
    [SerializeField, Range(0f, 1f)] private float desperateAt = 0.2f;
    [Header("Phase modifiers")]
    [SerializeField] private float enragedSpeedMul = 1.4f;
    [SerializeField] private float enragedSummonCdMul = 0.6f;

    public EnemyType? EnemyType => enemyType;
    public string DisplayNameFr => displayNameFr;
    public int World => world;
    public Color AuraColor => auraColor;
    public float EnragedAt => enragedAt;
    public float EnragedSpeedMul => enragedSpeedMul;
    public float EnragedSummonCdMul => enragedSummonCdMul;
}
```

### 4.2 `BossSystem.cs` (singleton + poll HP)

```csharp
public class BossSystem : MonoSingleton<BossSystem>
{
    [SerializeField] private List<BossDef> registry = new(); // 10 boss SOs
    private Dictionary<string, BossDef> _byEnemyId = new();
    private Enemy? _currentBoss;
    private BossDef? _currentDef;
    private float _lastRatio = 1f;
    private int _currentPhase = 0; // 0=normal, 1=enraged, 2=desperate

    protected override void OnAwakeSingleton()
    {
        foreach (var d in registry)
            if (d.EnemyType != null) _byEnemyId[d.EnemyType.Id] = d;
    }

    private void Start()
    {
        var em = EventManager.Instance;
        em?.Subscribe<EnemySpawnedEvent>(OnEnemySpawned);
        em?.Subscribe<LevelEndedEvent>(_ => ResetBoss());
    }

    private void OnEnemySpawned(EnemySpawnedEvent e)
    {
        var cfg = e.Enemy.Config;
        if (cfg == null || !cfg.IsBoss) return;
        if (!_byEnemyId.TryGetValue(cfg.Id, out var def)) return;
        _currentBoss = e.Enemy;
        _currentDef = def;
        _currentPhase = 0;
        _lastRatio = 1f;
        EventManager.Instance?.Publish(new BossEncounteredEvent(
            def.DisplayNameFr, cfg.Hp, def.AuraColor));
    }

    private void LateUpdate()
    {
        if (_currentBoss == null || _currentBoss.IsDead) { TryDefeat(); return; }
        float ratio = _currentBoss.HpRatio;
        if (Mathf.Abs(ratio - _lastRatio) > 0.005f)
        {
            _lastRatio = ratio;
            EventManager.Instance?.Publish(new BossHpChangedEvent(ratio));
        }
        if (_currentPhase == 0 && _currentDef != null && ratio <= _currentDef.EnragedAt)
        {
            _currentPhase = 1;
            _currentBoss.ApplyEnragedPhase(_currentDef.EnragedSpeedMul, _currentDef.EnragedSummonCdMul);
            EventManager.Instance?.Publish(new BossPhaseChangedEvent("enraged", 1));
        }
    }
    // ... TryDefeat / ResetBoss publishent BossDefeatedEvent
}
```

### 4.3 `BossUI.cs` (UI Toolkit overlay)

```csharp
public class BossUI : MonoBehaviour
{
    private VisualElement? _banner;
    private Label? _bannerName;
    private VisualElement? _bannerFill;
    private VisualElement? _cutscene;
    private Label? _cutsceneText;
    private int _chargeFlashFrames = 0;

    private void Start()
    {
        var root = GetComponent<UIDocument>().rootVisualElement;
        _banner = root.Q<VisualElement>("boss-banner");
        _bannerName = root.Q<Label>("boss-banner-name");
        _bannerFill = root.Q<VisualElement>("boss-banner-fill");
        _cutscene = root.Q<VisualElement>("boss-cutscene");
        _cutsceneText = root.Q<Label>("boss-cutscene-text");
        var em = EventManager.Instance;
        em?.Subscribe<BossEncounteredEvent>(OnEncountered);
        em?.Subscribe<BossHpChangedEvent>(OnHpChanged);
        em?.Subscribe<BossChargeWarningEvent>(OnChargeWarn);
        em?.Subscribe<BossDefeatedEvent>(OnDefeated);
    }

    private void OnEncountered(BossEncounteredEvent e)
    {
        _bannerName!.text = e.DisplayName.ToUpper();
        _bannerFill!.style.width = new Length(100f, LengthUnit.Percent);
        _bannerFill.style.backgroundColor = e.AuraColor;
        _banner!.RemoveFromClassList("hidden"); _banner.AddToClassList("show");
        _cutsceneText!.text = $"⚠ {e.DisplayName.ToUpper()} ⚠";
        _cutscene!.RemoveFromClassList("hidden");
        LevelRunner.Instance?.SetPaused(true);
        StartCoroutine(EndCutsceneAfter(2.0f));
    }
    // OnHpChanged → _bannerFill.style.width = ratio*100%
    // OnChargeWarn → _banner.AddToClassList("charging") + _chargeFlashFrames=90
    // OnDefeated → hide banner + cutscene
}
```

### 4.4 Patch `Enemy.cs` (enraged phase + charge warn)

```csharp
// Dans Enemy.TakeDamage après hp -= dmg, AVANT le check IsDead
if (cfg != null && cfg.IsBoss)
    EventManager.Instance?.Publish(new BossHpChangedEvent(HpRatio));

// Nouvelle méthode publique :
public float HpRatio => cfg != null ? Mathf.Clamp01(hp / (cfg.Hp * pressureHpMul)) : 0f;
public void ApplyEnragedPhase(float speedMul, float summonCdMul)
{
    if (cfg == null) return;
    _enragedSpeedMul = speedMul;
    _enragedSummonCdMul = summonCdMul;
}
// Dans UpdateSummons : if (cfg.SummonCooldownMs * _enragedSummonCdMul) ...
// Dans Update / UpdateFlyer : effectiveSpeed *= _enragedSpeedMul

// Dans le bloc charge cooldown — l. ~800 quand chargeActive devient true :
EventManager.Instance?.Publish(new BossChargeWarningEvent());
```

## 5. Critères de succès

- [ ] `git log --oneline` montre les 6 commits atomiques (cf. §6).
- [ ] Unity Editor charge sans erreur console après reload (`mcp__unity-mcp__read_console` clean).
- [ ] Aucun `Debug.Log` hors `#if UNITY_EDITOR` dans BossSystem / BossUI.
- [ ] Play mode sur W4-8 (Dragon BOSS volcan) : à la vague 6 (`dragon_boss: 1`), cutscene "⚠ DRAGON DE LAVE ⚠" apparaît 2 s, jeu pausé.
- [ ] Banner top reste visible avec barre HP qui descend lors de hits — couleur orange (`auraColor`).
- [ ] À 50 % HP : flash écran + `MusicManager.GetCurrentTrack() == "intense"` + dragon accélère visiblement (×1.4 speed).
- [ ] Mort du boss : banner disparait, `BossDefeatedEvent` publié 1 seule fois, retour music `calm`.
- [ ] W1-8 (Brigand) : à chaque charge cooldown end, banner pulse rouge 1.5 s + SFX `boss_charge` joué.
- [ ] Boss-rush level chargé : 4 boss back-to-back, banner switch correctement entre chaque (pas de fantôme).
- [ ] FPS desktop ≥ 60 pendant phase boss + 50 minions actifs (instrument via Profiler.GetTotalAllocatedMemoryLong).
- [ ] Pas d'alloc GC en `LateUpdate` de `BossSystem` (poll ratio + event sans `new` runtime ; struct records OK car heap-alloc 1×/frame seulement si changement).
- [ ] Test Editor : restart pendant cutscene → UI cleanup propre (banner caché, isPaused=false).

## 6. Effort estimé

**6 commits atomiques**, ~4-6 h Sonnet feature-dev.

1. **`feat(events): BossEncountered/HpChanged/PhaseChanged/Defeated records`** — extension `GameEvents.cs:40-44` + tests EventManager.Publish/Subscribe via play mode.
2. **`feat(data): BossDef SO + 10 boss assets W1-W10`** — créer `BossDef.cs` + `Assets/ScriptableObjects/Bosses/` × 10 via `manage_scriptable_object` MCP (mapping table dans §3 spec).
3. **`feat(entities): Enemy.HpRatio + ApplyEnragedPhase + boss event publish`** — patch `Enemy.cs:243-353` ; expose `HpRatio` getter ; publish `BossHpChangedEvent` dans `TakeDamage` ; publish `BossChargeWarningEvent` dans le bloc charge.
4. **`feat(systems): BossSystem singleton wave/spawn detection + phase poll`** — nouveau `BossSystem.cs` ; abonnement `EnemySpawnedEvent` ; détection seuil enraged dans `LateUpdate` ; cleanup sur `LevelEndedEvent`.
5. **`feat(systems): WaveManager.Publish EnemySpawnedEvent on SpawnEnemy`** — patch `WaveManager.cs:218-220` (1 ligne `EventManager.Instance?.Publish(new EnemySpawnedEvent(enemy))`).
6. **`feat(ui): BossUI overlay banner + cutscene + danger vignette`** — `BossUI.cs` + patch `HUD.uxml` (banner + cutscene + vignette VisualElements) + patch `HUD.uss` (`.boss-banner`, `.charging`, `.boss-cutscene`, `.danger-vignette` animations).

**Ordre obligatoire** : 1 → 2 → 3 → 4 → 5 → 6 (UI dépend du bus event ; système dépend des SO ; events dépendent du record).

## 7. Risques & mitigations

### R1 — `EnemySpawnedEvent` jamais publié avant ce port
**Impact** : aucun listener actuel, donc safe d'introduire. **Mitigation** : grep tout abonné `Subscribe<EnemySpawnedEvent>` ; n'utiliser que BossSystem comme consommateur initial.

### R2 — Double déclenchement enraged phase si Enemy.cs détecte aussi le seuil
**Impact** : V5 a la détection dans `Enemy.js:750-760`. Si on duplique dans `Enemy.cs` + `BossSystem.cs`, le boss devient `speed ×1.96`. **Mitigation** : déléguer **uniquement** à `BossSystem.LateUpdate` ; `Enemy.cs` n'a qu'un setter `ApplyEnragedPhase(float, float)` appelé par BossSystem.

### R3 — `LevelRunner.SetPaused(true)` pendant cutscene bloque tout y compris `BossUI.StartCoroutine`
**Impact** : si `Time.timeScale = 0`, les coroutines `WaitForSeconds` ne tickent pas → cutscene infinie. **Mitigation** : utiliser `WaitForSecondsRealtime(2f)` (analogue `MusicManager.cs:322` pattern existant).

### R4 — `BossDef.EnemyType` ref vers SO Enemy peut être perdue au domain reload
**Impact** : `_byEnemyId` dict vide après reload Unity → boss spawn non détecté. **Mitigation** : rebuild dict dans `Start()` ET `OnAwakeSingleton()` ; logger via `#if UNITY_EDITOR` si dict vide à `OnEnemySpawned`.

### R5 — UI Toolkit `RemoveFromClassList("hidden")` ne déclenche pas transition CSS si élément absent du tree
**Impact** : si UXML mal modifié, `_banner == null` → NullRef. **Mitigation** : guard `if (_banner == null) return;` dans toutes les méthodes de `BossUI` ; ajouter `Debug.LogError` éditeur-only si init échoue.

### R6 — Boss-rush level (4 boss) : `BossSystem._currentBoss` ref stale entre 2 boss
**Impact** : si boss N+1 spawn avant que `EnemyKilledEvent` N soit publié, le swap rate. **Mitigation** : dans `OnEnemySpawned`, si `_currentBoss != null && !_currentBoss.IsDead`, prioriser le **plus récent** (assignation directe) **mais** publier `BossDefeatedEvent` du précédent en cleanup logique. Alternative simple : forcer 1 boss actif max (assert + warn).

### R7 — Mapping `EnemyType.Id` ↔ `BossDef` brittle (string keys)
**Impact** : typo dans Id → boss jamais reconnu. **Mitigation** : `BossDef` réfère **directement** l'asset `EnemyType` via `[SerializeField]` ; le dict utilise `enemyType.Id` lu depuis le SO chargé, pas un string libre.

### R8 — `BossEncounteredEvent` déjà subscribed par `MusicManager` mais signature change
**Impact** : `MusicManager.cs:332` lit `BossEncounteredEvent _` sans arg → compat OK avec record paramétré (discard `_`). **Mitigation** : aucune action requise, juste s'assurer que la signature étendue (`DisplayName, MaxHp, AuraColor`) ne casse pas l'override `MusicManager.OnBossEncountered`.

### Mapping table BossDef ↔ Levels (réf §6 commit 2)

| World | Theme       | EnemyType SO            | DisplayName FR             | Final level     |
| ----- | ----------- | ----------------------- | -------------------------- | --------------- |
| 1     | plaine      | `BrigandBoss.asset`     | Brigand de la Plaine       | W1-8            |
| 2     | foret       | `WarlordBoss.asset`     | Sorcier de la Forêt        | W2-8            |
| 3     | desert      | `CorsairBoss.asset`     | Capitaine Corsaire         | W3-8 (foire reuse) |
| 4     | volcan      | `DragonBoss.asset`      | Dragon de Lave             | W4-8            |
| 5     | foire       | `CorsairBoss.asset`     | Capitaine Corsaire         | W5-8            |
| 6     | apocalypse  | `ApocalypseBoss.asset`  | L'Apocalypse               | W6-8            |
| 7     | espace      | `CosmicBoss.asset`      | Entité Galactique          | W7-8            |
| 8     | submarin    | `KrakenBoss.asset`      | Le Kraken                  | W8-8            |
| 9     | medieval    | `WizardKing.asset`      | Le Sorcier-Roi             | W9-8            |
| 10    | cyberpunk   | `AiHub.asset`           | Hub IA                     | W10-8           |
