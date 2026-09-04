using System;
using System.Collections.Generic;
using DeliveryBot.Input;
using DeliveryBot.Vehicle;
using DeliveryBot.World;
using UnityEngine;

namespace DeliveryBot.Delivery
{
    public enum DeliveryPhase { Idle, ToPickup, ToDropoff }

    /// <summary>
    /// Core loop: random spawn → pick up at a shop (press Interact in range) → deliver to a home
    /// (press Interact in range) → score → repeat. Collisions add time penalties. Owns no UI.
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
        [Tooltip("Start the first job automatically after startDelay. GameFlow turns this off and calls BeginRound instead.")]
        [SerializeField] private bool autoStart = true;
        [SerializeField] private bool randomSpawn = true;
        [SerializeField] private float minPickupDistance = 35f;
        [SerializeField] private float minDropoffDistance = 70f;
        [SerializeField] private float maxInteractSpeed = 2.5f;
        [SerializeField] private float trafficPenaltySeconds = 5f;
        [SerializeField] private float pedestrianPenaltySeconds = 5f;
        [SerializeField] private float wallPenaltySeconds = 2f;
        [SerializeField] private float wallPenaltyMinSpeed = 4f;

        private readonly System.Random _rng = new System.Random();
        private readonly List<DeliveryPoint> _shops = new List<DeliveryPoint>();
        private readonly List<DeliveryPoint> _homes = new List<DeliveryPoint>();
        private readonly List<Vector3> _shopPositions = new List<Vector3>();
        private readonly List<Vector3> _homePositions = new List<Vector3>();
        private DeliveryPoint _lastShop, _lastHome;
        private readonly HashSet<DeliveryPoint> _inRange = new HashSet<DeliveryPoint>();

        public DeliveryPhase Phase { get; private set; } = DeliveryPhase.Idle;
        public DeliveryPoint Target { get; private set; }
        public DeliveryPoint Pickup { get; private set; }
        /// <summary>True while the robot is inside the current target's trigger (neighbouring doorsteps may overlap).</summary>
        public bool IsTargetInRange => Target != null && _inRange.Contains(Target);
        public bool CanInteract => IsTargetInRange && RobotSlowEnough;
        public bool RobotSlowEnough => robotController == null || Mathf.Abs(robotController.ForwardSpeed) <= maxInteractSpeed;
        public int Completed { get; private set; }
        public int Penalties { get; private set; }
        public float PenaltyTime { get; private set; }
        public float ElapsedThisJob { get; private set; }
        public float TotalTime { get; private set; }
        /// <summary>True between <see cref="BeginRound"/> and <see cref="EndRound"/> (or always, when autoStart is on).</summary>
        public bool RoundActive { get; private set; }
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
            IndexPoints();
        }

        private void IndexPoints()
        {
            _shops.Clear(); _homes.Clear(); _shopPositions.Clear(); _homePositions.Clear();
            foreach (var p in points)
            {
                if (p == null) continue;
                if (p.Kind == PointKind.Shop) { _shops.Add(p); _shopPositions.Add(p.transform.position); }
                else { _homes.Add(p); _homePositions.Add(p.transform.position); }
            }
        }

        private void Start()
        {
            foreach (var p in points) if (p != null) p.SetMarkerVisible(false);
            if (robotController != null) robotController.Impacted += OnRobotImpact;
            if (randomSpawn) SpawnRobotRandomly();
            if (autoStart)
            {
                RoundActive = true;
                Invoke(nameof(StartNewJob), startDelay);
            }
        }

        /// <summary>Resets all counters and starts the first job immediately (synchronously, so tests can proceed).</summary>
        public void BeginRound()
        {
            CancelInvoke(nameof(StartNewJob));
            Completed = 0;
            Penalties = 0;
            PenaltyTime = 0f;
            TotalTime = 0f;
            ElapsedThisJob = 0f;
            _lastShop = null;
            _lastHome = null;
            Pickup = null;
            RoundActive = true;
            StartNewJob();
        }

        /// <summary>Stops the loop: current job discarded, marker hidden, Phase back to Idle.</summary>
        public void EndRound()
        {
            CancelInvoke(nameof(StartNewJob));
            RoundActive = false;
            Pickup = null;
            SetTarget(DeliveryPhase.Idle, null, Color.clear);
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

            var input = robotController != null ? robotController.InputSource : null;
            if (input != null && input.Current.Interact) TryInteract();
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

        public void SetRobotInRange(DeliveryPoint point, bool inRange)
        {
            if (inRange) _inRange.Add(point);
            else _inRange.Remove(point);
        }

        /// <summary>Called on the Interact button; completes the current step if the robot is at the target.</summary>
        public bool TryInteract()
        {
            if (!RoundActive || !CanInteract) return false;
            if (Phase == DeliveryPhase.ToPickup)
            {
                Pickup = Target;
                _lastShop = Target;
                var idx = JobPicker.Pick(_homePositions, Target.transform.position, minDropoffDistance, _homes.IndexOf(_lastHome), _rng);
                SetTarget(DeliveryPhase.ToDropoff, idx >= 0 ? _homes[idx] : null, dropoffColor);
                return true;
            }
            if (Phase == DeliveryPhase.ToDropoff)
            {
                _lastHome = Target;
                Completed++;
                DeliveryCompleted?.Invoke(Completed, ElapsedThisJob);
                StartNewJob();
                return true;
            }
            return false;
        }

        private void StartNewJob()
        {
            ElapsedThisJob = 0f;
            Pickup = null;
            var from = robot != null ? robot.position : Vector3.zero;
            var idx = JobPicker.Pick(_shopPositions, from, minPickupDistance, _shops.IndexOf(_lastShop), _rng);
            SetTarget(DeliveryPhase.ToPickup, idx >= 0 ? _shops[idx] : null, pickupColor);
        }

        private void SetTarget(DeliveryPhase phase, DeliveryPoint point, Color color)
        {
            if (Target != null) Target.SetMarkerVisible(false);
            Target = point;
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

        public void SetPoints(IEnumerable<DeliveryPoint> newPoints)
        {
            points = new List<DeliveryPoint>(newPoints);
            IndexPoints();
        }

        public void SetRobot(Transform t)
        {
            robot = t;
            robotController = t != null ? t.GetComponent<RobotController>() : null;
        }
    }
}
