#nullable enable
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using CrowdDefense.Common;
using CrowdDefense.Data;
using CrowdDefense.Entities;
using CrowdDefense.UI;
using CrowdDefense.Visual;

namespace CrowdDefense.Systems
{
    [DefaultExecutionOrder(-40)]
    public class WaveRewardSpawner : MonoSingleton<WaveRewardSpawner>
    {
        private const int   ChestGold          = 100;
        private const float AutoPickupRadius   = 1.5f;
        private const float SpawnScatter       = 2f;
        private const int   PerkOfferCount     = 3;

        private GameObject? _activeChest;
        private Coroutine?  _sparkleLoop;
        private Canvas?     _pickerCanvas;

        protected override void OnAwakeSingleton()
        {
            if (transform.parent != null) transform.SetParent(null);
        }

        private void Start()
        {
            if (WaveManager.Instance != null)
                WaveManager.Instance.OnWaveCleared += OnWaveCleared;
        }

        protected override void OnDestroySingleton()
        {
            if (WaveManager.Instance != null)
                WaveManager.Instance.OnWaveCleared -= OnWaveCleared;
        }

        private void OnWaveCleared(int _waveIdx)
        {
            DespawnChest();

            Vector3 castlePos = Castle.Instance != null
                ? Castle.Instance.transform.position
                : Vector3.zero;
            Vector3 burstPos = castlePos + Vector3.up * 1.5f;

            VfxPool.Instance?.SpawnConfetti(burstPos, 2.0f);
            FloatingPopupController.Instance?.SpawnReward($"+{ChestGold}g coffre", burstPos, new Color(1f, 0.88f, 0.15f));
            AudioController.Instance?.Play("chest_open", 0.9f);

            SpawnChest();
        }

        // ── Chest spawn / despawn ─────────────────────────────────────────────

        private void SpawnChest()
        {
            Vector3 castlePos = Castle.Instance != null
                ? Castle.Instance.transform.position
                : Vector3.zero;

            Vector2 scatter  = Random.insideUnitCircle * SpawnScatter;
            Vector3 spawnPos = castlePos + new Vector3(scatter.x, 0f, scatter.y);

            _activeChest = GameObject.CreatePrimitive(PrimitiveType.Cube);
            _activeChest.name = "RewardChest";
            _activeChest.transform.position = spawnPos;
            _activeChest.transform.localScale = Vector3.one * 0.7f;

            // Golden emissive material
            var rend = _activeChest.GetComponent<MeshRenderer>();
            var mat  = BuildGoldenMaterial();
            rend.material = mat;

            // Remove physics collider, add trigger for auto-pickup
            var col = _activeChest.GetComponent<BoxCollider>();
            if (col != null) col.isTrigger = true;

            // Coin-burst sparkle loop
            _sparkleLoop = StartCoroutine(SparkleLoop(spawnPos));

            // Click detection component
            _activeChest.AddComponent<ChestClickReceiver>().Init(this);
        }

        private void DespawnChest()
        {
            if (_sparkleLoop != null) { StopCoroutine(_sparkleLoop); _sparkleLoop = null; }
            if (_activeChest != null) { Destroy(_activeChest); _activeChest = null; }
        }

        // ── Sparkle loop ──────────────────────────────────────────────────────

        private IEnumerator SparkleLoop(Vector3 pos)
        {
            var wait = new WaitForSeconds(0.45f);
            while (_activeChest != null)
            {
                VfxPool.Instance?.SpawnCoinBurst(pos + Vector3.up * 0.4f);
                yield return wait;
            }
        }

        // ── Pickup logic ──────────────────────────────────────────────────────

        private void Update()
        {
            if (_activeChest == null) return;
            var hero = LevelRunner.Instance?.Hero;
            if (hero == null) return;
            if ((hero.transform.position - _activeChest.transform.position).sqrMagnitude < AutoPickupRadius * AutoPickupRadius) AutoCollect();
        }

        // Called by ChestClickReceiver
        public void OnChestClicked()
        {
            if (_activeChest == null) return;
            CollectWithPicker();
        }

