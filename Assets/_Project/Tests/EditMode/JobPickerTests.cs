using System.Collections.Generic;
using DeliveryBot.Delivery;
using NUnit.Framework;
using UnityEngine;

namespace DeliveryBot.Tests
{
    public class JobPickerTests
    {
        private static readonly List<Vector3> Points = new List<Vector3>
        {
            new Vector3(0, 0, 0), new Vector3(10, 0, 0), new Vector3(100, 0, 0), new Vector3(0, 0, 120)
        };

        [Test]
        public void PrefersFarPoints()
        {
            var rng = new System.Random(5);
            for (var i = 0; i < 30; i++)
            {
                var idx = JobPicker.Pick(Points, Vector3.zero, 50f, -1, rng);
                Assert.IsTrue(idx == 2 || idx == 3, $"picked {idx}");
            }
        }

        [Test]
        public void NeverReturnsExcluded()
        {
            var rng = new System.Random(9);
            for (var i = 0; i < 30; i++)
                Assert.AreNotEqual(2, JobPicker.Pick(Points, new Vector3(100, 0, 0), 5f, 2, rng));
        }

        [Test]
        public void RelaxesDistanceWhenNothingIsFar()
        {
            var rng = new System.Random(1);
            var idx = JobPicker.Pick(Points, Vector3.zero, 10000f, 0, rng);
            Assert.IsTrue(idx >= 1 && idx <= 3);
        }

        [Test]
        public void EmptyListReturnsMinusOne()
        {
            Assert.AreEqual(-1, JobPicker.Pick(new List<Vector3>(), Vector3.zero, 1f, -1, new System.Random()));
        }
    }
}
