using UnityEngine;

namespace DeliveryBot.Vehicle
{
    /// <summary>
    /// Camera mounted on the robot's "head". Adds a small lean into turns and a speed bob
    /// so the player feels like the robot rather than a floating camera.
    /// </summary>
    public sealed class FirstPersonCamera : MonoBehaviour
    {
        [SerializeField] private RobotController robot;
        [SerializeField] private DeliveryBot.Input.DriveInputProvider input;
        [SerializeField] private float leanDegrees = 3f;
        [SerializeField] private float bobAmplitude = 0.02f;
        [SerializeField] private float bobFrequency = 6f;
        [SerializeField] private float smoothing = 8f;

        private Vector3 _restLocalPos;
        private float _lean;

        private void Awake()
        {
            _restLocalPos = transform.localPosition;
            if (robot == null) robot = GetComponentInParent<RobotController>();
            if (input == null) input = FindAnyObjectByType<DeliveryBot.Input.DriveInputProvider>();
        }

        private void LateUpdate()
        {
            var steer = input != null ? input.Current.Steer : 0f;
            var speed01 = robot != null ? Mathf.Abs(robot.ForwardSpeed) / Mathf.Max(0.01f, robot.MaxForwardSpeed) : 0f;

            _lean = Mathf.Lerp(_lean, -steer * leanDegrees * speed01, Time.deltaTime * smoothing);
            transform.localRotation = Quaternion.Euler(0f, 0f, _lean);

            var bob = Mathf.Sin(Time.time * bobFrequency * Mathf.Max(0.2f, speed01)) * bobAmplitude * speed01;
            transform.localPosition = _restLocalPos + Vector3.up * bob;
        }
    }
}
