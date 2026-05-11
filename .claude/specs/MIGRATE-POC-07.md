# MIGRATE-POC-07 — HUD UI Toolkit (USS + UXML)

> Ticket 7/8 du Phase 1 POC. HUD overlay Gold/Wave/HP + panels GameOver/Victory en UI Toolkit.

## Type & Effort

- **Type** : feature-dev
- **Estimé** : 2 commits, ~70 min
- **Bloqué par** : POC-01..06 ✅
- **Branch** : `main` direct
- **Working dir** : `/Users/mike/Work/crowd-defense/`

## Objectif

HUD overlay minimal en **UI Toolkit** (USS + UXML, runtime UIDocument) :
- Pill Gold top-left
- Pill Wave top-center (`W 1/4`)
- Pill HP top-right (`120/120`) avec barre proportionnelle colorée (vert → orange → rouge)
- Panel GameOver (caché par défaut, affiché sur LevelRunner state=GameOver)
- Panel Victory (caché par défaut, affiché sur state=Victory)
- Bouton Restart sur les 2 panels → `SceneManager.LoadScene(0)`

3 fichiers à créer + 1 GameObject scene :
1. `Assets/UI/HUD.uxml` (layout)
2. `Assets/UI/HUD.uss` (styles)
3. `Assets/Scripts/UI/HudController.cs` (bindings + event subscriptions)
4. Scene : GameObject "HUD" avec component `UIDocument` (UI Toolkit runtime) référençant le UXML + un PanelSettings.

## Source canonique

UI Toolkit n'existe pas dans le source Phaser (HTML DOM). Pour référence layout/copy :
- `/Users/mike/Work/milan project/index.html` : pas critique, juste pour idée pills disposition.

Mais surtout, **doc Unity** :
- UI Builder : Window > UI Toolkit > UI Builder (visual editor pour UXML).
- Runtime UIDocument : https://docs.unity3d.com/Manual/UIE-runtime-rendering.html.

## Décisions techniques

- **UI Toolkit runtime** : `UIDocument` component avec :
  - `Source Asset` = `HUD.uxml`
  - `Panel Settings` = nouveau `PanelSettings.asset` (créer via menu Assets > Create > UI Toolkit > Panel Settings Asset)
  - `Sort Order` = 0 (par-dessus la scene 3D)
- **USS class-based** (pas inline style). Permet hot reload sur edit.
- **Query rootVisualElement** : `var goldLabel = root.Q<Label>("gold-value");` puis `goldLabel.text = $"{gold} g";`.
- **EventSystem** : UI Toolkit gère ses propres clicks. Mais raycast Tower placement utilise legacy Input mouse — vérifier qu'un click sur Restart bouton ne place pas une tower aussi (filter via `MouseDownEvent.StopPropagation()`).
- **Time.unscaledDeltaTime** : non requis ici car UI Toolkit utilise son propre Update independent de Time.timeScale.

---

## Commit 1 — `feat(ui): add HUD UXML + USS + PanelSettings`

### Fichier : `Assets/UI/HUD.uxml`

