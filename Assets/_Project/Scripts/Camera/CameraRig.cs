using DeliveryBot.Input;
using DeliveryBot.Vehicle;
using UnityEngine;

namespace DeliveryBot.CameraSystem
{
    public enum ViewMode { ThirdPerson, FirstPerson }

    /// <summary>
    /// Single camera that follows the robot in third person (default) or sits on its head (V toggles).
    /// Lives at scene root so third-person smoothing is independent of the robot's rotation.
    /// </summary>
    public sealed class CameraRig : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private RobotController robot;
        [SerializeField] private DriveInputProvider input;
        [SerializeField] private ViewMode mode = ViewMode.ThirdPerson;

        [Header("Third person")]
        [SerializeField] private Vector3 thirdPersonOffset = new Vector3(0f, 2.4f, -5f);
        [SerializeField] private Vector3 lookAtOffset = new Vector3(0f, 1.0f, 1.5f);
        [SerializeField] private float extraPullbackAtMaxSpeed = 1.2f;
        [SerializeField] private float positionSmoothTime = 0.15f;
        [SerializeField] private float rotationLerp = 8f;
        [SerializeField] private float collisionRadius = 0.3f;
        [SerializeField] private LayerMask collisionMask = ~0;

        [Header("First person")]
        [SerializeField] private Vector3 firstPersonOffset = new Vector3(0f, 1.25f, 0.55f);
        [SerializeField] private float leanDegrees = 3f;
        [SerializeField] private float bobAmplitude = 0.02f;
        [SerializeField] private float bobFrequency = 6f;

        private Vector3 _velocity;
        private float _lean;
        private float _shake;

        public ViewMode Mode => mode;

        private void Awake()
        {
            if (target != null && robot == null) robot = target.GetComponent<RobotController>();
            if (input == null && target != null) input = target.GetComponent<DriveInputProvider>();
        }

        private void LateUpdate()
        {
            if (target == null) return;
            if (input != null && input.Current.ToggleView)
                mode = mode == ViewMode.ThirdPerson ? ViewMode.FirstPerson : ViewMode.ThirdPerson;

            if (mode == ViewMode.ThirdPerson) UpdateThirdPerson();
            else UpdateFirstPerson();

            ApplyShake();
        }

        private float Speed01 => robot != null ? Mathf.Clamp01(Mathf.Abs(robot.ForwardSpeed) / Mathf.Max(0.01f, robot.MaxForwardSpeed)) : 0f;

        private void UpdateThirdPerson()
        {
            var offset = thirdPersonOffset + Vector3.back * (extraPullbackAtMaxSpeed * Speed01);
            var desired = target.TransformPoint(offset);
            var pivot = target.TransformPoint(lookAtOffset);

            // Pull the camera in when a wall is between the robot and the desired position.
            var dir = desired - pivot;
            if (Physics.SphereCast(pivot, collisionRadius, dir.normalized, out var hit, dir.magnitude, collisionMask, QueryTriggerInteraction.Ignore)
                && !hit.transform.IsChildOf(target))
                desired = pivot + dir.normalized * Mathf.Max(0.5f, hit.distance - 0.1f);

            transform.position = Vector3.SmoothDamp(transform.position, desired, ref _velocity, positionSmoothTime);
            var lookRot = Quaternion.LookRotation(pivot - transform.position, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRot, Time.deltaTime * rotationLerp);
        }

        private void UpdateFirstPerson()
        {
            var steer = input != null ? input.Current.Steer : 0f;
            var speed01 = Speed01;
            _lean = Mathf.Lerp(_lean, -steer * leanDegrees * speed01, Time.deltaTime * 8f);
            var bob = Mathf.Sin(Time.time * bobFrequency * Mathf.Max(0.2f, speed01)) * bobAmplitude * speed01;

            transform.position = target.TransformPoint(firstPersonOffset + Vector3.up * bob);
            transform.rotation = target.rotation * Quaternion.Euler(0f, 0f, _lean);
            _velocity = Vector3.zero;
        }

        private void ApplyShake()
        {
            if (_shake <= 0f) return;
            transform.position += Random.insideUnitSphere * _shake * 0.15f;
            _shake = Mathf.MoveTowards(_shake, 0f, Time.deltaTime * 2f);
        }

        public void Shake(float strength) => _shake = Mathf.Max(_shake, strength);

        public void SetTarget(Transform t)
        {
            target = t;
            robot = t != null ? t.GetComponent<RobotController>() : null;
            input = t != null ? t.GetComponent<DriveInputProvider>() : null;
        }
    }
}
