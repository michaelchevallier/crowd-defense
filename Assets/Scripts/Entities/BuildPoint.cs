#nullable enable
using UnityEngine;
using CrowdDefense.Systems;

namespace CrowdDefense.Entities
{
    public class BuildPoint : MonoBehaviour
    {
        public Vector2Int Cell { get; private set; }
        [SerializeField] private float triggerRadius = 1.4f;
        private bool _heroInside;
        private float _pulseTime = 0f;
        private MeshRenderer? _cachedRend;
        private MaterialPropertyBlock? _mpb;

        public void Init(Vector2Int cell)
        {
            Cell = cell;
            name = $"BuildPoint_{cell.x}_{cell.y}";
        }

        private void Update()
        {
            _pulseTime += Time.deltaTime;
            float pulse = 1f + Mathf.Sin(_pulseTime * 4f) * 0.075f;
            transform.localScale = new Vector3(1.6f * pulse, 0.15f, 1.6f * pulse);

            var hero = Hero.Current;
            if (hero == null) return;
            bool inside = Vector3.Distance(transform.position, hero.transform.position) < triggerRadius;
            if (inside && !_heroInside)
            {
                _heroInside = true;
                UpdateColor(new Color(0.4f, 1f, 0.4f, 0.85f));
                PlacementController.Instance?.OpenBuildPointPicker(Cell);
            }
            else if (!inside && _heroInside)
            {
                _heroInside = false;
                UpdateColor(new Color(1f, 0.85f, 0.1f, 0.75f));
                PlacementController.Instance?.CloseBuildPointPicker(Cell);
            }
        }

        private void UpdateColor(Color c)
        {
            if (_cachedRend == null) _cachedRend = GetComponent<MeshRenderer>();
            if (_cachedRend == null) return;
            if (_mpb == null) _mpb = new MaterialPropertyBlock();
            _cachedRend.GetPropertyBlock(_mpb);
            _mpb.SetColor("_BaseColor", c);
            _cachedRend.SetPropertyBlock(_mpb);
        }
    }
}