```xml
<ui:UXML xmlns:ui="UnityEngine.UIElements" xmlns:uie="UnityEditor.UIElements"
         xsi="http://www.w3.org/2001/XMLSchema-instance" engine="UnityEngine.UIElements"
         editor="UnityEditor.UIElements" noNamespaceSchemaLocation="../UIElementsSchema/UIElements.xsd">
    <Style src="HUD.uss" />
    <ui:VisualElement name="hud-root" class="hud-root">
        <!-- Top bar pills -->
        <ui:VisualElement class="top-bar">
            <ui:VisualElement class="pill pill-gold" name="pill-gold">
                <ui:Label text="GOLD" class="pill-label" />
                <ui:Label text="120" name="gold-value" class="pill-value" />
            </ui:VisualElement>

            <ui:VisualElement class="pill pill-wave" name="pill-wave">
                <ui:Label text="WAVE" class="pill-label" />
                <ui:Label text="1/4" name="wave-value" class="pill-value" />
            </ui:VisualElement>

            <ui:VisualElement class="pill pill-hp" name="pill-hp">
                <ui:Label text="HP" class="pill-label" />
                <ui:Label text="120/120" name="hp-value" class="pill-value" />
                <ui:VisualElement class="hp-bar-bg">
                    <ui:VisualElement name="hp-bar-fill" class="hp-bar-fill" />
                </ui:VisualElement>
            </ui:VisualElement>
        </ui:VisualElement>

        <!-- Game Over panel -->
        <ui:VisualElement name="panel-game-over" class="overlay-panel overlay-game-over hidden">
            <ui:Label text="GAME OVER" class="overlay-title" />
            <ui:Label text="Le castle est tombé." class="overlay-subtitle" />
            <ui:Button text="Recommencer" name="btn-restart-go" class="overlay-button" />
        </ui:VisualElement>

        <!-- Victory panel -->
        <ui:VisualElement name="panel-victory" class="overlay-panel overlay-victory hidden">
            <ui:Label text="VICTOIRE" class="overlay-title" />
            <ui:Label text="W1-1 clear." class="overlay-subtitle" />
            <ui:Button text="Rejouer" name="btn-restart-victory" class="overlay-button" />
        </ui:VisualElement>
    </ui:VisualElement>
</ui:UXML>
```

### Fichier : `Assets/UI/HUD.uss`

```css
.hud-root {
    width: 100%;
    height: 100%;
    position: absolute;
}

.top-bar {
    flex-direction: row;
    justify-content: space-between;
    padding: 16px 24px;
    width: 100%;
}

.pill {
    flex-direction: column;
    background-color: rgba(0, 0, 0, 0.65);
    border-radius: 14px;
    padding: 8px 16px;
    min-width: 100px;
    align-items: center;
}

.pill-label {
    color: rgba(220, 220, 220, 0.7);
    font-size: 12px;
    -unity-font-style: bold;
}

.pill-value {
    color: white;
    font-size: 24px;
    -unity-font-style: bold;
}

.pill-gold .pill-value {
    color: rgb(255, 210, 63);
}

.pill-hp {
    min-width: 140px;
}

.hp-bar-bg {
    height: 6px;
    background-color: rgba(40, 40, 40, 0.9);
    border-radius: 3px;
    margin-top: 4px;
    width: 100%;
}

.hp-bar-fill {
    height: 100%;
    width: 100%;
    background-color: rgb(80, 220, 80);
    border-radius: 3px;
}

.overlay-panel {
    position: absolute;
    width: 100%;
    height: 100%;
    background-color: rgba(0, 0, 0, 0.7);
    align-items: center;
    justify-content: center;
}

.overlay-game-over {
    background-color: rgba(80, 10, 10, 0.85);
}

.overlay-victory {
    background-color: rgba(10, 60, 10, 0.85);
}

.overlay-title {
    color: white;
    font-size: 72px;
    -unity-font-style: bold;
    margin-bottom: 8px;
}

.overlay-subtitle {
    color: rgba(220, 220, 220, 0.9);
    font-size: 24px;
    margin-bottom: 32px;
}

.overlay-button {
    padding: 16px 48px;
    background-color: rgba(255, 255, 255, 0.15);
    color: white;
    border-radius: 8px;
    font-size: 24px;
    border-width: 0;
}

.overlay-button:hover {
    background-color: rgba(255, 255, 255, 0.3);
}

.hidden {
    display: none;
}
```

### PanelSettings : `Assets/UI/HUDPanelSettings.asset`

Créer via menu Editor : `Assets > Create > UI Toolkit > Panel Settings Asset` puis renommer.

Réglages essentiels :
- `Scale Mode` = Scale With Screen Size
- `Reference Resolution` = 1920×1080
- `Match` = 0.5 (mix W/H)
- `Theme Style Sheet` = défaut Unity

Création via MCP fallback `execute_code` :
```csharp
var ps = ScriptableObject.CreateInstance<UnityEngine.UIElements.PanelSettings>();
ps.scaleMode = UnityEngine.UIElements.PanelScaleMode.ScaleWithScreenSize;
ps.referenceResolution = new Vector2Int(1920, 1080);
ps.match = 0.5f;
AssetDatabase.CreateAsset(ps, "Assets/UI/HUDPanelSettings.asset");
AssetDatabase.SaveAssets();
```

