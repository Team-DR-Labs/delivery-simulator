using System;
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
        [SerializeField] private float maxForwardSpeed = 7f;
        [SerializeField] private float maxReverseSpeed = 2.5f;
        [SerializeField] private float acceleration = 5f;
        [SerializeField] private float brakeDeceleration = 10f;
        [SerializeField] private float coastDeceleration = 2.5f;

        [Tooltip("Below this forward speed, holding the brake starts reversing (racing-game style)")]
        [SerializeField] private float brakeToReverseSpeed = 0.3f;

        [Header("Steering")]
        [SerializeField] private float turnRateDegPerSec = 95f;
        [Tooltip("Steering is scaled by speed so the robot cannot spin in place")]
        [SerializeField] private float minSpeedForFullTurn = 1.5f;

        [Header("Ground")]
        [SerializeField] private float groundCheckDistance = 0.5f;
        [SerializeField] private LayerMask groundMask = ~0;

        [Header("Collisions")]
        [SerializeField] private float minImpactSpeed = 1.5f;
        [SerializeField] private float impactCooldown = 0.8f;

        private Rigidbody _rb;
        private float _lastImpactTime = -10f;

        public float ForwardSpeed { get; private set; }
        public bool IsGrounded { get; private set; }
        public float MaxForwardSpeed => maxForwardSpeed;

        /// <summary>Raised when the robot bumps into something tagged Traffic/Pedestrian (or any solid at speed).</summary>
        public event Action<Collision> Impacted;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody>();
            _rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
            _rb.interpolation = RigidbodyInterpolation.Interpolate;
            if (input == null) input = GetComponent<DriveInputProvider>();
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
            {
                // Brake while moving forward; once (almost) stopped, keep holding to reverse.
                if (current > brakeToReverseSpeed)
                    return Mathf.MoveTowards(current, 0f, brakeDeceleration * s.Brake * dt);
                return Mathf.MoveTowards(current, -maxReverseSpeed * s.Brake, acceleration * dt);
            }
            if (s.Throttle > 0.01f)
            {
                // Throttle while rolling backwards acts as a brake first.
                if (current < -brakeToReverseSpeed && !s.Reverse)
                    return Mathf.MoveTowards(current, 0f, brakeDeceleration * s.Throttle * dt);
                return Mathf.MoveTowards(current, target, acceleration * dt);
            }
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

        private void OnCollisionEnter(Collision collision)
        {
            if (collision.relativeVelocity.magnitude < minImpactSpeed) return;
            if (Time.time - _lastImpactTime < impactCooldown) return;
            _lastImpactTime = Time.time;
            Impacted?.Invoke(collision);
        }

        /// <summary>Teleports the robot (used for random spawn).</summary>
        public void Place(Vector3 position, Quaternion rotation)
        {
            if (_rb == null) _rb = GetComponent<Rigidbody>();
            transform.SetPositionAndRotation(position, rotation);
            _rb.position = position;
            _rb.rotation = rotation;
            _rb.linearVelocity = Vector3.zero;
            _rb.angularVelocity = Vector3.zero;
        }
    }
}
