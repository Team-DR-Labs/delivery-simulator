using UnityEngine;

namespace DeliveryBot.Minimap
{
    /// <summary>Top-down orthographic camera that tracks the robot and renders into a RenderTexture.</summary>
    [RequireComponent(typeof(Camera))]
    public sealed class MinimapFollow : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private float height = 60f;
        [Tooltip("If true the map rotates so the robot always faces up")]
        [SerializeField] private bool rotateWithTarget = true;

        private void LateUpdate()
        {
            if (target == null) return;
            var p = target.position;
            transform.position = new Vector3(p.x, p.y + height, p.z);
            var yaw = rotateWithTarget ? target.eulerAngles.y : 0f;
            transform.rotation = Quaternion.Euler(90f, yaw, 0f);
        }

        public void SetTarget(Transform t) => target = t;
    }
}
