using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

namespace DeliveryBot.EditorTools
{
    /// <summary>Lists every device the Input System sees with its layout — to check whether a pad/wheel is recognised.</summary>
    public static class InputDeviceLogger
    {
        [MenuItem("DeliveryBot/Log Input Devices")]
        public static void Log()
        {
            var sb = new StringBuilder("[InputDevices] ");
            sb.Append(InputSystem.devices.Count).AppendLine(" device(s):");
            foreach (var d in InputSystem.devices)
                sb.AppendLine($"  - {d.displayName} | layout={d.layout} | interface={d.description.interfaceName} | product={d.description.product} | isGamepad={d is Gamepad} | isJoystick={d is Joystick}");
            Debug.Log(sb.ToString());
        }
    }
}
