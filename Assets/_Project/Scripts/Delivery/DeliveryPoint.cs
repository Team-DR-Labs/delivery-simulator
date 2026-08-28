using UnityEngine;

namespace DeliveryBot.Delivery
{
    /// <summary>A location that can serve as pickup or drop-off. Trigger collider + visual marker.</summary>
    [RequireComponent(typeof(SphereCollider))]
    public sealed class DeliveryPoint : MonoBehaviour
    {
        [SerializeField] private string displayName = "Point";
        [SerializeField] private GameObject marker;

        public string DisplayName => displayName;

        public void SetMarkerVisible(bool on)
        {
            if (marker != null) marker.SetActive(on);
        }

        public void SetMarkerColor(Color c)
        {
            if (marker == null) return;
            foreach (var r in marker.GetComponentsInChildren<Renderer>())
            {
                var block = new MaterialPropertyBlock();
                r.GetPropertyBlock(block);
                block.SetColor("_Color", c);
                block.SetColor("_EmissionColor", c);
                r.SetPropertyBlock(block);
            }
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
            col.radius = 3f;
        }
    }
}
