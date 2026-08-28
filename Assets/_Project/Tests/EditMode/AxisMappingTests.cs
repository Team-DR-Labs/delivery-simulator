using DeliveryBot.Input;
using NUnit.Framework;

namespace DeliveryBot.Tests
{
    public class AxisMappingTests
    {
        [Test]
        public void Pedal_RestIsZero_PressedIsOne()
        {
            var m = new PedalAxisMapping { restValue = 1f, pressedValue = -1f, deadzone = 0.03f };
            Assert.AreEqual(0f, m.Normalize(1f), 1e-4f);
            Assert.AreEqual(1f, m.Normalize(-1f), 1e-4f);
            Assert.AreEqual(0.5f, m.Normalize(0f), 1e-4f);
        }

        [Test]
        public void Pedal_Deadzone_SuppressesSmallValues()
        {
            var m = new PedalAxisMapping { restValue = 1f, pressedValue = -1f, deadzone = 0.05f };
            Assert.AreEqual(0f, m.Normalize(0.95f), 1e-4f);
        }

        [Test]
        public void Steer_RangeScaling_ReachesFullLockEarly()
        {
            var m = new SteerAxisMapping { wheelRangeDegrees = 900f, usedRangeDegrees = 270f, deadzone = 0f };
            Assert.AreEqual(1f, m.Normalize(0.5f), 1e-4f);   // 450° of 900° -> beyond 270° -> clamped
            Assert.AreEqual(-0.5f, m.Normalize(-0.15f), 1e-4f);
        }

        [Test]
        public void Steer_Invert_FlipsSign()
        {
            var m = new SteerAxisMapping { invert = true, wheelRangeDegrees = 1f, usedRangeDegrees = 1f, deadzone = 0f };
            Assert.AreEqual(-0.3f, m.Normalize(0.3f), 1e-4f);
        }
    }
}
