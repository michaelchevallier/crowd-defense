#nullable enable
using UnityEngine;

namespace CrowdDefense.Visual
{
    public class WindSway : MonoBehaviour
    {
        private float _offset;
        private Quaternion _baseRot;

        private void Awake()
        {
            _offset  = Random.Range(0f, Mathf.PI * 2f);
            _baseRot = transform.localRotation;
        }

        private void Update()
        {
            float angle = Mathf.Sin(Time.time * 1.2f + _offset) * 2.5f;
            transform.localRotation = _baseRot * Quaternion.Euler(angle, 0f, angle * 0.5f);
        }
    }
}
