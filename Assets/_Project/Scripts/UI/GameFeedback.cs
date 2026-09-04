using DeliveryBot.Audio;
using DeliveryBot.CameraSystem;
using DeliveryBot.Delivery;
using DeliveryBot.Vehicle;
using UnityEngine;

namespace DeliveryBot.UI
{
    /// <summary>Turns game events into sound, particles, camera shake and HUD messages.</summary>
    public sealed class GameFeedback : MonoBehaviour
    {
        [SerializeField] private DeliveryManager manager;
        [SerializeField] private DeliveryHUD hud;
        [SerializeField] private CameraRig cameraRig;
        [SerializeField] private Transform robot;
        [SerializeField] private GameFlow flow;

        private void Start()
        {
            if (manager == null) manager = DeliveryManager.Instance;
            if (flow == null) flow = GameFlow.Instance;
            if (flow != null) flow.StateChanged += OnFlowState;
            if (manager == null) return;
            manager.PhaseChanged += OnPhaseChanged;
            manager.DeliveryCompleted += OnDelivered;
            manager.PenaltyAdded += OnPenalty;
        }

        private void OnDestroy()
        {
            if (flow != null) flow.StateChanged -= OnFlowState;
            if (manager == null) return;
            manager.PhaseChanged -= OnPhaseChanged;
            manager.DeliveryCompleted -= OnDelivered;
            manager.PenaltyAdded -= OnPenalty;
        }

        private void OnFlowState(FlowState state)
        {
            if (state == FlowState.Playing)
                hud?.ShowToast($"{flow.Nickname} 님, {flow.RoundSeconds:F0}초 시작!", Color.white);
            else if (state == FlowState.Results)
            {
                SfxPlayer.Instance?.PlayDelivered();
                hud?.ShowToast("시간 종료!", new Color(1f, 0.85f, 0.3f));
            }
        }

        private void OnPhaseChanged(DeliveryPhase phase, DeliveryPoint target)
        {
            if (phase == DeliveryPhase.ToDropoff)
            {
                SfxPlayer.Instance?.PlayPickup();
                hud?.ShowToast($"픽업 완료! {target?.DisplayName} 으로 배달하세요", new Color(0.4f, 0.9f, 1f));
            }
            else if (phase == DeliveryPhase.ToPickup && manager.Completed == 0 && manager.TotalTime < 2f)
            {
                hud?.ShowToast($"새 주문: {target?.DisplayName} 에서 픽업 (도착 후 A/E)", Color.white);
            }
        }

        private void OnDelivered(int count, float seconds)
        {
            SfxPlayer.Instance?.PlayDelivered();
            if (robot != null) ConfettiFactory.Burst(robot.position + Vector3.up * 1.5f);
            hud?.ShowToast($"배달 완료! ({seconds:F0}초)  총 {count}건", new Color(1f, 0.85f, 0.3f));
        }

        private void OnPenalty(float seconds, string reason)
        {
            SfxPlayer.Instance?.PlayBump();
            cameraRig?.Shake(1f);
            hud?.Flash(new Color(1f, 0.1f, 0.1f, 0.35f));
            var sign = flow != null ? "-" : "+";
            hud?.ShowToast($"{reason}! {sign}{seconds:F0}초", new Color(1f, 0.35f, 0.3f));
        }
    }
}
