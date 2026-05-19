#nullable enable
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using CrowdDefense.Systems;

namespace CrowdDefense.UI
{
    // 3-step animated arrow guide shown once on W1-1.
    // Steps: empty buildable cell → wave button → hero.
    // Each step auto-advances after 5s or on any click.
    // Driven by PlayerPrefs "tutorial_arrow_done_v1".
    public class TutorialArrowGuide : MonoBehaviour
    {
        private const string PrefKey  = "tutorial_arrow_done_v1";
        private const string LevelId  = "world1-1";
        private const float  AutoSecs = 5f;
        private const float  BobAmp   = 12f;  // px up/down
        private const float  BobFreq  = 2.5f; // hz

        private static readonly string[] Labels =
        {
            "Clique ici pour placer une tour",
            "Lance la vague",
            "Ton heros defend le chateau"
        };

        private int              _step;
        private float            _stepTimer;
        private bool             _advancing;
        private RectTransform?   _arrowRect;
        private Text?            _labelText;
        private CanvasGroup?     _cg;
        private Vector3          _targetScreenPos;

        public static void TryStart(string? levelId)
        {
            if (PlayerPrefs.GetInt(PrefKey, 0) == 1) return;
            if (levelId != LevelId) return;

            var go = new GameObject("[TutorialArrowGuide]");
            DontDestroyOnLoad(go);
            go.AddComponent<TutorialArrowGuide>().Build();
        }

        private void Build()
        {
            var canvas          = gameObject.AddComponent<Canvas>();
            canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 998;
            gameObject.AddComponent<CanvasScaler>();
            gameObject.AddComponent<GraphicRaycaster>();

            _cg       = gameObject.AddComponent<CanvasGroup>();
            _cg.alpha = 0f;

            // Arrow triangle (rotated Image with custom color acting as simple shape)
            var arrowGo      = new GameObject("Arrow");
            arrowGo.transform.SetParent(transform, false);
            var arrowImg     = arrowGo.AddComponent<Image>();
            arrowImg.color   = new Color(1f, 0.85f, 0.1f, 1f);
            _arrowRect       = arrowGo.GetComponent<RectTransform>();
            _arrowRect.sizeDelta = new Vector2(28f, 36f);
            // Rotate so the triangle tip points downward (toward target)
            arrowGo.transform.localRotation = Quaternion.Euler(0f, 0f, 180f);

            // Label above the arrow
            var lblGo        = new GameObject("Label");
            lblGo.transform.SetParent(transform, false);
            _labelText       = lblGo.AddComponent<Text>();
            _labelText.font  = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _labelText.fontSize  = 15;
            _labelText.fontStyle = FontStyle.Bold;
            _labelText.alignment = TextAnchor.MiddleCenter;
            _labelText.color     = Color.white;
            var shadow       = lblGo.AddComponent<Shadow>();
            shadow.effectColor    = new Color(0f, 0f, 0f, 0.8f);
            shadow.effectDistance = new Vector2(1f, -1f);
            var lblRect      = lblGo.GetComponent<RectTransform>();
            lblRect.sizeDelta = new Vector2(260f, 40f);

            StartCoroutine(RunSequence());
        }

        private IEnumerator RunSequence()
        {
            // Small delay so the scene is fully initialised
            yield return new WaitForSecondsRealtime(0.8f);

            // Fade in
            yield return StartCoroutine(Fade(0f, 1f, 0.35f));

            for (_step = 0; _step < 3; _step++)
            {
                _stepTimer = 0f;
                _advancing = false;

                if (_labelText != null)
                    _labelText.text = Labels[_step];

                // Resolve world target for this step
                _targetScreenPos = ResolveScreenPos(_step);

                // Wait for click or auto-advance
                while (_stepTimer < AutoSecs && !_advancing)
                {
                    _stepTimer += Time.unscaledDeltaTime;

                    if (Input.GetMouseButtonDown(0) || Input.touchCount > 0)
                        _advancing = true;

                    // Bob animation
                    float bob = Mathf.Sin(Time.unscaledTime * BobFreq * Mathf.PI * 2f) * BobAmp;
                    PositionArrow(_targetScreenPos, bob);

                    // Refresh target each frame (hero/HUD may move)
                    _targetScreenPos = ResolveScreenPos(_step);

                    yield return null;
                }

                // Brief flash before advancing
                yield return StartCoroutine(Fade(1f, 0.3f, 0.12f));
                yield return StartCoroutine(Fade(0.3f, 1f, 0.12f));
            }

            // All steps done
            yield return StartCoroutine(Fade(1f, 0f, 0.35f));
            PlayerPrefs.SetInt(PrefKey, 1);
            PlayerPrefs.Save();
            Destroy(gameObject);
        }

        private void PositionArrow(Vector3 screenPos, float bobOffset)
        {
            if (_arrowRect == null || _labelText == null) return;

            // Arrow sits 50px above target (tip pointing down toward target)
            var arrowScreenPos = new Vector2(screenPos.x, screenPos.y + 52f + bobOffset);
            _arrowRect.position = arrowScreenPos;

            // Label 44px above arrow centre
            var lblRect = _labelText.GetComponent<RectTransform>();
            lblRect.position = new Vector2(arrowScreenPos.x, arrowScreenPos.y + 44f);
        }

        // Maps step index to a screen-space position.
        private static Vector3 ResolveScreenPos(int step)
        {
            var cam = Camera.main;

            switch (step)
            {
                case 0:
                {
                    // First buildable cell on the grid
                    var grid = PathManager.Instance?.Grid;
                    if (grid != null && cam != null)
                    {
                        for (int r = 0; r < grid.Height; r++)
                        {
                            for (int c = 0; c < grid.Width; c++)
                            {
                                if (!grid.IsBuildable(c, r)) continue;
                                var world = GridCoords.CellToWorld(c, r, grid.Width, grid.Height, grid.CellSize);
                                return cam.WorldToScreenPoint(world);
                            }
                        }
                    }
                    // Fallback: lower-left quarter of screen
                    return new Vector3(Screen.width * 0.25f, Screen.height * 0.35f, 0f);
                }

                case 1:
                {
                    // Wave button — sits in lower-center of the HUD (UIToolkit overlay)
                    // Approximated as bottom-center of screen
                    return new Vector3(Screen.width * 0.5f, Screen.height * 0.12f, 0f);
                }

                case 2:
                {
                    // Hero position
                    var hero = LevelRunner.Instance?.Hero;
                    if (hero != null && cam != null)
                        return cam.WorldToScreenPoint(hero.transform.position + Vector3.up * 0.5f);

                    return new Vector3(Screen.width * 0.55f, Screen.height * 0.45f, 0f);
                }

                default:
                    return new Vector3(Screen.width * 0.5f, Screen.height * 0.5f, 0f);
            }
        }

        private IEnumerator Fade(float from, float to, float duration)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                if (_cg != null)
                    _cg.alpha = Mathf.Lerp(from, to, elapsed / duration);
                yield return null;
            }
            if (_cg != null)
                _cg.alpha = to;
        }
    }
}
