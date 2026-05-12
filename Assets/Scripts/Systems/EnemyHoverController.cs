#nullable enable
using UnityEngine;
using CrowdDefense.Common;
using CrowdDefense.Entities;
using CrowdDefense.UI;

namespace CrowdDefense.Systems
{
    public class EnemyHoverController : MonoSingleton<EnemyHoverController>
    {
        private Camera? cam;
        private Enemy? hoveredEnemy;

        protected override void OnAwakeSingleton() => cam = Camera.main;

        private void Update()
        {
            if (cam == null) return;

            Ray ray = cam.ScreenPointToRay(Input.mousePosition);
            Enemy? found = null;

            if (Physics.Raycast(ray, out RaycastHit hit, 200f))
                found = hit.collider.GetComponentInParent<Enemy>();

            if (found == hoveredEnemy) return;

            hoveredEnemy = found;
            if (hoveredEnemy != null && !hoveredEnemy.IsDead)
                EnemyTooltipController.Instance?.Show(hoveredEnemy);
            else
                EnemyTooltipController.Instance?.Hide();
        }

        private void OnDisable()
        {
            hoveredEnemy = null;
            EnemyTooltipController.Instance?.Hide();
        }
    }
}
