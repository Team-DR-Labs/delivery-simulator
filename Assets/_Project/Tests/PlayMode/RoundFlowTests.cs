using System.Collections;
using System.IO;
using DeliveryBot.Delivery;
using DeliveryBot.Input;
using DeliveryBot.Vehicle;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace DeliveryBot.Tests
{
    /// <summary>Nickname → 180 s round → results, driven through GameFlow without any keyboard.</summary>
    public class RoundFlowTests
    {
        private GameFlow _flow;
        private DeliveryManager _manager;
        private RobotController _robot;

        [UnitySetUp]
        public IEnumerator LoadCity()
        {
            RoundTestKit.UseTempLeaderboard();
            RoundTestKit.ResetTempLeaderboard();
            yield return SceneManager.LoadSceneAsync("City", LoadSceneMode.Single);
            yield return new WaitForSeconds(1.5f);
            _flow = GameFlow.Instance;
            _manager = DeliveryManager.Instance;
            _robot = Object.FindAnyObjectByType<RobotController>();
            Assert.IsNotNull(_flow, "City scene has no GameFlow — rebuild the scene (DeliveryBot > Build City Scene)");
        }

        [UnityTearDown]
        public IEnumerator Cleanup()
        {
            RoundTestKit.ResetTempLeaderboard();
            yield return null;
        }

        [UnityTest]
        public IEnumerator SceneStartsInNameEntry_RobotFrozen()
        {
            Assert.AreEqual(FlowState.NameEntry, _flow.State);
            Assert.IsTrue(GameFlow.MenuOpen);
            Assert.AreEqual(DeliveryPhase.Idle, _manager.Phase, "no job may start before a nickname is entered");
            Assert.IsFalse(_robot.InputSource is DriveInputProvider, "robot must not read live input on the nickname screen");
            yield return null;
        }

        [UnityTest]
        public IEnumerator BeginRound_StartsJobAndRestoresInput()
        {
            _flow.BeginRound("  tester  ");
            yield return null;
            Assert.AreEqual(FlowState.Playing, _flow.State);
            Assert.AreEqual("tester", _flow.Nickname);
            Assert.AreEqual(DeliveryPhase.ToPickup, _manager.Phase);
            Assert.IsTrue(_robot.InputSource is DriveInputProvider, "live input must be restored for the round");
            Assert.AreEqual(_flow.RoundSeconds, _flow.Remaining, 0.5f);
        }

        [UnityTest]
        public IEnumerator Expiry_EndsRound_WritesEntry()
        {
            _flow.BeginRound("t");
            yield return null;
            _flow.SkipToRemaining(0.05f);
            yield return new WaitForSeconds(0.3f);
            Assert.AreEqual(FlowState.Results, _flow.State);
            Assert.AreEqual(DeliveryPhase.Idle, _manager.Phase);
            Assert.IsFalse(_robot.InputSource is DriveInputProvider, "robot must be frozen on the results screen");
            Assert.AreEqual(1, _flow.Board.entries.Count);
            Assert.AreEqual(0, _flow.LastRank);
            Assert.AreEqual("t", _flow.LastEntry.name);
            Assert.IsTrue(File.Exists(RoundTestKit.TempLeaderboardPath), "leaderboard file was not written");
        }

        [UnityTest]
        public IEnumerator Penalty_BurnsRemainingTime()
        {
            _flow.BeginRound("p");
            yield return null;
            var before = _flow.Remaining;
            _manager.AddPenalty(5f, "test");
            Assert.AreEqual(before - 5f, _flow.Remaining, 0.1f);
        }
    }
}
