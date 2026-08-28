using DeliveryBot.World;
using UnityEngine;

namespace DeliveryBot.Traffic
{
    /// <summary>
    /// Kinematic pedestrian walking the sidewalk loop of one block. Pauses now and then,
    /// stops when the robot is close in front, and gets nudged aside (with a "!" bubble) when hit.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public sealed class Pedestrian : MonoBehaviour
    {
        [SerializeField] private Transform body;
        [SerializeField] private GameObject alertBubble;
        [SerializeField] private float walkSpeed = 1.3f;
        [SerializeField] private float robotStopDistance = 2.6f;

        private Rigidbody _rb;
        private Vector3[] _loop;
        private int _targetCorner;
        private int _direction = 1;
        private float _pauseUntil;
        private float _alertUntil;
        private float _bobPhase;
        private System.Random _rng;
        private Transform _robot;
        private float _height;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody>();
            _rb.isKinematic = true;
            _rb.interpolation = RigidbodyInterpolation.Interpolate;
        }

        public void Init(Vector3[] loop, int startCorner, float startT, bool clockwise, float height, Transform robot, int seed)
        {
            _loop = loop;
            _rng = new System.Random(seed);
            _direction = clockwise ? 1 : -1;
            _robot = robot;
            _height = height;
            walkSpeed = 1.0f + (float)_rng.NextDouble() * 0.7f;
            var a = loop[startCorner];
            var b = loop[Wrap(startCorner + _direction)];
            _targetCorner = Wrap(startCorner + _direction);
            var pos = Vector3.Lerp(a, b, startT);
            transform.position = new Vector3(pos.x, height, pos.z);
            _bobPhase = (float)_rng.NextDouble() * 10f;
        }

        private int Wrap(int i) => (i % _loop.Length + _loop.Length) % _loop.Length;

        private void FixedUpdate()
        {
            if (_loop == null) return;
            if (alertBubble != null && alertBubble.activeSelf && Time.time > _alertUntil) alertBubble.SetActive(false);
            if (Time.time < _pauseUntil || RobotInFront()) { Animate(false); return; }

            var target = _loop[_targetCorner];
            target.y = _height;
            var to = target - transform.position;
            if (to.magnitude < 0.3f)
            {
                _targetCorner = Wrap(_targetCorner + _direction);
                if (_rng.NextDouble() < 0.25) _pauseUntil = Time.time + 1f + (float)_rng.NextDouble() * 3f;
                if (_rng.NextDouble() < 0.1) _direction = -_direction;
                return;
            }

            var step = to.normalized * (walkSpeed * Time.fixedDeltaTime);
            _rb.MovePosition(transform.position + step);
            _rb.MoveRotation(Quaternion.Slerp(_rb.rotation, Quaternion.LookRotation(to.normalized, Vector3.up), Time.fixedDeltaTime * 8f));
            Animate(true);
        }

        private bool RobotInFront()
        {
            if (_robot == null) return false;
            var rel = _robot.position - transform.position;
            return rel.magnitude < robotStopDistance && Vector3.Dot(rel.normalized, transform.forward) > 0.3f;
        }

        private void Animate(bool walking)
        {
            if (body == null) return;
            if (!walking) { body.localRotation = Quaternion.identity; return; }
            _bobPhase += Time.fixedDeltaTime * walkSpeed * 6f;
            body.localPosition = new Vector3(0f, Mathf.Abs(Mathf.Sin(_bobPhase)) * 0.05f, 0f);
            body.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(_bobPhase) * 3f);
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (!collision.collider.attachedRigidbody || !collision.collider.attachedRigidbody.CompareTag("Player")) return;
            var away = transform.position - collision.collider.transform.position;
            away.y = 0f;
            transform.position += away.normalized * 0.7f;
            _pauseUntil = Time.time + 2f;
            if (alertBubble != null)
            {
                alertBubble.SetActive(true);
                _alertUntil = Time.time + 1.5f;
            }
        }
    }
}
