using System;
using DeliveryBot.Input;
using DeliveryBot.Vehicle;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DeliveryBot.Delivery
{
    public enum FlowState { NameEntry, Playing, Results }

    /// <summary>
    /// Meta loop around <see cref="DeliveryManager"/>: nickname → timed round → local ranking → nickname.
    /// Runs before other scripts so an expiring timer wins over a same-frame delivery.
    /// While a menu is open the robot's input source is swapped for a frozen one, which also
    /// blocks the Interact poll inside DeliveryManager.
    /// </summary>
    [DefaultExecutionOrder(-50)]
    public sealed class GameFlow : MonoBehaviour
    {
        public const string LastNicknamePref = "DeliveryBot.LastNickname";

        public static GameFlow Instance { get; private set; }
        /// <summary>True whenever a GameFlow exists and the round is not running. Global hotkeys (R/H/F1) check this.</summary>
        public static bool MenuOpen => Instance != null && Instance.State != FlowState.Playing;
        public static string LastNickname => PlayerPrefs.GetString(LastNicknamePref, "");

        [SerializeField] private DeliveryManager manager;
        [SerializeField] private RobotController robot;
        [SerializeField] private float roundSeconds = 180f;

        public FlowState State { get; private set; } = FlowState.NameEntry;
        public RoundTimer Timer { get; private set; }
        public float RoundSeconds => roundSeconds;
        public float Remaining => Timer?.Remaining ?? roundSeconds;
        public string Nickname { get; private set; } = "";
        public LeaderboardData Board { get; private set; }
        public LeaderboardEntry LastEntry { get; private set; }
        /// <summary>0-based rank of <see cref="LastEntry"/> after the last round, -1 if it fell off the list.</summary>
        public int LastRank { get; private set; } = -1;

        public event Action<FlowState> StateChanged;

        private readonly ScriptedDriveInput _frozen = new ScriptedDriveInput { Handbrake = true };
        private float _lastDeliverySeconds;

        private void Awake()
        {
            Instance = this;
            if (manager == null) manager = DeliveryManager.Instance != null ? DeliveryManager.Instance : FindAnyObjectByType<DeliveryManager>();
            if (robot == null)
            {
                var player = GameObject.FindWithTag("Player");
                robot = player != null ? player.GetComponent<RobotController>() : null;
            }
            Timer = new RoundTimer(roundSeconds);
            Board = LeaderboardStore.Load();
        }

        private void Start()
        {
            if (manager != null)
            {
                manager.DeliveryCompleted += OnDelivered;
                manager.PenaltyAdded += OnPenalty;
            }
            Freeze();
            SetState(FlowState.NameEntry);
        }

        private void OnDestroy()
        {
            if (manager != null)
            {
                manager.DeliveryCompleted -= OnDelivered;
                manager.PenaltyAdded -= OnPenalty;
            }
            if (Instance == this) Instance = null;
        }

        private void Update()
        {
            if (State != FlowState.Playing) return;
            Timer.Tick(Time.deltaTime);
            if (Timer.Expired) EndRound();
        }

        /// <summary>Starts a round from the nickname screen. Restores driving input and kicks off the first job.</summary>
        public void BeginRound(string nickname)
        {
            if (State != FlowState.NameEntry) return;
            Nickname = Leaderboard.SanitizeName(nickname);
            PlayerPrefs.SetString(LastNicknamePref, Nickname);
            PlayerPrefs.Save();
            Timer.Reset();
            _lastDeliverySeconds = 0f;
            if (robot != null) robot.SetInputSource(null);
            if (manager != null) manager.BeginRound();
            SetState(FlowState.Playing);
        }

        /// <summary>Stops everything, records the run and shows the results.</summary>
        public void EndRound()
        {
            if (State != FlowState.Playing) return;
            if (manager != null) manager.EndRound();
            Freeze();
            LastEntry = new LeaderboardEntry
            {
                name = Nickname,
                deliveries = manager != null ? manager.Completed : 0,
                lastDeliverySeconds = _lastDeliverySeconds,
                timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                penaltySeconds = manager != null ? manager.PenaltyTime : 0f
            };
            LastRank = Leaderboard.Insert(Board, LastEntry);
            LeaderboardStore.Save(Board);
            SetState(FlowState.Results);
        }

        /// <summary>Fresh scene = fresh traffic, spawn and counters; the nickname is prefilled from PlayerPrefs.</summary>
        public void ReturnToNameEntry() => SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);

        /// <summary>Debug/test helper: jump the clock so only <paramref name="remaining"/> seconds are left.</summary>
        public void SkipToRemaining(float remaining) => Timer.SetElapsed(roundSeconds - remaining);

        private void Freeze()
        {
            if (robot == null) return;
            robot.SetInputSource(_frozen);
            robot.Place(robot.transform.position, robot.transform.rotation);
        }

        private void OnDelivered(int count, float jobSeconds) => _lastDeliverySeconds = Timer.Elapsed;

        private void OnPenalty(float seconds, string reason)
        {
            if (State == FlowState.Playing) Timer.Penalize(seconds);
        }

        private void SetState(FlowState state)
        {
            State = state;
            StateChanged?.Invoke(state);
        }
    }
}
