using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;

namespace DeliveryBot.Input
{
    /// <summary>
    /// Press F1 to show every axis/button of every connected joystick with its live value.
    /// Use it the first time the G27 is plugged in to find the correct control paths
    /// for the SteeringWheelProfile asset.
    /// </summary>
    public sealed class WheelDebugOverlay : MonoBehaviour
    {
        [SerializeField] private bool visible;
        [SerializeField] private DriveInputProvider provider;

        private readonly StringBuilder _sb = new StringBuilder(2048);
        private GUIStyle _style;

        private void Update()
        {
            if (Keyboard.current != null && Keyboard.current.f1Key.wasPressedThisFrame) visible = !visible;
        }

        private void OnGUI()
        {
            if (!visible) return;
            _style ??= new GUIStyle(GUI.skin.box) { alignment = TextAnchor.UpperLeft, fontSize = 13, richText = true };

            _sb.Clear();
            _sb.AppendLine("<b>[F1] Wheel debug</b>");
            if (provider != null)
            {
                var c = provider.Current;
                _sb.AppendLine($"source={provider.ActiveSourceName}  steer={c.Steer:F2}  throttle={c.Throttle:F2}  brake={c.Brake:F2}  rev={c.Reverse}  hb={c.Handbrake}");
            }

            if (Joystick.all.Count == 0)
            {
                _sb.AppendLine("No joystick / wheel connected. Keyboard: WASD or arrows, Shift=reverse, Space=handbrake, E=interact");
            }

            foreach (var joy in Joystick.all)
            {
                _sb.AppendLine($"\n<b>{joy.displayName}</b>  (layout={joy.layout}, path=<{joy.layout}> or <HID::{joy.displayName}>)");
                foreach (var control in joy.allControls)
                {
                    if (control.children.Count > 0) continue; // print leaves only
                    var v = control.ReadValueAsObject();
                    _sb.AppendLine($"  {control.path.Replace(joy.path, "")}  = {Format(v)}");
                }
            }

            GUILayout.BeginArea(new Rect(10, 10, 640, Screen.height - 20));
            GUILayout.Box(_sb.ToString(), _style, GUILayout.ExpandWidth(true));
            GUILayout.EndArea();
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
