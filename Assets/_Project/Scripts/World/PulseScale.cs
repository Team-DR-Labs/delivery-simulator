using UnityEngine;

namespace DeliveryBot.World
{
    /// <summary>Gentle breathing scale animation for markers and lamps.</summary>
    public sealed class PulseScale : MonoBehaviour
    {
        [SerializeField] private float amplitude = 0.15f;
        [SerializeField] private float frequency = 2.5f;
        [SerializeField] private bool xz = true;

        private Vector3 _base;

        private void Awake() => _base = transform.localScale;

        private void Update()
        {
            var s = 1f + Mathf.Sin(Time.time * frequency) * amplitude;
            transform.localScale = xz ? new Vector3(_base.x * s, _base.y, _base.z * s) : _base * s;
        }
    }
}