        private void AutoCollect()
        {
            if (_activeChest == null) return;
            Vector3 pos = _activeChest.transform.position;
            DespawnChest();
            GrantGold(pos);
            // Auto-collect: apply random perk directly, no pause
            ApplyRandomPerk();
        }

        private void CollectWithPicker()
        {
            if (_activeChest == null) return;
            Vector3 pos = _activeChest.transform.position;
            DespawnChest();
            GrantGold(pos);
            OpenPerkPicker();
        }

        // ── Gold grant ────────────────────────────────────────────────────────

        private static void GrantGold(Vector3 worldPos)
        {
            Economy.Instance?.AddGold(ChestGold);
            FloatingPopupController.Instance?.SpawnCoin(ChestGold, worldPos + Vector3.up);
            VfxPool.Instance?.SpawnCoinBurst(worldPos + Vector3.up * 0.5f);
            AudioController.Instance?.Play("coin_pickup", 0.8f);
        }

        // ── Random perk (auto-collect path) ──────────────────────────────────

        private static void ApplyRandomPerk()
        {
            var hero = LevelRunner.Instance?.Hero;
            if (hero == null) return;
            var reg = PerkRegistry.Get();
            if (reg == null) return;
            var offers = reg.GetRandom(1);
            if (offers.Count == 0) return;
            PerkSystem.Instance?.ApplyPerk(hero, offers[0]);
            SaveSystem.AppendRunPerk(offers[0].id);
        }

        // ── Inline perk picker (UGUI Canvas) ─────────────────────────────────

        private void OpenPerkPicker()
        {
            var reg  = PerkRegistry.Get();
            var hero = LevelRunner.Instance?.Hero;
            if (reg == null || hero == null) { return; }

            var offers = reg.GetRandom(PerkOfferCount);
            if (offers.Count == 0) return;

            Time.timeScale = 0f;
            _pickerCanvas  = BuildPickerCanvas(offers, hero);
        }

        private Canvas BuildPickerCanvas(List<PerkDef> offers, Hero hero)
        {
            var go = new GameObject("WaveReward_PerkPicker");
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 200;
            go.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            go.AddComponent<GraphicRaycaster>();

            // Semi-transparent backdrop
            var bg = new GameObject("BG");
            bg.transform.SetParent(go.transform, false);
            var bgImg = bg.AddComponent<Image>();
            bgImg.color = new Color(0f, 0f, 0f, 0.65f);
            var bgRect = bg.GetComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.offsetMin = bgRect.offsetMax = Vector2.zero;

            // Title label
            var titleGo = new GameObject("Title");
            titleGo.transform.SetParent(go.transform, false);
            var title = titleGo.AddComponent<Text>();
            title.text      = "Coffre de vague — choisissez un perk";
            title.alignment = TextAnchor.UpperCenter;
            title.fontSize  = 28;
            title.color     = new Color(1f, 0.88f, 0.15f);
            title.font      = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var titleRect = titleGo.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0.1f, 0.72f);
            titleRect.anchorMax = new Vector2(0.9f, 0.85f);
            titleRect.offsetMin = titleRect.offsetMax = Vector2.zero;

            // Card layout: 3 cards spread horizontally
            float[] xAnchors = { 0.08f, 0.38f, 0.68f };
            for (int i = 0; i < offers.Count && i < xAnchors.Length; i++)
            {
                BuildCard(go.transform, offers[i], hero, xAnchors[i]);
            }

            return canvas;
        }

