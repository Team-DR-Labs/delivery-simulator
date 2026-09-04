using UnityEngine;

namespace DeliveryBot.Delivery
{
    /// <summary>Countdown for one round. Penalties burn time directly. Plain class so it is testable without a scene.</summary>
    public sealed class RoundTimer
    {
        public float Duration { get; }
        public float Elapsed { get; private set; }
        public float Remaining => Mathf.Max(0f, Duration - Elapsed);
        public bool Expired => Elapsed >= Duration;

        public RoundTimer(float duration)
        {
            Duration = Mathf.Max(0f, duration);
        }

        public bool IsWarning(float threshold = 10f) => Remaining <= threshold;

        public void Tick(float dt) => SetElapsed(Elapsed + Mathf.Max(0f, dt));

        /// <summary>Removes seconds from the remaining time (collision penalties).</summary>
        public void Penalize(float seconds) => SetElapsed(Elapsed + Mathf.Max(0f, seconds));

        public void Reset() => Elapsed = 0f;

        public void SetElapsed(float seconds) => Elapsed = Mathf.Clamp(seconds, 0f, Duration);
    }
}
