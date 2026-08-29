using System.Collections;
using System.Collections.Generic;
using DeliveryBot.Traffic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace DeliveryBot.Tests
{
    /// <summary>Traffic cars and pedestrians must actually move in the City scene.</summary>
    public class NpcMovementTests
    {
        [UnitySetUp]
        public IEnumerator LoadCity()
        {
            yield return SceneManager.LoadSceneAsync("City", LoadSceneMode.Single);
            yield return new WaitForSeconds(1.0f);
        }

        [UnityTest]
        public IEnumerator TrafficCars_Move()
        {
            var cars = Object.FindObjectsByType<TrafficCar>();
            Assert.Greater(cars.Length, 5, "too few cars spawned");
            var start = Snapshot(cars);
            yield return new WaitForSeconds(2f);
            var moving = CountMoved(cars, start, 2f);
            Debug.Log($"[NpcMovementTests] cars={cars.Length} moving={moving} blocked={System.Array.FindAll(cars, c => c != null && c.IsBlocked).Length}");
            Assert.Greater(moving, cars.Length / 2, "most traffic cars did not move");
        }

        [UnityTest]
        public IEnumerator Pedestrians_Move()
        {
            var peds = Object.FindObjectsByType<Pedestrian>();
            Assert.Greater(peds.Length, 5, "too few pedestrians spawned");
            var start = Snapshot(peds);
            yield return new WaitForSeconds(3f);
            var moving = CountMoved(peds, start, 0.8f);
            Debug.Log($"[NpcMovementTests] pedestrians={peds.Length} moving={moving}");
            Assert.Greater(moving, peds.Length / 2, "most pedestrians did not move");
        }

        private static Dictionary<Component, Vector3> Snapshot<T>(T[] items) where T : Component
        {
            var d = new Dictionary<Component, Vector3>();
            foreach (var i in items) d[i] = i.transform.position;
            return d;
        }

        private static int CountMoved<T>(T[] items, Dictionary<Component, Vector3> start, float minDistance) where T : Component
        {
            var n = 0;
            foreach (var i in items)
                if (i != null && Vector3.Distance(i.transform.position, start[i]) > minDistance) n++;
            return n;
        }
    }
}
