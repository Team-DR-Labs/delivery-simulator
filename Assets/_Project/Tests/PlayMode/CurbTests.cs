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
    /// <summary>The robot must be able to drive from the road up onto the sidewalk to a doorstep.</summary>
    public class CurbTests
    {
        [UnitySetUp]
        public IEnumerator LoadCity()
        {
            yield return SceneManager.LoadSceneAsync("City", LoadSceneMode.Single);
            yield return new WaitForSeconds(1.5f);
            yield return RoundTestKit.BeginRound();
        }

        [UnityTest]
        public IEnumerator Robot_ClimbsCurb_ToDoorstep()
        {
            var robot = Object.FindAnyObjectByType<RobotController>();
            var target = DeliveryManager.Instance.Target.transform;
            // Start on the road 7 m out from the doorstep, facing it.
            var start = target.position + target.forward * 7f;
            start.y = 0.05f;
            robot.Place(start, Quaternion.LookRotation(-target.forward, Vector3.up));
            var input = new ScriptedDriveInput { Throttle = 0.5f };
            robot.SetInputSource(input);

            var maxY = 0f;
            var elapsed = 0f;
            while (elapsed < 4f)
            {
                yield return new WaitForFixedUpdate();
                elapsed += Time.fixedDeltaTime;
                maxY = Mathf.Max(maxY, robot.transform.position.y);
                if (Vector3.Distance(robot.transform.position, target.position) < 1.5f) break;
            }
            var dist = Vector3.Distance(robot.transform.position, target.position);
            Debug.Log($"[CurbTests] dist={dist:F2} maxY={maxY:F3} y={robot.transform.position.y:F3}");
            Assert.Less(dist, 2.5f, "robot was stopped before reaching the doorstep (curb blocked it)");
            Assert.Greater(maxY, 0.09f, "robot never rose onto the sidewalk");
        }
    }
}
