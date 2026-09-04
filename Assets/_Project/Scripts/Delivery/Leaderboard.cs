using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace DeliveryBot.Delivery
{
    /// <summary>One finished round. Every play adds its own row (same nickname may repeat).</summary>
    [Serializable]
    public sealed class LeaderboardEntry
    {
        public string name;
        public int deliveries;
        /// <summary>Seconds into the round when the last delivery completed; 0 when nothing was delivered.</summary>
        public float lastDeliverySeconds;
        /// <summary>Unix time in milliseconds when the round ended.</summary>
        public long timestamp;
        public float penaltySeconds;
    }

    /// <summary>JsonUtility cannot serialize a bare list, so the file root is this wrapper.</summary>
    [Serializable]
    public sealed class LeaderboardData
    {
        public int version = 1;
        public List<LeaderboardEntry> entries = new List<LeaderboardEntry>();
    }

    /// <summary>
    /// Pure ranking rules for the 180 s time-attack: deliveries desc, then the run whose last delivery
    /// came earlier, then the older record. No scene access so it is unit-testable in EditMode.
    /// </summary>
    public static class Leaderboard
    {
        public const int MaxRows = 100;
        public const int TopCount = 10;
        public const int MaxNameLength = 12;
        public const string DefaultName = "플레이어";

        public static int Compare(LeaderboardEntry a, LeaderboardEntry b)
        {
            if (a.deliveries != b.deliveries) return b.deliveries.CompareTo(a.deliveries);
            if (a.deliveries > 0 && !Mathf.Approximately(a.lastDeliverySeconds, b.lastDeliverySeconds))
                return a.lastDeliverySeconds.CompareTo(b.lastDeliverySeconds);
            return a.timestamp.CompareTo(b.timestamp);
        }

        /// <summary>Adds the entry, re-sorts, trims to <see cref="MaxRows"/>. Returns the 0-based rank, or -1 if trimmed away.</summary>
        public static int Insert(LeaderboardData data, LeaderboardEntry entry)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));
            if (entry == null) throw new ArgumentNullException(nameof(entry));
            data.entries ??= new List<LeaderboardEntry>();
            data.entries.Add(entry);
            data.entries.Sort(Compare);
            if (data.entries.Count > MaxRows) data.entries.RemoveRange(MaxRows, data.entries.Count - MaxRows);
            return data.entries.IndexOf(entry);
        }

        public static List<LeaderboardEntry> Top(LeaderboardData data, int n)
        {
            var result = new List<LeaderboardEntry>();
            if (data?.entries == null) return result;
            for (var i = 0; i < data.entries.Count && i < n; i++) result.Add(data.entries[i]);
            return result;
        }

        /// <summary>Trim, drop control characters, cap the length, fall back to <see cref="DefaultName"/> when empty.</summary>
        public static string SanitizeName(string raw)
        {
            if (string.IsNullOrEmpty(raw)) return DefaultName;
            var sb = new StringBuilder(raw.Length);
            foreach (var c in raw)
                if (c >= ' ') sb.Append(c);
            var trimmed = sb.ToString().Trim();
            if (trimmed.Length > MaxNameLength) trimmed = trimmed.Substring(0, MaxNameLength);
            return trimmed.Length == 0 ? DefaultName : trimmed;
        }

        public static string ToJson(LeaderboardData data) => JsonUtility.ToJson(data ?? new LeaderboardData(), true);

        /// <summary>Never throws: null, empty or corrupt JSON yields an empty board.</summary>
        public static LeaderboardData FromJson(string json)
        {
            if (string.IsNullOrWhiteSpace(json)) return new LeaderboardData();
            try
            {
                var data = JsonUtility.FromJson<LeaderboardData>(json);
                if (data == null) return new LeaderboardData();
                data.entries ??= new List<LeaderboardEntry>();
                data.entries.RemoveAll(e => e == null);
                return data;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Leaderboard] Could not parse saved ranking, starting empty: {e.Message}");
                return new LeaderboardData();
            }
        }

        /// <summary>Rounds up to whole seconds and formats as M:SS; negative values clamp to 0:00.</summary>
        public static string FormatClock(float seconds)
        {
            var total = Mathf.Max(0, Mathf.CeilToInt(seconds));
            return $"{total / 60}:{total % 60:00}";
        }
    }
}
