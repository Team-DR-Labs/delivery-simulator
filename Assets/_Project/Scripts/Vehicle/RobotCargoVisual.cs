using DeliveryBot.Delivery;
using UnityEngine;

namespace DeliveryBot.Vehicle
{
    /// <summary>Shows the parcel on the roof while a delivery is in progress.</summary>
    public sealed class RobotCargoVisual : MonoBehaviour
    {
        [SerializeField] private GameObject cargo;

        private void Start()
        {
            var manager = DeliveryManager.Instance;
            if (manager == null) return;
            manager.PhaseChanged += OnPhaseChanged;
            OnPhaseChanged(manager.Phase, manager.Target);
        }

        private void OnDestroy()
        {
            if (DeliveryManager.Instance != null) DeliveryManager.Instance.PhaseChanged -= OnPhaseChanged;
        }

        private void OnPhaseChanged(DeliveryPhase phase, DeliveryPoint _)
        {
            if (cargo != null) cargo.SetActive(phase == DeliveryPhase.ToDropoff);
        }
    }
}
