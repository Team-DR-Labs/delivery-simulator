using System;
using System.IO;
using UnityEngine;

namespace DeliveryBot.Delivery
{
    /// <summary>Local-only persistence: one JSON file under persistentDataPath. Tests redirect it via <see cref="PathOverride"/>.</summary>
    public static class LeaderboardStore
    {
        public const string FileName = "leaderboard.json";

        /// <summary>When set, Load/Save use this path instead of the real one (PlayMode tests).</summary>
        public static string PathOverride;

        public static string FilePath => PathOverride ?? Path.Combine(Application.persistentDataPath, FileName);

        public static LeaderboardData Load(string path = null)
        {
            path ??= FilePath;
            try
            {
                if (!File.Exists(path)) return new LeaderboardData();
                return Leaderboard.FromJson(File.ReadAllText(path));
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[LeaderboardStore] Could not read {path}, starting empty: {e.Message}");
                return new LeaderboardData();
            }
        }

        /// <summary>Writes to a temp file first so a crash mid-write cannot leave a truncated ranking.</summary>
        public static void Save(LeaderboardData data, string path = null)
        {
            path ??= FilePath;
            try
            {
                var dir = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
                var tmp = path + ".tmp";
                File.WriteAllText(tmp, Leaderboard.ToJson(data));
                File.Copy(tmp, path, true);
                File.Delete(tmp);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[LeaderboardStore] Could not write {path}: {e.Message}");
            }
        }
    }
}
