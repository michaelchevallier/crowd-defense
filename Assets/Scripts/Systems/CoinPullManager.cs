#nullable enable
using System.Collections.Generic;
using UnityEngine;

namespace CrowdDefense.Systems
{
    // Registre des sources Magnet actives, mis à jour chaque frame par Tower.UpdateCoinPull().
    // Enemy.TakeDamage() interroge GetCoinMulAt() pour booster la récompense.
    public class CoinPullManager : MonoBehaviour
    {
        public static CoinPullManager? Instance { get; private set; }

        private struct CoinSource
        {
            public Vector3 position;
            public float range;
            public float coinMul;
        }

        // Réinitialisé en fin de frame via LateUpdate, reconstruit par les Magnets dans Update
        private readonly List<CoinSource> sources = new();

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void LateUpdate()
        {
            // Nettoyage en fin de frame : les Magnets re-remplissent au prochain frame
            sources.Clear();
        }

        // Appelé par chaque Tower Magnet dans son Update
        public void RegisterSource(Vector3 pos, float range, float coinMul)
        {
            sources.Add(new CoinSource { position = pos, range = range, coinMul = coinMul });
        }

        // Retourne le multiplicateur le plus élevé parmi les sources en portée (max, pas produit)
        public float GetCoinMulAt(Vector3 pos)
        {
            float best = 1f;
            for (int i = 0; i < sources.Count; i++)
            {
                var s = sources[i];
                float dx = pos.x - s.position.x;
                float dz = pos.z - s.position.z;
                if (dx * dx + dz * dz < s.range * s.range)
                    best = Mathf.Max(best, s.coinMul);
            }
            return best;
        }
    }
}
