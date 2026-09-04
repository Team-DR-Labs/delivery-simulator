using DeliveryBot.Delivery;
using NUnit.Framework;

namespace DeliveryBot.Tests
{
    public class RoundTimerTests
    {
        [Test]
        public void Tick_AccumulatesAndClamps()
        {
            var t = new RoundTimer(180f);
            t.Tick(10f);
            Assert.AreEqual(170f, t.Remaining, 0.001f);
            Assert.IsFalse(t.Expired);
            t.Tick(1000f);
            Assert.AreEqual(180f, t.Elapsed, 0.001f);
            Assert.AreEqual(0f, t.Remaining);
            Assert.IsTrue(t.Expired);
        }

        [Test]
        public void Tick_IgnoresNegative()
        {
            var t = new RoundTimer(180f);
            t.Tick(-5f);
            Assert.AreEqual(0f, t.Elapsed);
        }

        [Test]
        public void Penalize_BurnsTimeAndCanExpire()
        {
            var t = new RoundTimer(10f);
            t.Penalize(4f);
            Assert.AreEqual(6f, t.Remaining, 0.001f);
            t.Penalize(100f);
            Assert.IsTrue(t.Expired);
        }

        [Test]
        public void IsWarning_AtThreshold()
        {
            var t = new RoundTimer(180f);
            Assert.IsFalse(t.IsWarning());
            t.SetElapsed(170f);
            Assert.IsTrue(t.IsWarning());
            Assert.IsFalse(t.IsWarning(5f));
        }

        [Test]
        public void Reset_ClearsElapsed()
        {
            var t = new RoundTimer(180f);
            t.Tick(50f);
            t.Reset();
            Assert.AreEqual(0f, t.Elapsed);
            Assert.IsFalse(t.Expired);
        }
    }
}
