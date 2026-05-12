#nullable enable
using UnityEngine;
using CrowdDefense.Systems;

namespace CrowdDefense.Entities
{
    public class BuildPoint : MonoBehaviour
    {
        public Vector2Int Cell { get; private set; }
        [SerializeField] private float triggerRadius = 0.8f;
        private bool _heroInside;

        public void Init(Vector2Int cell)
        {
            Cell = cell;
            name = $"BuildPoint_{cell.x}_{cell.y}";
        }

        private void Update()
        {
            var hero = Hero.Current;
            if (hero == null) return;
            bool inside = Vector3.Distance(transform.position, hero.transform.position) < triggerRadius;
            if (inside && !_heroInside)
            {
                _heroInside = true;
                PlacementController.Instance?.OpenBuildPointPicker(Cell);
            }
            else if (!inside && _heroInside)
            {
                _heroInside = false;
                PlacementController.Instance?.CloseBuildPointPicker(Cell);
            }
        }
    }
}
