using System;
using System.Collections;
using System.IO;
using DeliveryBot.Delivery;

namespace DeliveryBot.Tests
{
    /// <summary>
    /// Shared helpers for PlayMode tests that need a running round. The City scene starts on the
    /// nickname screen with the robot frozen, so tests call <see cref="BeginRound"/> right after
    /// loading. The leaderboard file is redirected to a temp path so tests never touch the real one.
    /// </summary>
    public static class RoundTestKit
    {
        public static string TempLeaderboardPath { get; private set; }

        /// <summary>Redirects the leaderboard file and starts a round with a test nickname (no-op without a GameFlow).</summary>
        public static IEnumerator BeginRound(string nickname = "test")
        {
            UseTempLeaderboard();
            var flow = GameFlow.Instance;
            if (flow != null && flow.State == FlowState.NameEntry)
            {
                flow.BeginRound(nickname);
                yield return null;
            }
        }

        public static void UseTempLeaderboard()
        {
            if (TempLeaderboardPath == null)
                TempLeaderboardPath = Path.Combine(Path.GetTempPath(), $"deliverybot-tests-{Guid.NewGuid():N}", "leaderboard.json");
            LeaderboardStore.PathOverride = TempLeaderboardPath;
        }

        public static void ResetTempLeaderboard()
        {
            if (TempLeaderboardPath != null && File.Exists(TempLeaderboardPath)) File.Delete(TempLeaderboardPath);
        }
    }
}