### Process commit 1

1. Write UXML + USS.
2. Créer PanelSettings via MCP.
3. `refresh_unity`.
4. `git add Assets/UI/` + commit `feat(ui): add HUD UXML layout + USS styles + PanelSettings`.

---

## Commit 2 — `feat(ui): add HudController binding events Economy/Castle/WaveManager/LevelRunner + scene setup`

### Fichier : `Assets/Scripts/UI/HudController.cs`

```csharp
#nullable enable
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;
using CrowdDefense.Systems;
using CrowdDefense.Entities;

namespace CrowdDefense.UI
{
    [RequireComponent(typeof(UIDocument))]
    public class HudController : MonoBehaviour
    {
        private Label? goldValue;
        private Label? waveValue;
        private Label? hpValue;
        private VisualElement? hpBarFill;
        private VisualElement? panelGameOver;
        private VisualElement? panelVictory;
        private Button? btnRestartGo;
        private Button? btnRestartVictory;

        private void Start()
        {
            var root = GetComponent<UIDocument>().rootVisualElement;
            goldValue = root.Q<Label>("gold-value");
            waveValue = root.Q<Label>("wave-value");
            hpValue = root.Q<Label>("hp-value");
            hpBarFill = root.Q<VisualElement>("hp-bar-fill");
            panelGameOver = root.Q<VisualElement>("panel-game-over");
            panelVictory = root.Q<VisualElement>("panel-victory");
            btnRestartGo = root.Q<Button>("btn-restart-go");
            btnRestartVictory = root.Q<Button>("btn-restart-victory");

            btnRestartGo?.RegisterCallback<ClickEvent>(_ => Restart());
            btnRestartVictory?.RegisterCallback<ClickEvent>(_ => Restart());

            if (Economy.Instance != null)
            {
                Economy.Instance.OnGoldChanged += OnGoldChanged;
                OnGoldChanged(Economy.Instance.Gold);
            }
            if (Castle.Instance != null)
            {
                Castle.Instance.OnHPChanged += OnHPChanged;
                OnHPChanged(Castle.Instance.HP, Castle.Instance.HPMax);
            }
            if (WaveManager.Instance != null)
            {
                WaveManager.Instance.OnWaveStart += OnWaveStart;
                OnWaveStart(WaveManager.Instance.CurrentWaveIdx);
            }
            if (LevelRunner.Instance != null)
            {
                LevelRunner.Instance.OnStateChanged += OnStateChanged;
                OnStateChanged(LevelRunner.Instance.State);
            }
        }

        private void OnDestroy()
        {
            if (Economy.Instance != null) Economy.Instance.OnGoldChanged -= OnGoldChanged;
            if (Castle.Instance != null) Castle.Instance.OnHPChanged -= OnHPChanged;
            if (WaveManager.Instance != null) WaveManager.Instance.OnWaveStart -= OnWaveStart;
            if (LevelRunner.Instance != null) LevelRunner.Instance.OnStateChanged -= OnStateChanged;
        }

        private void OnGoldChanged(int gold)
        {
            if (goldValue != null) goldValue.text = gold.ToString();
        }

        private void OnHPChanged(int hp, int hpMax)
        {
            if (hpValue != null) hpValue.text = $"{hp}/{hpMax}";
            if (hpBarFill != null)
            {
                float ratio = hpMax > 0 ? (float)hp / hpMax : 0f;
                hpBarFill.style.width = new Length(ratio * 100f, LengthUnit.Percent);
                hpBarFill.style.backgroundColor = ratio > 0.6f ? new Color(0.31f, 0.86f, 0.31f)
                                                : ratio > 0.3f ? new Color(0.86f, 0.55f, 0.13f)
                                                : new Color(0.86f, 0.20f, 0.13f);
            }
        }

        private void OnWaveStart(int idx)
        {
            if (waveValue == null || WaveManager.Instance == null) return;
            waveValue.text = $"{idx + 1}/{WaveManager.Instance.TotalWaves}";
        }

        private void OnStateChanged(GameState state)
        {
            if (panelGameOver != null) SetVisible(panelGameOver, state == GameState.GameOver);
            if (panelVictory != null) SetVisible(panelVictory, state == GameState.Victory);
        }

        private static void SetVisible(VisualElement el, bool visible)
        {
            if (visible) el.RemoveFromClassList("hidden");
            else el.AddToClassList("hidden");
        }

        private void Restart()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }
}
```

