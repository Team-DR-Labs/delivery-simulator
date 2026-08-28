using DeliveryBot.Input;
using UnityEngine;

namespace DeliveryBot.Vehicle
{
    /// <summary>
    /// Arcade-style drive for a slow delivery robot. No WheelColliders: velocity is set
    /// directly along the robot's forward axis and yaw is rotated by steering input,
    /// which is stable, predictable and cheap. Gravity/vertical velocity is preserved.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public sealed class RobotController : MonoBehaviour
    {
        [SerializeField] private DriveInputProvider input;

        [Header("Speed (m/s)")]
        [SerializeField] private float maxForwardSpeed = 6f;
        [SerializeField] private float maxReverseSpeed = 2.5f;
        [SerializeField] private float acceleration = 4f;
        [SerializeField] private float brakeDeceleration = 10f;
        [SerializeField] private float coastDeceleration = 2f;

        [Header("Steering")]
        [SerializeField] private float turnRateDegPerSec = 90f;
        [Tooltip("Steering is scaled by speed so the robot cannot spin in place")]
        [SerializeField] private float minSpeedForFullTurn = 1.5f;

        [Header("Ground")]
        [SerializeField] private float groundCheckDistance = 0.4f;
        [SerializeField] private LayerMask groundMask = ~0;

        private Rigidbody _rb;

        public float ForwardSpeed { get; private set; }
        public bool IsGrounded { get; private set; }
        public float MaxForwardSpeed => maxForwardSpeed;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody>();
            _rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
            _rb.interpolation = RigidbodyInterpolation.Interpolate;
            if (input == null) input = FindAnyObjectByType<DriveInputProvider>();
        }

        private void FixedUpdate()
        {
            var state = input != null ? input.Current : DriveInputState.None;
            IsGrounded = Physics.Raycast(transform.position + Vector3.up * 0.1f, Vector3.down, groundCheckDistance + 0.1f, groundMask, QueryTriggerInteraction.Ignore);

            ForwardSpeed = Vector3.Dot(_rb.linearVelocity, transform.forward);
            var newSpeed = ComputeNextSpeed(ForwardSpeed, state, Time.fixedDeltaTime);

            if (IsGrounded)
            {
                var vertical = Vector3.Dot(_rb.linearVelocity, Vector3.up);
                _rb.linearVelocity = transform.forward * newSpeed + Vector3.up * vertical;
                ApplySteering(state.Steer, newSpeed, Time.fixedDeltaTime);
            }
        }

        private float ComputeNextSpeed(float current, DriveInputState s, float dt)
        {
            if (s.Handbrake) return Mathf.MoveTowards(current, 0f, brakeDeceleration * dt);

            var target = s.Reverse ? -maxReverseSpeed * s.Throttle : maxForwardSpeed * s.Throttle;
            if (s.Brake > 0.01f)
                return Mathf.MoveTowards(current, 0f, brakeDeceleration * s.Brake * dt);
            if (s.Throttle > 0.01f)
                return Mathf.MoveTowards(current, target, acceleration * dt);
            return Mathf.MoveTowards(current, 0f, coastDeceleration * dt);
        }

        private void ApplySteering(float steer, float speed, float dt)
        {
            if (Mathf.Abs(steer) < 0.001f || Mathf.Abs(speed) < 0.05f) return;
            var speedFactor = Mathf.Clamp01(Mathf.Abs(speed) / minSpeedForFullTurn);
            var direction = Mathf.Sign(speed); // steering flips when reversing, like a real vehicle
            var yaw = steer * turnRateDegPerSec * speedFactor * direction * dt;
            _rb.MoveRotation(_rb.rotation * Quaternion.Euler(0f, yaw, 0f));
        }
    }
}
