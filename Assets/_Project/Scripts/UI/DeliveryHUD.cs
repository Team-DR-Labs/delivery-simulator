using DeliveryBot.CameraSystem;
using DeliveryBot.Delivery;
using DeliveryBot.Vehicle;
using UnityEngine;
using UnityEngine.UI;

namespace DeliveryBot.UI
{
    /// <summary>Order card, score, speedometer, direction arrow, minimap target blip, flash and toast.</summary>
    public sealed class DeliveryHUD : MonoBehaviour
    {
        [SerializeField] private DeliveryManager manager;
        [SerializeField] private Transform robot;
        [SerializeField] private RobotController robotController;
        [SerializeField] private CameraRig cameraRig;

        [Header("Texts")]
        [SerializeField] private Text titleText;
        [SerializeField] private Text infoText;
        [SerializeField] private Text scoreText;
        [SerializeField] private Text speedText;
        [SerializeField] private Text promptText;

        [Header("Widgets")]
        [SerializeField] private RectTransform arrow;
        [SerializeField] private RectTransform minimapBlip;
        [SerializeField] private float minimapRadiusPx = 150f;
        [SerializeField] private float minimapOrthoSize = 45f;
        [SerializeField] private Image flash;
        [SerializeField] private HudToast toast;

        private Color _flashColor;
        private float _flashAlpha;

        private void Awake()
        {
            if (manager == null) manager = FindAnyObjectByType<DeliveryManager>();
            if (robot == null)
            {
                var player = GameObject.FindWithTag("Player");
                robot = player != null ? player.transform : null;
            }
            if (robotController == null && robot != null) robotController = robot.GetComponent<RobotController>();
            if (cameraRig == null) cameraRig = FindAnyObjectByType<CameraRig>();
        }

        private void Update()
        {
            if (manager == null) return;
            UpdateTexts();
            UpdateArrowAndBlip();
            UpdateFlash();
        }

        private void UpdateTexts()
        {
            var name = manager.Target != null ? manager.Target.DisplayName : "-";
            var category = manager.Target != null && !string.IsNullOrEmpty(manager.Target.Category) ? $" ({manager.Target.Category})" : "";
            var title = manager.Phase switch
            {
                DeliveryPhase.ToPickup => $"픽업 → {name}{category}",
                DeliveryPhase.ToDropoff => $"배달 → {name}",
                _ => "주문 대기 중"
            };
            if (titleText != null) titleText.text = title;
            var from = manager.Phase == DeliveryPhase.ToDropoff && manager.Pickup != null ? $"   {manager.Pickup.DisplayName} 주문" : "";
            if (infoText != null) infoText.text = $"{manager.DistanceToTarget:F0} m   {manager.ElapsedThisJob:F0}초{from}";
            UpdatePrompt();
            if (scoreText != null)
                scoreText.text = $"완료 {manager.Completed}건   페널티 {manager.Penalties}회 (+{manager.PenaltyTime:F0}초)   총 {manager.TotalTime:F0}초";
            if (speedText != null)
            {
                var kmh = robotController != null ? Mathf.Abs(robotController.ForwardSpeed) * 3.6f : 0f;
                var view = cameraRig != null && cameraRig.Mode == ViewMode.FirstPerson ? "1인칭" : "3인칭";
                speedText.text = $"{kmh:F0} km/h\n[V] {view}";
            }
        }

        private void UpdatePrompt()
        {
            if (promptText == null) return;
            if (!manager.IsTargetInRange) { promptText.text = ""; return; }
            var action = manager.Phase == DeliveryPhase.ToPickup ? "픽업하기" : "문 앞에 놓기";
            promptText.text = manager.RobotSlowEnough ? $"[A] / [E]  {action}" : "천천히 멈춘 뒤  [A] / [E]";
        }

        private void UpdateArrowAndBlip()
        {
            var hasTarget = robot != null && manager.Target != null;
            if (arrow != null) arrow.gameObject.SetActive(hasTarget);
            if (minimapBlip != null) minimapBlip.gameObject.SetActive(hasTarget);
            if (!hasTarget) return;

            var rel = manager.Target.transform.position - robot.position;
            rel.y = 0f;
            var angle = Vector3.SignedAngle(robot.forward, rel, Vector3.up);
            if (arrow != null) arrow.localRotation = Quaternion.Euler(0f, 0f, -angle);

            if (minimapBlip != null)
            {
                var local = Quaternion.Euler(0f, -robot.eulerAngles.y, 0f) * rel;
                var px = new Vector2(local.x, local.z) * (minimapRadiusPx / Mathf.Max(1f, minimapOrthoSize));
                var limit = minimapRadiusPx - 12f;
                if (px.magnitude > limit) px = px.normalized * limit;
                minimapBlip.anchoredPosition = px;
            }
        }

        private void UpdateFlash()
        {
            if (flash == null) return;
            _flashAlpha = Mathf.MoveTowards(_flashAlpha, 0f, Time.deltaTime * 1.2f);
            flash.color = new Color(_flashColor.r, _flashColor.g, _flashColor.b, _flashAlpha);
            flash.raycastTarget = false;
        }

        public void Flash(Color color)
        {
            _flashColor = color;
            _flashAlpha = color.a;
        }

        public void ShowToast(string message, Color color) => toast?.Show(message, color);
    }
}
