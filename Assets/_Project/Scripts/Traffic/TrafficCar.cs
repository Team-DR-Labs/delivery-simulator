using DeliveryBot.World;
using UnityEngine;

namespace DeliveryBot.Traffic
{
    /// <summary>
    /// Kinematic car that follows the right-hand lane of the road graph, picks a random
    /// direction at every intersection (no U-turns) and brakes for anything with a rigidbody ahead.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public sealed class TrafficCar : MonoBehaviour
    {
        [SerializeField] private float cruiseSpeed = 6f;
        [SerializeField] private float acceleration = 4f;
        [SerializeField] private float braking = 9f;
        [SerializeField] private float halfLength = 2.2f;
        [Tooltip("Half extents of the sensing box in front of the bumper; z = half of the look-ahead distance")]
        [SerializeField] private Vector3 senseHalfExtents = new Vector3(1.3f, 0.8f, 3.5f);

        private Rigidbody _rb;
        private Collider _self;
        private RoadGraph _graph;
        private RoadGraph.Node _from, _to;
        private float _distance;
        private float _speed;
        private System.Random _rng;
        private readonly Collider[] _hits = new Collider[8];

        public bool IsBlocked { get; private set; }

        private void Awake()
        {
            _rb = GetComponent<Rigidbody>();
            _rb.isKinematic = true;
            _rb.interpolation = RigidbodyInterpolation.Interpolate;
            _self = GetComponent<Collider>();
        }

        public void Init(RoadGraph graph, RoadGraph.Node from, RoadGraph.Node to, float distanceAlongEdge, float speed, int seed)
        {
            _graph = graph;
            _from = from;
            _to = to;
            _distance = distanceAlongEdge;
            cruiseSpeed = speed;
            _rng = new System.Random(seed);
            var pos = graph.LanePoint(from, to, distanceAlongEdge / graph.EdgeLength);
            transform.SetPositionAndRotation(pos, Quaternion.LookRotation(graph.EdgeDirection(from, to), Vector3.up));
        }

        private void FixedUpdate()
        {
            if (_graph == null) return;
            IsBlocked = SomethingAhead();
            var target = IsBlocked ? 0f : cruiseSpeed;
            var rate = target < _speed ? braking : acceleration;
            _speed = Mathf.MoveTowards(_speed, target, rate * Time.fixedDeltaTime);

            _distance += _speed * Time.fixedDeltaTime;
            var len = _graph.EdgeLength;
            if (_distance >= len)
            {
                var next = _graph.NextNode(_from, _to, _rng);
                _from = _to;
                _to = next;
                _distance -= len;
            }

            var pos = _graph.LanePoint(_from, _to, _distance / len);
            var rot = Quaternion.LookRotation(_graph.EdgeDirection(_from, _to), Vector3.up);
            _rb.MovePosition(pos);
            _rb.MoveRotation(Quaternion.Slerp(_rb.rotation, rot, Time.fixedDeltaTime * 6f));
        }

        private bool SomethingAhead()
        {
            var center = transform.position + transform.forward * (halfLength + senseHalfExtents.z) + Vector3.up * 0.8f;
            var count = Physics.OverlapBoxNonAlloc(center, senseHalfExtents, _hits, transform.rotation, ~0, QueryTriggerInteraction.Ignore);
            for (var i = 0; i < count; i++)
            {
                var c = _hits[i];
                if (c == _self || c.attachedRigidbody == null) continue;
                if (c.attachedRigidbody == _rb) continue;
                return true;
            }
            return false;
        }

        /// <summary>Tints every renderer named "Paint" (body panels) without creating material instances.</summary>
        public void SetColor(Color color)
        {
            var block = new MaterialPropertyBlock();
            block.SetColor("_Color", color);
            block.SetColor("_BaseColor", color);
            foreach (var r in GetComponentsInChildren<Renderer>())
                if (r.name == "Paint") r.SetPropertyBlock(block);
        }
    }
}