        private void BuildCard(Transform parent, PerkDef def, Hero hero, float xAnchorMin)
        {
            const float cardW = 0.28f;

            var cardGo = new GameObject($"Card_{def.id}");
            cardGo.transform.SetParent(parent, false);
            var cardImg = cardGo.AddComponent<Image>();
            cardImg.color = new Color(0.12f, 0.10f, 0.18f, 0.95f);
            var cardRect = cardGo.GetComponent<RectTransform>();
            cardRect.anchorMin = new Vector2(xAnchorMin, 0.28f);
            cardRect.anchorMax = new Vector2(xAnchorMin + cardW, 0.70f);
            cardRect.offsetMin = cardRect.offsetMax = Vector2.zero;

            var btn = cardGo.AddComponent<Button>();
            var capturedDef = def;
            btn.onClick.AddListener(() => OnPerkPicked(capturedDef, hero));

            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            // Rarity stripe
            var stripeGo = new GameObject("Stripe");
            stripeGo.transform.SetParent(cardGo.transform, false);
            var stripeImg = stripeGo.AddComponent<Image>();
            stripeImg.color = RarityColor(def.rarity);
            var stripeRect = stripeGo.GetComponent<RectTransform>();
            stripeRect.anchorMin = new Vector2(0f, 0.88f);
            stripeRect.anchorMax = Vector2.one;
            stripeRect.offsetMin = stripeRect.offsetMax = Vector2.zero;

            AddLabel(cardGo.transform, def.nameKey,  new Vector2(0f, 0.66f), new Vector2(1f, 0.86f), 20, Color.white,        TextAnchor.MiddleCenter, font);
            AddLabel(cardGo.transform, def.descKey,  new Vector2(0f, 0.22f), new Vector2(1f, 0.64f), 14, new Color(0.85f, 0.85f, 0.85f), TextAnchor.MiddleCenter, font);
            AddLabel(cardGo.transform, "Choisir",    new Vector2(0.1f, 0.04f), new Vector2(0.9f, 0.20f), 16, new Color(1f, 0.88f, 0.15f), TextAnchor.MiddleCenter, font);
        }

        private static void AddLabel(Transform parent, string text, Vector2 anchorMin, Vector2 anchorMax,
                                     int fontSize, Color color, TextAnchor align, Font font)
        {
            var go = new GameObject("Label");
            go.transform.SetParent(parent, false);
            var lbl = go.AddComponent<Text>();
            lbl.text      = text;
            lbl.fontSize  = fontSize;
            lbl.color     = color;
            lbl.alignment = align;
            lbl.font      = font;
            lbl.horizontalOverflow = HorizontalWrapMode.Wrap;
            lbl.verticalOverflow   = VerticalWrapMode.Overflow;
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }

        private void OnPerkPicked(PerkDef def, Hero hero)
        {
            Time.timeScale = 1f;
            if (_pickerCanvas != null) { Destroy(_pickerCanvas.gameObject); _pickerCanvas = null; }

            PerkSystem.Instance?.ApplyPerk(hero, def);
            SaveSystem.AppendRunPerk(def.id);
            VfxPool.Instance?.SpawnPerkPickup(hero.transform.position + Vector3.up, RarityColor(def.rarity));
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private static Material BuildGoldenMaterial()
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit")
                      ?? Shader.Find("Standard");
            var mat = new Material(shader ?? Shader.Find("Hidden/InternalErrorShader")!);
            mat.color = new Color(1f, 0.85f, 0.1f);
            // Emission for glow
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", new Color(1f, 0.5f, 0f, 1f) * 2f);
            mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            return mat;
        }

        private static Color RarityColor(PerkRarity rarity) => rarity switch
        {
            PerkRarity.Common    => new Color(0.75f, 0.75f, 0.75f),
            PerkRarity.Uncommon  => new Color(0.25f, 0.85f, 0.25f),
            PerkRarity.Rare      => new Color(0.25f, 0.5f,  1f),
            PerkRarity.Epic      => new Color(0.65f, 0.25f, 1f),
            PerkRarity.Legendary => new Color(1f,    0.65f, 0.1f),
            _ => Color.white
        };

        // ── Inner component ───────────────────────────────────────────────────

        private class ChestClickReceiver : MonoBehaviour
        {
            private WaveRewardSpawner? _spawner;

            public void Init(WaveRewardSpawner spawner) => _spawner = spawner;

            private void OnMouseDown() => _spawner?.OnChestClicked();
        }
    }
}
