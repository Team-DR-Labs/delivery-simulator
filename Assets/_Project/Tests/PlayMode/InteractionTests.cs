using System.Collections;
using DeliveryBot.Delivery;
using DeliveryBot.Input;
using DeliveryBot.Vehicle;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace DeliveryBot.Tests
{
    /// <summary>Pickup at a shop and drop-off at a home must complete when Interact is pressed in range.</summary>
    public class InteractionTests
    {
        private RobotController _robot;
        private DeliveryManager _manager;
        private ScriptedDriveInput _input;

        [UnitySetUp]
        public IEnumerator LoadCity()
        {
            yield return SceneManager.LoadSceneAsync("City", LoadSceneMode.Single);
            yield return new WaitForSeconds(1.5f);
            _robot = Object.FindAnyObjectByType<RobotController>();
            _manager = DeliveryManager.Instance;
            _input = new ScriptedDriveInput();
            _robot.SetInputSource(_input);
            Assert.AreEqual(DeliveryPhase.ToPickup, _manager.Phase);
            Assert.AreEqual(PointKind.Shop, _manager.Target.Kind, "first target must be a shop");
        }

        [UnityTest]
        public IEnumerator PickupThenDropoff_CompletesDelivery()
        {
            yield return DriveToTargetAndInteract();
            Assert.AreEqual(DeliveryPhase.ToDropoff, _manager.Phase, "pickup did not switch to drop-off");
            Assert.AreEqual(PointKind.Home, _manager.Target.Kind, "drop-off target must be a home");

            yield return DriveToTargetAndInteract();
            Assert.AreEqual(1, _manager.Completed, "delivery was not counted");
            Assert.AreEqual(DeliveryPhase.ToPickup, _manager.Phase, "next job did not start");
        }

        [UnityTest]
        public IEnumerator Interact_OutsideRange_DoesNothing()
        {
            yield return PressInteract();
            Assert.AreEqual(DeliveryPhase.ToPickup, _manager.Phase);
        }

        private IEnumerator DriveToTargetAndInteract()
        {
            var target = _manager.Target.transform;
            // Teleport to the road in front of the doorstep, then wait for the trigger.
            var pos = target.position + target.forward * 3f + Vector3.up * 0.05f;
            _robot.Place(pos, Quaternion.LookRotation(-target.forward, Vector3.up));
            yield return new WaitForFixedUpdate();
            yield return new WaitForSeconds(0.3f);
            Assert.IsTrue(_manager.IsTargetInRange, "robot not detected in range of target");
            yield return PressInteract();
        }

        private IEnumerator PressInteract()
        {
            _input.Interact = true;
            yield return null;
            yield return null;
            _input.Interact = false;
            yield return null;
        }
    }
}
