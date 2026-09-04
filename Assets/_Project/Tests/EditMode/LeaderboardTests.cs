using System.Collections.Generic;
using DeliveryBot.Delivery;
using NUnit.Framework;

namespace DeliveryBot.Tests
{
    public class LeaderboardTests
    {
        private static LeaderboardEntry E(string name, int deliveries, float last, long ts) =>
            new LeaderboardEntry { name = name, deliveries = deliveries, lastDeliverySeconds = last, timestamp = ts };

        [Test]
        public void Compare_MoreDeliveriesRanksFirst()
        {
            Assert.Less(Leaderboard.Compare(E("a", 5, 170f, 2), E("b", 4, 10f, 1)), 0);
        }

        [Test]
        public void Compare_TieBreaksByEarlierLastDelivery()
        {
            Assert.Less(Leaderboard.Compare(E("a", 3, 120f, 9), E("b", 3, 150f, 1)), 0);
        }

        [Test]
        public void Compare_ThenByEarlierTimestamp()
        {
            Assert.Less(Leaderboard.Compare(E("a", 3, 120f, 1), E("b", 3, 120f, 2)), 0);
        }

        [Test]
        public void Compare_ZeroDeliveriesIgnoresSeconds()
        {
            // Both delivered nothing: lastDeliverySeconds is meaningless, order purely by time.
            Assert.Less(Leaderboard.Compare(E("a", 0, 100f, 1), E("b", 0, 0f, 2)), 0);
        }

        [Test]
        public void Insert_ReturnsRankAndKeepsSorted()
        {
            var data = new LeaderboardData();
            Leaderboard.Insert(data, E("a", 2, 100f, 1));
            Leaderboard.Insert(data, E("b", 5, 100f, 2));
            var rank = Leaderboard.Insert(data, E("c", 3, 100f, 3));
            Assert.AreEqual(1, rank);
            Assert.AreEqual("b", data.entries[0].name);
            Assert.AreEqual("c", data.entries[1].name);
            Assert.AreEqual("a", data.entries[2].name);
        }

        [Test]
        public void Insert_TrimsToMaxRows_ReturnsMinusOneWhenDropped()
        {
            var data = new LeaderboardData();
            for (var i = 0; i < Leaderboard.MaxRows; i++) Leaderboard.Insert(data, E("p", 10, 50f, i));
            var rank = Leaderboard.Insert(data, E("worst", 1, 50f, 999));
            Assert.AreEqual(-1, rank);
            Assert.AreEqual(Leaderboard.MaxRows, data.entries.Count);
        }

        [Test]
        public void Insert_AllowsDuplicateNames()
        {
            var data = new LeaderboardData();
            Leaderboard.Insert(data, E("same", 1, 10f, 1));
            Leaderboard.Insert(data, E("same", 1, 10f, 2));
            Assert.AreEqual(2, data.entries.Count);
        }

        [Test]
        public void Top_ReturnsAtMostN()
        {
            var data = new LeaderboardData();
            for (var i = 0; i < 15; i++) Leaderboard.Insert(data, E("p", i, 10f, i));
            var top = Leaderboard.Top(data, Leaderboard.TopCount);
            Assert.AreEqual(10, top.Count);
            Assert.AreEqual(14, top[0].deliveries);
            Assert.AreEqual(0, Leaderboard.Top(new LeaderboardData(), 10).Count);
        }

        [Test]
        public void Json_RoundTripPreservesEntriesIncludingKorean()
        {
            var data = new LeaderboardData();
            Leaderboard.Insert(data, E("김배달", 7, 173.5f, 1725500000000L));
            var back = Leaderboard.FromJson(Leaderboard.ToJson(data));
            Assert.AreEqual(1, back.entries.Count);
            Assert.AreEqual("김배달", back.entries[0].name);
            Assert.AreEqual(7, back.entries[0].deliveries);
            Assert.AreEqual(173.5f, back.entries[0].lastDeliverySeconds, 0.001f);
            Assert.AreEqual(1725500000000L, back.entries[0].timestamp);
        }

        [Test]
        public void FromJson_GarbageOrNullReturnsEmpty()
        {
            Assert.AreEqual(0, Leaderboard.FromJson("{not json").entries.Count);
            Assert.AreEqual(0, Leaderboard.FromJson(null).entries.Count);
            Assert.AreEqual(0, Leaderboard.FromJson("   ").entries.Count);
        }

        [Test]
        public void SanitizeName_TrimsTruncatesDefaults()
        {
            Assert.AreEqual(Leaderboard.DefaultName, Leaderboard.SanitizeName(null));
            Assert.AreEqual(Leaderboard.DefaultName, Leaderboard.SanitizeName("   "));
            Assert.AreEqual("abc", Leaderboard.SanitizeName("  abc  "));
            Assert.AreEqual("ab", Leaderboard.SanitizeName("a\nb\t"));
            Assert.AreEqual("123456789012", Leaderboard.SanitizeName("12345678901234567890"));
        }

        [Test]
        public void FormatClock_Examples()
        {
            Assert.AreEqual("3:00", Leaderboard.FormatClock(180f));
            Assert.AreEqual("0:10", Leaderboard.FormatClock(9.2f));
            Assert.AreEqual("0:00", Leaderboard.FormatClock(0f));
            Assert.AreEqual("0:00", Leaderboard.FormatClock(-1f));
            Assert.AreEqual("1:05", Leaderboard.FormatClock(65f));
        }
    }
}
