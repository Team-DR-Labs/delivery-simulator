using System;
using System.IO;
using DeliveryBot.Delivery;
using NUnit.Framework;

namespace DeliveryBot.Tests
{
    public class LeaderboardStoreTests
    {
        private string _path;

        [SetUp]
        public void SetUp() => _path = Path.Combine(Path.GetTempPath(), $"deliverybot-{Guid.NewGuid():N}", "leaderboard.json");

        [TearDown]
        public void TearDown()
        {
            var dir = Path.GetDirectoryName(_path);
            if (dir != null && Directory.Exists(dir)) Directory.Delete(dir, true);
        }

        [Test]
        public void Save_ThenLoad_RoundTrips()
        {
            var data = new LeaderboardData();
            Leaderboard.Insert(data, new LeaderboardEntry { name = "x", deliveries = 4, lastDeliverySeconds = 99f, timestamp = 5 });
            LeaderboardStore.Save(data, _path);
            Assert.IsTrue(File.Exists(_path));
            Assert.IsFalse(File.Exists(_path + ".tmp"));
            var back = LeaderboardStore.Load(_path);
            Assert.AreEqual(1, back.entries.Count);
            Assert.AreEqual(4, back.entries[0].deliveries);
        }

        [Test]
        public void Load_MissingFile_ReturnsEmpty()
        {
            Assert.AreEqual(0, LeaderboardStore.Load(_path).entries.Count);
        }

        [Test]
        public void Load_CorruptFile_ReturnsEmpty()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_path));
            File.WriteAllText(_path, "{\"entries\": [ {broken");
            Assert.AreEqual(0, LeaderboardStore.Load(_path).entries.Count);
        }
    }
}
