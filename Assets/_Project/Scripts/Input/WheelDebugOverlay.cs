using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;

namespace DeliveryBot.Input
{
    /// <summary>
    /// Always shows a one-line input status in the editor (is the keyboard seen? what values arrive?).
    /// F1 expands it to every axis/button of every connected joystick with live values —
    /// use that the first time the G27 is plugged in to fill in the SteeringWheelProfile.
    /// </summary>
    public sealed class WheelDebugOverlay : MonoBehaviour
    {
        [SerializeField] private bool expanded;
        [SerializeField] private bool statusLineInEditor = true;
        [SerializeField] private DriveInputProvider provider;
        [SerializeField] private DeliveryBot.Vehicle.RobotController robot;

        private readonly StringBuilder _sb = new StringBuilder(2048);
        private GUIStyle _style;
        private string _lastKey = "-";

        private void Update()
        {
            if (robot == null && provider != null) robot = provider.GetComponent<DeliveryBot.Vehicle.RobotController>();
            var kb = Keyboard.current;
            if (kb == null) return;
            if (kb.f1Key.wasPressedThisFrame && !DeliveryBot.Delivery.GameFlow.MenuOpen) expanded = !expanded;
            foreach (var key in kb.allKeys)
                if (key.wasPressedThisFrame) { _lastKey = key.name; break; }
        }

        private void OnGUI()
        {
            var showStatus = expanded || (statusLineInEditor && Application.isEditor);
            if (!showStatus) return;
            _style ??= new GUIStyle(GUI.skin.box) { alignment = TextAnchor.UpperLeft, fontSize = 13, richText = true };

            _sb.Clear();
            if (provider != null)
            {
                var c = provider.Current;
                var pad = provider.GamepadName;
                var trig = provider.RawTriggers;
                _sb.Append($"kb={(provider.KeyboardPresent ? "ok" : "NONE")} pad={(pad ?? "none")}{(pad != null ? $" LT={trig.x:F2} RT={trig.y:F2}" : "")} lastKey={_lastKey} src={provider.ActiveSourceName} ")
                   .Append($"steer={c.Steer:F2} thr={c.Throttle:F2} brk={c.Brake:F2} rev={(c.Reverse ? 1 : 0)} hb={(c.Handbrake ? 1 : 0)}");
                if (robot != null) _sb.Append($" | spd={robot.ForwardSpeed:F1} grounded={(robot.IsGrounded ? 1 : 0)}");
                _sb.Append("  [F1 detail]");
            }

            if (expanded) AppendJoystickDetail();

            var height = expanded ? Screen.height - 20 : 28;
            GUILayout.BeginArea(new Rect(10, Screen.height - height - 10, 720, height));
            GUILayout.Box(_sb.ToString(), _style, GUILayout.ExpandWidth(true));
            GUILayout.EndArea();
        }

        private void AppendJoystickDetail()
        {
            _sb.AppendLine("\n<b>All input devices</b>");
            foreach (var d in InputSystem.devices)
                _sb.AppendLine($"  {d.displayName}  layout={d.layout}  {(d is Gamepad ? "[Gamepad]" : d is Joystick ? "[Joystick]" : "")}");

            var pad = Gamepad.current;
            if (pad != null)
            {
                _sb.AppendLine($"\n<b>{pad.displayName}</b> (layout={pad.layout}) pressed buttons: " +
                    string.Join(" ", System.Linq.Enumerable.Select(System.Linq.Enumerable.Where(pad.allControls, ctl => ctl is UnityEngine.InputSystem.Controls.ButtonControl b && b.isPressed), ctl => ctl.name)));
            }

            if (Joystick.all.Count == 0)
            {
                _sb.AppendLine("\nNo joystick / wheel connected. Keyboard: WASD or arrows, Shift=reverse, Space=handbrake, V=view, E=interact");
                return;
            }
            foreach (var joy in Joystick.all)
            {
                _sb.AppendLine($"\n<b>{joy.displayName}</b>  (layout={joy.layout}, path=<{joy.layout}> or <HID::{joy.displayName}>)");
                foreach (var control in joy.allControls)
                {
                    if (control.children.Count > 0) continue;
                    _sb.AppendLine($"  {control.path.Replace(joy.path, "")}  = {Format(control.ReadValueAsObject())}");
                }
            }
        }

        private static string Format(object v) => v switch
        {
            float f => f.ToString("F3"),
            Vector2 v2 => $"({v2.x:F3}, {v2.y:F3})",
            null => "-",
            _ => v.ToString()
        };
    }
}
