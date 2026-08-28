using DeliveryBot.Delivery;
using UnityEngine;
using UnityEngine.UI;

namespace DeliveryBot.UI
{
    /// <summary>Shows current job, distance, score and a direction arrow toward the target.</summary>
    public sealed class DeliveryHUD : MonoBehaviour
    {
        [SerializeField] private DeliveryManager manager;
        [SerializeField] private Transform robot;
        [SerializeField] private Text statusText;
        [SerializeField] private Text scoreText;
        [SerializeField] private RectTransform arrow;

        private void Awake()
        {
            if (manager == null) manager = FindAnyObjectByType<DeliveryManager>();
            if (robot == null)
            {
                var player = GameObject.FindWithTag("Player");
                robot = player != null ? player.transform : null;
            }
        }

        private void Update()
        {
            if (manager == null) return;
            UpdateText();
            UpdateArrow();
        }

        private void UpdateText()
        {
            var dist = manager.DistanceToTarget;
            var label = manager.Phase switch
            {
                DeliveryPhase.ToPickup => $"픽업하러 가기 → {manager.Target?.DisplayName}",
                DeliveryPhase.ToDropoff => $"배달 중 → {manager.Target?.DisplayName}",
                _ => "대기 중"
            };
            if (statusText != null) statusText.text = $"{label}\n{dist:F0} m   {manager.ElapsedThisJob:F0}초";
            if (scoreText != null) scoreText.text = $"완료 {manager.Completed}건";
        }

        private void UpdateArrow()
        {
            if (arrow == null || robot == null || manager.Target == null)
            {
                if (arrow != null) arrow.gameObject.SetActive(false);
                return;
            }
            arrow.gameObject.SetActive(true);
            var to = manager.Target.transform.position - robot.position;
            to.y = 0f;
            var angle = Vector3.SignedAngle(robot.forward, to, Vector3.up);
            arrow.localRotation = Quaternion.Euler(0f, 0f, -angle);
        }
    }
}
