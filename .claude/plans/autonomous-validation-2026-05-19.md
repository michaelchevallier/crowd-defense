# Plan de validation autonome Crowd Defense V6 — 2026-05-19

## Objectif

Atteindre **80%+ confiance autonome** sur l'état du jeu sans intervention Mike (Cmd+P, screenshot manuel, etc.). Mike valide le **dernier 20%** (subjectif UX).

## Contraintes connues

| # | Bloquant | Impact | Workaround |
|---|---|---|---|
| C1 | Unity-MCP HTTP 8080 DOWN (package coplaydev port dynamique) | Pas de cycle fix+test interactif via curl | Skip MCP, use batch mode |
| C2 | V3LoopAutoRunner.Tick utilise EditorApplication.update qui ne tourne pas en batch `-quit` | Pas de phase loop runtime headless | Run batch SANS -quit + poll report file |
| C3 | Unity Editor lock empêche batch mode pendant qu'il tourne | Soit batch, soit Mike Editor — pas les deux | Kill Editor pour batch, relance après |
| C4 | Pas de Play mode possible en pure batch (EnterPlayMode skip si batch) | Aucune validation gameplay sans Editor interactif | Edit Mode tests + direct manipulation Sans Play |
| C5 | WebGL Unity 6 + URP 17 cassé (Mike memory) | Pas de WebGL test | Skip WebGL |
| C6 | Mike a Unity Editor ouvert souvent (work in progress) | Kill Editor = risk perte changes | Toujours git status check avant kill, save reminder |

## 6 niveaux de validation autonome

### Niveau 1 — Static Analysis Pipeline ✅ DÉJÀ DEPLOYÉ

| Quoi | Comment | Validé |
|---|---|---|
| Compile C# | `Unity -batchmode -executeMethod NOOP -quit -logFile /tmp/compile.log` puis grep `error CS` | ✅ Wave B-Soir validé (commit `313d106e`) |
| Audit patterns | Explore agents : `transform.position +=`, `GetComponentsInChildren` dans Update, NRE, Color.Lerp, etc. | ✅ 5 agents passés |
| Resources.Load coverage | `grep "Resources.Load" + verify Assets/Resources/ exists` | ✅ R2A+R2B fixés |
| Shader.Find fallback | `grep "Shader.Find" + verify Always Included` | ✅ Audit OK |

**Output** : Pre-flight check before any commit. Cycle : 1-2 min.

### Niveau 2 — Edit Mode Test Suite (À CRÉER, P0)

Script `Assets/Tests/Editor/V3BatchValidator.cs`. Pure Edit Mode, pas de Play.

```csharp
[MenuItem("Tools/CrowdDefense/QA/V3Batch/RunAll")]
public static void RunAll()
{
    var report = new V3Report();
    try {
        Test_Singletons(report);
        Test_LevelDataLoad(report);
        Test_PathfindingGrid(report);
        Test_TowerData(report);
        Test_EnemyData(report);
        Test_AudioRegistry(report);
        Test_Shaders(report);
        Test_UIDocuments(report);
        Test_Resources(report);
        Test_LevelRegistry(report);
    } finally {
        report.WriteToFile("Library/V3BatchReports/edit-mode.txt");
    }
}
```

Chaque test :
- Instancie le système via API (pas via scene)
- Vérifie state attendu
- PASS si match, FAIL avec message si non

**Avantage** : marche en `-batchmode -executeMethod V3BatchValidator.RunAll -quit`. Pas besoin de Play.

**Coverage attendu** :
- 50% des bugs runtime (NRE init, asset miss, wiring fail)
- 0% des bugs Play mode (EnterPlayMode skip)

**Effort** : 1 agent feature-dev, ~1-2h.

### Niveau 3 — Play Mode via batch SANS -quit + Monitor (À CRÉER, P0)

Pour valider gameplay réel (waves, mobs, towers fire, hero combat), il faut Play mode. Approche :

```bash
# Step 1: kill Unity Editor + run batch sans -quit en background
kill $(pgrep -f "Unity.app/Contents/MacOS/Unity -projectPath")
Unity -batchmode -projectPath . -executeMethod V3PlayModeRunner.RunHeadless \
  -logFile /tmp/v3-play.log &
UNITY_PID=$!

# Step 2: monitor le file Library/V3LoopBatchReports/latest-auto.txt
until [ -f Library/V3LoopBatchReports/latest-auto.txt ] || ! kill -0 $UNITY_PID; do
  sleep 5
done

# Step 3: read report, kill Unity, relance Editor
cat Library/V3LoopBatchReports/latest-auto.txt
kill $UNITY_PID
open -a Unity --args -projectPath .
```

`V3PlayModeRunner.RunHeadless` nouveau script :
1. `EditorApplication.EnterPlaymode()` (works in batch ? À tester)
2. Wait scene loaded
3. Spawn castle/hero/wave via API
4. Tick frames (Time.deltaTime simulated)
5. Verify state
6. Exit Play mode + write report

**Limites** : 
- EnterPlaymode peut être skipped en batch (Unity 2020+ docs)
- Plan B : si batch EnterPlayMode skip, fallback à PlayModeTests via Unity Test Framework

**Effort** : 1 agent, 2-3h. Risque haut.

### Niveau 4 — Visual Regression via Screenshot Editor (À CRÉER, P1)

Script Editor batch :

```csharp
[MenuItem("Tools/CrowdDefense/QA/V3Batch/Screenshot")]
public static void Screenshot()
{
    foreach (var scene in new[] {"Loader", "Menu", "WorldMap", "Main"}) {
        EditorSceneManager.OpenScene($"Assets/Scenes/{scene}.unity");
        // Force render Game view via Camera.targetTexture
        var cam = Camera.main;
        var rt = new RenderTexture(1920, 1080, 32);
        cam.targetTexture = rt;
        cam.Render();
        // Save PNG
        var tex = new Texture2D(rt.width, rt.height);
        RenderTexture.active = rt;
        tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
        File.WriteAllBytes($"Library/V3Screenshots/{scene}.png", tex.EncodeToPNG());
    }
}
```

