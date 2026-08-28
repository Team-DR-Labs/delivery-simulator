using UnityEngine;
using UnityEngine.UI;

namespace DeliveryBot.Delivery
{
    /// <summary>A storefront that can serve as pickup or drop-off. Trigger collider + marker + name sign.</summary>
    [RequireComponent(typeof(SphereCollider))]
    public sealed class DeliveryPoint : MonoBehaviour
    {
        [SerializeField] private string displayName = "Point";
        [SerializeField] private GameObject marker;
        [SerializeField] private Text signText;

        public string DisplayName => displayName;

        public void Configure(string name, GameObject markerGo, Text sign)
        {
            displayName = name;
            marker = markerGo;
            signText = sign;
            if (signText != null) signText.text = name;
        }

        public void SetMarkerVisible(bool on)
        {
            if (marker != null) marker.SetActive(on);
        }

        public void SetMarkerColor(Color c)
        {
            if (marker == null) return;
            foreach (var r in marker.GetComponentsInChildren<Renderer>(true))
            {
                var block = new MaterialPropertyBlock();
                r.GetPropertyBlock(block);
                block.SetColor("_Color", c);
                block.SetColor("_BaseColor", c);
                block.SetColor("_EmissionColor", c * 0.8f);
                r.SetPropertyBlock(block);
            }
            if (signText != null) signText.color = c;
        }

        private void OnTriggerEnter(Collider other)
        {
            var manager = DeliveryManager.Instance;
            if (manager != null && other.attachedRigidbody != null && other.attachedRigidbody.CompareTag("Player"))
                manager.OnRobotEntered(this);
        }

        private void Reset()
        {
            var col = GetComponent<SphereCollider>();
            col.isTrigger = true;
            col.radius = 3.5f;
        }
    }
}
