using System;
using System.Collections.Generic;
using UnityEngine;

namespace DeliveryBot.Delivery
{
    public enum DeliveryPhase { Idle, ToPickup, ToDropoff }

    /// <summary>
    /// Core game loop: pick a pickup point, then a drop-off point, score on completion, repeat.
    /// Fires events for the HUD; keeps no UI references itself.
    /// </summary>
    public sealed class DeliveryManager : MonoBehaviour
    {
        public static DeliveryManager Instance { get; private set; }

        [SerializeField] private List<DeliveryPoint> points = new List<DeliveryPoint>();
        [SerializeField] private Transform robot;
        [SerializeField] private Color pickupColor = new Color(0.2f, 0.8f, 1f);
        [SerializeField] private Color dropoffColor = new Color(1f, 0.6f, 0.1f);
        [SerializeField] private float startDelay = 1f;

        private readonly System.Random _rng = new System.Random();

        public DeliveryPhase Phase { get; private set; } = DeliveryPhase.Idle;
        public DeliveryPoint Target { get; private set; }
        public int Completed { get; private set; }
        public float ElapsedThisJob { get; private set; }
        public float DistanceToTarget => Target != null && robot != null
            ? Vector3.Distance(robot.position, Target.transform.position) : 0f;

        public event Action<DeliveryPhase, DeliveryPoint> PhaseChanged;
        public event Action<int, float> DeliveryCompleted;

        private void Awake()
        {
            Instance = this;
            if (robot == null)
            {
                var player = GameObject.FindWithTag("Player");
                robot = player != null ? player.transform : null;
            }
        }

        private void Start()
        {
            foreach (var p in points) p.SetMarkerVisible(false);
            Invoke(nameof(StartNewJob), startDelay);
        }

        private void Update()
        {
            if (Phase != DeliveryPhase.Idle) ElapsedThisJob += Time.deltaTime;
        }

        public void OnRobotEntered(DeliveryPoint point)
        {
            if (point != Target) return;

            if (Phase == DeliveryPhase.ToPickup)
            {
                SetTarget(DeliveryPhase.ToDropoff, PickRandom(exclude: point), dropoffColor);
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
            var pickup = PickRandom(exclude: Target);
            SetTarget(DeliveryPhase.ToPickup, pickup, pickupColor);
        }

        private void SetTarget(DeliveryPhase phase, DeliveryPoint point, Color color)
        {
            if (Target != null) Target.SetMarkerVisible(false);
            Target = point;
            Phase = point != null ? phase : DeliveryPhase.Idle;
            if (point != null)
            {
                point.SetMarkerColor(color);
                point.SetMarkerVisible(true);
            }
            PhaseChanged?.Invoke(Phase, Target);
        }

        private DeliveryPoint PickRandom(DeliveryPoint exclude)
        {
            var candidates = points.FindAll(p => p != null && p != exclude);
            if (candidates.Count == 0) return points.Count > 0 ? points[0] : null;
            return candidates[_rng.Next(candidates.Count)];
        }

        public void SetPoints(IEnumerable<DeliveryPoint> newPoints) => points = new List<DeliveryPoint>(newPoints);
        public void SetRobot(Transform t) => robot = t;
    }
}
