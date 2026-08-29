using UnityEngine;
using UnityEngine.UI;

namespace DeliveryBot.Delivery
{
    public enum PointKind { Shop, Home }

    /// <summary>A shop (pickup) or a home/lobby (drop-off). Trigger reports robot in range; marker + name sign.</summary>
    [RequireComponent(typeof(SphereCollider))]
    public sealed class DeliveryPoint : MonoBehaviour
    {
        [SerializeField] private string displayName = "Point";
        [SerializeField] private PointKind kind = PointKind.Shop;
        [SerializeField] private string category = "";
        [SerializeField] private GameObject marker;
        [SerializeField] private Text signText;

        public string DisplayName => displayName;
        public PointKind Kind => kind;
        public string Category => category;

        public void Configure(string name, PointKind pointKind, string categoryName, GameObject markerGo, Text sign)
        {
            displayName = name;
            kind = pointKind;
            category = categoryName;
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

        private static bool IsRobot(Collider other) =>
            other.attachedRigidbody != null && other.attachedRigidbody.CompareTag("Player");

        private void OnTriggerEnter(Collider other)
        {
            if (IsRobot(other)) DeliveryManager.Instance?.SetRobotInRange(this, true);
        }

        private void OnTriggerExit(Collider other)
        {
            if (IsRobot(other)) DeliveryManager.Instance?.SetRobotInRange(this, false);
        }

        private void Reset()
        {
            var col = GetComponent<SphereCollider>();
            col.isTrigger = true;
            col.radius = 6f;
        }
    }
}
