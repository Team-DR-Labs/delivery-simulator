using System;
using System.Collections.Generic;
using DeliveryBot.Vehicle;
using DeliveryBot.World;
using UnityEngine;

namespace DeliveryBot.Delivery
{
    public enum DeliveryPhase { Idle, ToPickup, ToDropoff }

    /// <summary>
    /// Core loop: random spawn → random pickup (far enough) → random drop-off (far enough) → score → repeat.
    /// Collisions with traffic/pedestrians add time penalties. Fires events for HUD/feedback; owns no UI.
    /// </summary>
    public sealed class DeliveryManager : MonoBehaviour
    {
        public static DeliveryManager Instance { get; private set; }

        [SerializeField] private List<DeliveryPoint> points = new List<DeliveryPoint>();
        [SerializeField] private Transform robot;
        [SerializeField] private RobotController robotController;
        [SerializeField] private Color pickupColor = new Color(0.2f, 0.8f, 1f);
        [SerializeField] private Color dropoffColor = new Color(1f, 0.6f, 0.1f);
        [SerializeField] private float startDelay = 1f;
        [SerializeField] private bool randomSpawn = true;
        [SerializeField] private float minPickupDistance = 35f;
        [SerializeField] private float minDropoffDistance = 70f;
        [SerializeField] private float trafficPenaltySeconds = 5f;
        [SerializeField] private float pedestrianPenaltySeconds = 5f;
        [SerializeField] private float wallPenaltySeconds = 2f;
        [SerializeField] private float wallPenaltyMinSpeed = 4f;

        private readonly System.Random _rng = new System.Random();
        private readonly List<Vector3> _positions = new List<Vector3>();
        private int _targetIndex = -1;

        public DeliveryPhase Phase { get; private set; } = DeliveryPhase.Idle;
        public DeliveryPoint Target { get; private set; }
        public int Completed { get; private set; }
        public int Penalties { get; private set; }
        public float PenaltyTime { get; private set; }
        public float ElapsedThisJob { get; private set; }
        public float TotalTime { get; private set; }
        public float DistanceToTarget => Target != null && robot != null
            ? Vector3.Distance(robot.position, Target.transform.position) : 0f;

        public event Action<DeliveryPhase, DeliveryPoint> PhaseChanged;
        public event Action<int, float> DeliveryCompleted;
        public event Action<float, string> PenaltyAdded;

        private void Awake()
        {
            Instance = this;
            if (robot == null)
            {
                var player = GameObject.FindWithTag("Player");
                robot = player != null ? player.transform : null;
            }
            if (robotController == null && robot != null) robotController = robot.GetComponent<RobotController>();
            _positions.Clear();
            foreach (var p in points) _positions.Add(p != null ? p.transform.position : Vector3.zero);
        }

        private void Start()
        {
            foreach (var p in points) if (p != null) p.SetMarkerVisible(false);
            if (robotController != null) robotController.Impacted += OnRobotImpact;
            if (randomSpawn) SpawnRobotRandomly();
            Invoke(nameof(StartNewJob), startDelay);
        }

        private void OnDestroy()
        {
            if (robotController != null) robotController.Impacted -= OnRobotImpact;
        }

        private void Update()
        {
            if (Phase == DeliveryPhase.Idle) return;
            ElapsedThisJob += Time.deltaTime;
            TotalTime += Time.deltaTime;
        }

        private void SpawnRobotRandomly()
        {
            var layout = CityLayout.Instance;
            if (layout == null || robotController == null) return;
            var g = layout.Graph;
            var node = new RoadGraph.Node(_rng.Next(g.NodesPerAxis), _rng.Next(g.NodesPerAxis));
            var neighbors = g.Neighbors(node);
            var next = neighbors[_rng.Next(neighbors.Count)];
            var pos = g.LanePoint(node, next, 0.2f) + Vector3.up * 0.05f;
            robotController.Place(pos, Quaternion.LookRotation(g.EdgeDirection(node, next), Vector3.up));
        }

        public void OnRobotEntered(DeliveryPoint point)
        {
            if (point != Target) return;

            if (Phase == DeliveryPhase.ToPickup)
            {
                var idx = JobPicker.Pick(_positions, point.transform.position, minDropoffDistance, _targetIndex, _rng);
                SetTarget(DeliveryPhase.ToDropoff, idx, dropoffColor);
            }
            else if (Phase == DeliveryPhase.ToDropoff)
            {
                Completed++;
                DeliveryCompleted?.Invoke(Completed, ElapsedThisJob);
                StartNewJob();
            }
        }

        private void StartNewJob()
        {
            ElapsedThisJob = 0f;
            var from = robot != null ? robot.position : Vector3.zero;
            var idx = JobPicker.Pick(_positions, from, minPickupDistance, _targetIndex, _rng);
            SetTarget(DeliveryPhase.ToPickup, idx, pickupColor);
        }

        private void SetTarget(DeliveryPhase phase, int index, Color color)
        {
            if (Target != null) Target.SetMarkerVisible(false);
            _targetIndex = index;
            Target = index >= 0 && index < points.Count ? points[index] : null;
            Phase = Target != null ? phase : DeliveryPhase.Idle;
            if (Target != null)
            {
                Target.SetMarkerColor(color);
                Target.SetMarkerVisible(true);
            }
            PhaseChanged?.Invoke(Phase, Target);
        }

        private void OnRobotImpact(Collision collision)
        {
            var other = collision.collider;
            if (other.CompareTag("Traffic")) AddPenalty(trafficPenaltySeconds, "차량 충돌");
            else if (other.CompareTag("Pedestrian")) AddPenalty(pedestrianPenaltySeconds, "보행자 충돌");
            else if (collision.relativeVelocity.magnitude >= wallPenaltyMinSpeed) AddPenalty(wallPenaltySeconds, "충돌");
        }

        public void AddPenalty(float seconds, string reason)
        {
            Penalties++;
            PenaltyTime += seconds;
            ElapsedThisJob += seconds;
            TotalTime += seconds;
            PenaltyAdded?.Invoke(seconds, reason);
        }

        public void SetPoints(IEnumerable<DeliveryPoint> newPoints) => points = new List<DeliveryPoint>(newPoints);

        public void SetRobot(Transform t)
        {
            robot = t;
            robotController = t != null ? t.GetComponent<RobotController>() : null;
        }
    }
}
