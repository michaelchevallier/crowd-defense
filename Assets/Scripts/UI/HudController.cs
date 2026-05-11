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
                hpBarFill.style.backgroundColor = ratio > 0.6f
                    ? new Color(0.31f, 0.86f, 0.31f)
                    : ratio > 0.3f
                        ? new Color(0.86f, 0.55f, 0.13f)
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