Output : 4 PNG dans `Library/V3Screenshots/`. Compare vs baseline images (golden) via pixel diff Python OpenCV.

**Détecte** : URP magenta materials, missing meshes, UI elements visible/invisible, layout broken.

**Limites** :
- En batch, scene cameras + lighting peuvent pas être full populated (singletons auto-create marche mais sans HUD setup)
- Baseline images à générer manuellement la première fois (1× session Mike)

**Effort** : 1 agent, ~2h.

### Niveau 5 — Console Log Pattern Validator (À CRÉER, P1)

Plus simple que Niveau 3. Run Unity batch SANS -quit en background, monitor `~/Library/Logs/Unity/Editor.log` pour patterns.

```bash
# Lance batch + tail log en parallèle
Unity -batchmode -projectPath . -executeMethod V3LoopAutoRunner.RunNow \
  -logFile /tmp/v3-log.log &
UNITY_PID=$!

# Tail le log, kill quand pattern PASS/FAIL trouvé OU timeout 15min
timeout 900 tail -f /tmp/v3-log.log | awk '
  /phase11 FINAL VICTORY PASS/ { print "PASS"; exit 0 }
  /phase.*FAIL/ { print "FAIL"; exit 1 }
'
RESULT=$?
kill $UNITY_PID 2>/dev/null
echo "Test result: $RESULT"
```

**Avantage** : utilise V3LoopAutoRunner existant (déjà conçu). Marche si EditorApplication.update tourne en batch (à confirmer).

**Effort** : juste Bash script, 30 min.

### Niveau 6 — Performance Profiling (À CRÉER, P2)

Editor batch script avec `UnityEngine.Profiling.Profiler.BeginSample/EndSample`.

```csharp
public static void ProfileGameplay()
{
    Profiler.BeginSample("Tower.Update x100");
    for (int i = 0; i < 100; i++) tower.Update();
    Profiler.EndSample();
    // Log to file
}
```

Output : ms/frame moyens, GC alloc per Update.

**Détecte** : perf regressions, allocs cachées.

**Effort** : 1 agent, ~2h. Low ROI sans bottleneck identifié.

## Ordre d'implémentation

| Wave | Niveau | Effort | Priorité |
|------|--------|--------|----------|
| W1 (NOW) | N5 — Console log validator | 30 min | P0 |
| W1 | N2 — Edit Mode Test Suite | 1-2h | P0 |
| W2 | N3 — Play Mode batch headless | 2-3h (risque haut) | P0 |
| W3 | N4 — Visual regression screenshots | 2h | P1 |
| W4 | N6 — Performance profiling | 2h | P2 |

## Stop conditions / Notify Mike

| Condition | Action |
|-----------|--------|
| N5 PASS sur V3LoopAutoRunner | Confidence runtime ↑ MEDIUM → HIGH. Brief Mike T2. |
| N5 FAIL avec stack trace clair | Dispatch bug-fixer auto. T1 si non-trivial. |
| N3 Play mode batch impossible (EnterPlayMode skip) | Document, fallback N2 + N5 only. Mike test final. T2. |
| Niveau 2 catch un bug pre-flight | Fix puis re-run. Brief Mike T2 si > 2 bugs found. |
| Tout Niveau 1-5 PASS | Confidence HIGH. Mike test final pour 100%. T1 "Prêt pour test final". |

## Risks

- **R1** : Unity Editor lock conflict si Mike ouvre Editor pendant batch run → batch fail. Mitigation : check `pgrep Unity` avant kill, defer batch si Editor running with unsaved changes.
- **R2** : EnterPlaymode skip en batch (Unity docs unclear). Mitigation : test N3 sur 1 scenario simple, fallback N2 si fail.
- **R3** : V3LoopAutoRunner.Tick peut ne pas tourner en batch même sans -quit. Mitigation : Niveau 5 expérimental, si fail abandon.

## Workflow proposé pour autonomie 48h

```
1. Implement N5 (30 min) → run V3LoopAutoRunner batch
   - PASS : confidence ↑, scrute clean-log, attendre cron
   - FAIL : dispatch bug-fixer, fix, retry

2. Implement N2 (1-2h) → cover Edit Mode tests
   - 10 test fonctions covering core systems
   - Run après chaque commit pre-flight

3. Try N3 (2-3h, accept may fail)
   - Si OK : full runtime validation
   - Si fail : skip, document, N2+N5 only

4. N4 visual screenshot (2h) si N3 OK
   - Baseline screenshots Mike via Editor
   - Pixel diff after each fix

5. Cycle continu :
   - Cron supervise 30min
   - À chaque commit code: N5 + N2 pre-flight
   - À chaque fin journée : N5 + N3 full validation
   - Update _clean-log.md avec PASS/FAIL
```

## Validation gate finale (Mike retour)

Quand Mike revient (après nuit / pause) :
1. Lit `_clean-log.md` pour voir cumul scrutes
2. Lit dernier `latest-auto.txt` pour résultat V3LoopAutoRunner
3. Lit `Library/V3BatchReports/edit-mode.txt` pour Edit Mode tests
4. Lit `Library/V3Screenshots/*.png` pour visual check
5. Si tout vert → Mike fait juste UX check 5 min (gameplay feel)
6. Si rouge → Mike investigue + dispatch fix

**Confidence target post-N5+N2 implementation** : 70-85%. Mike test final pour 100%.
