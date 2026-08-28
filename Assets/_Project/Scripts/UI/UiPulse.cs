using UnityEngine;

namespace DeliveryBot.UI
{
    /// <summary>Scale pulse for UI blips.</summary>
    public sealed class UiPulse : MonoBehaviour
    {
        [SerializeField] private float amplitude = 0.3f;
        [SerializeField] private float frequency = 3f;

        private void Update()
        {
            transform.localScale = Vector3.one * (1f + Mathf.Sin(Time.time * frequency) * amplitude);
        }
    }
}
