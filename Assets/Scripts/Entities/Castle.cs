#nullable enable
using System;
using System.Collections;
using UnityEngine;
using TMPro;
using CrowdDefense.Common;
using CrowdDefense.Data;
using CrowdDefense.Systems;
using CrowdDefense.Visual;

namespace CrowdDefense.Entities
{
    public partial class Castle : MonoSingleton<Castle>
    {
        public int HP { get; private set; }
        public int HPMax { get; private set; }
        public bool IsDead => HP <= 0;
        public bool WasHitThisWave { get; private set; }

        public event Action<int, int>? OnHPChanged;
        public event Action<Castle>?   OnCastleDied;

        protected int _world = 1;
        protected MeshFilter? _meshFilter;
        protected ParticleSystem? _smokePs;
        protected Light? _castleAura;
        protected Transform? _gateDoor;
        protected Coroutine? _gateCoroutine;
        protected Light? _dangerLight;
        protected bool _smokeActive;
        protected Coroutine? _smokeCoroutine;
        protected Coroutine? _sparksCoroutine;
        protected bool _grayscaleApplied;
        protected ParticleSystem? _firePs;
        protected bool _firePsSpawned;
        protected readonly ParticleSystem?[] _candlePs = new ParticleSystem?[4];

        public void InitWithFormula(int world, int level = 1)
        {
            int hp = BalanceConfig.GetCastleMaxHp(world, level);
            Init(hp, world);
        }

        public void Init(int hp, int world = 1)
        {
            HP = HPMax = Systems.SaveSystem.IsHardcoreRun ? 1 : hp;
            _world = world;
            _meshFilter = GetComponentInChildren<MeshFilter>();
            _smokePs    = GetComponentInChildren<ParticleSystem>();

            if (PathManager.Instance?.Grid != null)
            {
                var grid = PathManager.Instance.Grid;
                if (grid.Castles.Count > 0)
                {
                    var cell = grid.Castles[0];
                    transform.position = GridCoords.CellToWorld(cell.x, cell.y, grid.Width, grid.Height, grid.CellSize) + Vector3.up * 0.5f;
                }
            }

            BuildHpBar();
            SpawnCandleParticles();
            BuildGate();
            BuildCastleAura();
            SubscribeWaveEvents();
            OnHPChanged?.Invoke(HP, HPMax);
        }

        public void SpawnVictoryBanner()
            => StartCoroutine(VictoryBannerCoroutine());

        private IEnumerator VictoryBannerCoroutine()
        {
            var go = new GameObject("VictoryBanner");
            go.transform.position = transform.position + Vector3.up * 2.5f;

            var tmp = go.AddComponent<TextMeshPro>();
            tmp.text             = "Victoire !";
            tmp.fontSize         = 7f;
            tmp.fontStyle        = FontStyles.Bold;
            tmp.alignment        = TextAlignmentOptions.Center;
            tmp.color            = new Color(1f, 0.92f, 0.15f, 1f);
            tmp.outlineWidth     = 0.25f;
            tmp.outlineColor     = new Color32(80, 40, 0, 255);
            tmp.enableWordWrapping   = false;
            tmp.rectTransform.sizeDelta = new Vector2(8f, 2f);

            var cam        = Camera.main;
            const float punchDur  = 0.25f;
            const float totalDur  = 2.0f;
            const float riseUnits = 3.0f;

            float elapsed = 0f;
            while (elapsed < punchDur)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / punchDur);
                float s = t < 0.5f ? Mathf.Lerp(0f, 1.4f, t * 2f) : Mathf.Lerp(1.4f, 1f, (t - 0.5f) * 2f);
                go.transform.localScale = Vector3.one * s;
                if (cam != null) go.transform.rotation = cam.transform.rotation;
                yield return null;
            }
            go.transform.localScale = Vector3.one;

            Vector3 startPos  = go.transform.position;
            float   remaining = totalDur - punchDur;
            elapsed = 0f;
            while (elapsed < remaining)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / remaining);
                go.transform.position = startPos + Vector3.up * (riseUnits * t);
                var c = tmp.color;
                c.a = 1f - t;
                tmp.color = c;
                if (cam != null) go.transform.rotation = cam.transform.rotation;
                yield return null;
            }

            Destroy(go);
        }

        protected void BuildGate()
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = "CastleGate";
            Destroy(go.GetComponent<BoxCollider>());
            go.transform.SetParent(transform, false);
            go.transform.localPosition = new Vector3(0f, 0.5f, 0.55f);
            go.transform.localScale    = new Vector3(0.7f, 0.9f, 0.08f);
            var rend = go.GetComponent<MeshRenderer>();
            rend.material = BuildUnlitMaterial(new Color(0.29f, 0.17f, 0.04f, 1f), transparent: false);
            _gateDoor = go.transform;
        }

        protected void SubscribeWaveEvents()
        {
            var wm = WaveManager.Instance;
            if (wm == null) return;
            wm.OnWaveStart   += _ => { WasHitThisWave = false; AnimateGate(open: true); };
            wm.OnWaveCleared += _ => AnimateGate(open: false);
        }

        protected void AnimateGate(bool open)
        {
            if (_gateDoor == null) return;
            if (_gateCoroutine != null) StopCoroutine(_gateCoroutine);
            _gateCoroutine = StartCoroutine(open ? OpenGateCoroutine() : CloseGateCoroutine());
        }

        private IEnumerator OpenGateCoroutine()
        {
            const float dur = 0.5f;
            float startY = NormalizeAngle(_gateDoor!.localEulerAngles.y);
            float t = 0f;
            while (t < dur)
            {
                t += Time.deltaTime;
                float y = Mathf.LerpAngle(startY, 90f, Mathf.Clamp01(t / dur));
                _gateDoor.localEulerAngles = new Vector3(0f, y, 0f);
                yield return null;
            }
            _gateDoor.localEulerAngles = new Vector3(0f, 90f, 0f);
            _gateCoroutine = null;
        }

        private IEnumerator CloseGateCoroutine()
        {
            const float dur = 0.5f;
            float startY = NormalizeAngle(_gateDoor!.localEulerAngles.y);
            float t = 0f;
            while (t < dur)
            {
                t += Time.deltaTime;
                float y = Mathf.LerpAngle(startY, 0f, Mathf.Clamp01(t / dur));
                _gateDoor.localEulerAngles = new Vector3(0f, y, 0f);
                yield return null;
            }
            _gateDoor.localEulerAngles = new Vector3(0f, 0f, 0f);
            _gateCoroutine = null;
        }

        private static float NormalizeAngle(float deg)
        {
            if (deg > 180f) deg -= 360f;
            return deg;
        }

        protected static Material BuildUnlitMaterial(Color color, bool transparent)
        {
            var shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color");
            var mat = new Material(shader ?? Shader.Find("Standard")!);
            mat.color = color;
            if (transparent)
            {
                mat.SetFloat("_Surface", 1f);
                mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                mat.SetInt("_ZWrite", 0);
                mat.renderQueue = 3000;
            }
            return mat;
        }

        protected void Update()
        {
            UpdateDangerLightFlicker();
        }

        protected void UpdateDangerLightFlicker()
        {
            if (_dangerLight == null) return;
            _dangerLightPhase += Time.deltaTime * 3.5f;
            float sin   = Mathf.Sin(_dangerLightPhase) * 0.5f + 0.5f;
            float ratio = HPMax > 0 ? (float)HP / HPMax : 0f;
            float baseI = ratio <= 0.15f ? 3.5f : 1.5f;
            _dangerLight.intensity = baseI + baseI * sin;
        }

        protected float _dangerLightPhase;
    }
}
