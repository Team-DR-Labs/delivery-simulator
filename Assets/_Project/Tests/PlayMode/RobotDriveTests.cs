using System.Collections;
using DeliveryBot.Input;
using DeliveryBot.Traffic;
using DeliveryBot.Vehicle;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace DeliveryBot.Tests
{
    /// <summary>Loads the real City scene and drives the robot with scripted input.</summary>
    public class RobotDriveTests
    {
        private RobotController _robot;

        [UnitySetUp]
        public IEnumerator LoadCity()
        {
            yield return SceneManager.LoadSceneAsync("City", LoadSceneMode.Single);
            yield return null;
            _robot = Object.FindAnyObjectByType<RobotController>();
            Assert.IsNotNull(_robot, "Robot not found in City scene");
            yield return new WaitForSeconds(1.5f); // let DeliveryManager spawn the robot and physics settle
            yield return RoundTestKit.BeginRound();
            // Deterministic road: remove traffic and pedestrians so nothing blocks the robot.
            foreach (var car in Object.FindObjectsByType<TrafficCar>()) Object.Destroy(car.gameObject);
            foreach (var ped in Object.FindObjectsByType<Pedestrian>()) Object.Destroy(ped.gameObject);
            yield return null;
        }

        [UnityTest]
        public IEnumerator Throttle_MovesRobotForward()
        {
            var input = new ScriptedDriveInput { Throttle = 1f };
            _robot.SetInputSource(input);
            var start = _robot.transform.position;
            var forward = _robot.transform.forward;
            yield return new WaitForSeconds(2f);
            var moved = Vector3.Dot(_robot.transform.position - start, forward);
            Debug.Log($"[RobotDriveTests] grounded={_robot.IsGrounded} speed={_robot.ForwardSpeed:F2} moved={moved:F2} y={_robot.transform.position.y:F3}");
            Assert.IsTrue(_robot.IsGrounded, "robot never registered as grounded");
            Assert.Greater(moved, 6f, "robot did not move forward under full throttle");
            Assert.Greater(_robot.ForwardSpeed, 5f, "robot did not reach cruising speed");
        }

        [UnityTest]
        public IEnumerator HoldingBrake_FromStandstill_Reverses()
        {
            var input = new ScriptedDriveInput { Brake = 1f };
            _robot.SetInputSource(input);
            var start = _robot.transform.position;
            var forward = _robot.transform.forward;
            yield return new WaitForSeconds(2f);
            var moved = Vector3.Dot(_robot.transform.position - start, forward);
            Debug.Log($"[RobotDriveTests] reverse moved={moved:F2} speed={_robot.ForwardSpeed:F2}");
            Assert.Less(moved, -1f, "robot did not reverse while holding brake from standstill");
        }

        [UnityTest]
        public IEnumerator Steering_TurnsRobot()
        {
            var input = new ScriptedDriveInput { Throttle = 1f, Steer = 1f };
            _robot.SetInputSource(input);
            var startYaw = _robot.transform.eulerAngles.y;
            yield return new WaitForSeconds(1.5f);
            var delta = Mathf.DeltaAngle(startYaw, _robot.transform.eulerAngles.y);
            Assert.Greater(delta, 20f, "robot did not turn right");
        }
    }
}