### Scene setup

1. Créer GameObject "HUD" racine scene (pas sous Systems).
2. Ajouter component `UIDocument` :
   - Source Asset = `Assets/UI/HUD.uxml`
   - Panel Settings = `Assets/UI/HUDPanelSettings.asset`
3. Ajouter component `HudController.cs`.
4. Save scene.

Setup via MCP :
```csharp
// execute_code
var go = new GameObject("HUD");
var doc = go.AddComponent<UnityEngine.UIElements.UIDocument>();
doc.panelSettings = AssetDatabase.LoadAssetAtPath<UnityEngine.UIElements.PanelSettings>("Assets/UI/HUDPanelSettings.asset");
doc.visualTreeAsset = AssetDatabase.LoadAssetAtPath<UnityEngine.UIElements.VisualTreeAsset>("Assets/UI/HUD.uxml");
go.AddComponent<CrowdDefense.UI.HudController>();
EditorSceneManager.MarkSceneDirty(go.scene);
EditorSceneManager.SaveScene(go.scene);
```

### Process commit 2

1. Write HudController.cs.
2. `refresh_unity` + compile check.
3. Scene setup HUD GO + UIDocument + HudController via MCP.
4. Test play mode 30s : verify pills visible top, valeurs update.
5. Test game over scenario : laisser castle mourir → panel rouge visible avec bouton Recommencer → click → scene reload.
6. `git add Assets/Scripts/UI/HudController.cs Assets/Scenes/` + commit.
7. Push.

---

## Verification finale

```bash
find Assets/UI -type f | wc -l           # 3 (HUD.uxml, HUD.uss, HUDPanelSettings.asset) + meta files
find Assets/Scripts/UI -name "*.cs" | wc -l # 1
```

Via MCP play mode :
- Pill Gold visible top-left avec valeur (couleur dorée).
- Pill Wave visible top-center "1/4".
- Pill HP visible top-right "120/120" avec barre verte pleine.
- Kill un enemy → Gold pill update.
- Castle prend damage → HP value descend + barre rétrécit + change couleur (vert > 60% → orange > 30% → rouge).
- HP atteint 0 → panel GameOver rouge translucide overlay.
- Click Restart → scene reload.

**Critères succès** :
- UXML + USS + PanelSettings créés
- HUD overlay visible Play mode
- Tous les events Economy/Castle/Wave/Runner branchés
- Game over / Victory panels apparaissent au bon moment
- Restart bouton fonctionne
- 2 commits pushed

## Pièges anticipés

1. **TextMeshPro vs UI Toolkit** : UI Toolkit utilise sa propre stack texte (pas TMP). Si tu vois texte fallback ugly → la font UI Toolkit par défaut suffit POC.
2. **PanelSettings null** : si tu oublies d'assigner PanelSettings sur UIDocument → rien ne s'affiche. Le component log un warning.
3. **Click Restart aussi place tower** : si PlacementController.Update ne filter pas, click sur Restart bouton aussi spawn tower (mais sur cell hors-grid → reject). Sinon `EventSystem.current.IsPointerOverGameObject()` ne marche pas avec UI Toolkit (legacy uGUI only). Solution : dans UI Toolkit, `panelSettings.SetScreenToPanelSpaceFunction()` ou un flag global `bool isUIInteracting` set par `ClickEvent` capture sur root.
4. **USS hot reload** : Unity auto-reload les .uss changes pendant Play mode. Pratique pour itérer styles sans restart.
5. **`Length` enum** : `LengthUnit.Percent` (pourcent) vs `LengthUnit.Pixel` (px). Pour la barre HP fill, on veut Percent.
6. **Wave display starts at 1** : `idx` est 0-based, on display `idx+1`. Le brief le fait.

## Quand fini

2 commits push, termine.
