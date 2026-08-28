using DeliveryBot.Input;
using UnityEngine;

namespace DeliveryBot.Vehicle
{
    /// <summary>Spins wheel meshes with speed and yaws the front wheels with steering. Visual only.</summary>
    public sealed class WheelVisuals : MonoBehaviour
    {
        [SerializeField] private RobotController robot;
        [SerializeField] private DriveInputProvider input;
        [SerializeField] private Transform[] steerPivots;
        [SerializeField] private Transform[] spinPivots;
        [SerializeField] private float wheelRadius = 0.35f;
        [SerializeField] private float maxSteerAngle = 28f;
        [SerializeField] private float steerSmoothing = 10f;

        private float _steerAngle;

        private void Awake()
        {
            if (robot == null) robot = GetComponentInParent<RobotController>();
            if (input == null) input = GetComponentInParent<DriveInputProvider>();
        }

        private void Update()
        {
            var speed = robot != null ? robot.ForwardSpeed : 0f;
            var degrees = speed / Mathf.Max(0.01f, wheelRadius) * Mathf.Rad2Deg * Time.deltaTime;
            foreach (var t in spinPivots) t.Rotate(degrees, 0f, 0f, Space.Self);

            var target = (input != null ? input.Current.Steer : 0f) * maxSteerAngle;
            _steerAngle = Mathf.Lerp(_steerAngle, target, Time.deltaTime * steerSmoothing);
            foreach (var t in steerPivots) t.localRotation = Quaternion.Euler(0f, _steerAngle, 0f);
        }

        public void Configure(Transform[] steer, Transform[] spin, float radius)
        {
            steerPivots = steer;
            spinPivots = spin;
            wheelRadius = radius;
        }
    }
}
