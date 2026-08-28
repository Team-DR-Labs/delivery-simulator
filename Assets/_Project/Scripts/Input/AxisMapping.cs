using System;
using UnityEngine;

namespace DeliveryBot.Input
{
    /// <summary>
    /// Maps one raw joystick axis to a normalized value.
    /// Pedals on Logitech wheels typically rest at +1 and go to -1 when pressed,
    /// so the default rest/pressed pair is (1, -1).
    /// </summary>
    [Serializable]
    public sealed class PedalAxisMapping
    {
        [Tooltip("Input System control path, e.g. <Joystick>/stick/y or <HID::Logitech G27 Racing Wheel>/rz")]
        public string controlPath = "<Joystick>/stick/y";

        [Tooltip("Raw axis value when the pedal is released")]
        public float restValue = 1f;

        [Tooltip("Raw axis value when the pedal is fully pressed")]
        public float pressedValue = -1f;

        [Range(0f, 0.3f)] public float deadzone = 0.03f;

        /// <summary>Returns 0 (released) .. 1 (fully pressed).</summary>
        public float Normalize(float raw)
        {
            var t = Mathf.InverseLerp(restValue, pressedValue, raw);
            return t < deadzone ? 0f : Mathf.Clamp01(t);
        }
    }

    [Serializable]
    public sealed class SteerAxisMapping
    {
        [Tooltip("Input System control path for the wheel rotation axis")]
        public string controlPath = "<Joystick>/stick/x";

        public bool invert;

        [Range(0f, 0.2f)] public float deadzone = 0.01f;

        [Tooltip("Physical lock-to-lock range reported by the driver (G27 default = 900°)")]
        public float wheelRangeDegrees = 900f;

        [Tooltip("How many degrees of wheel rotation map to full steering lock in game")]
        public float usedRangeDegrees = 270f;

        /// <summary>Returns -1 .. 1 with deadzone and range scaling applied.</summary>
        public float Normalize(float raw)
        {
            var v = invert ? -raw : raw;
            if (Mathf.Abs(v) < deadzone) return 0f;
            var scale = usedRangeDegrees > 0f ? wheelRangeDegrees / usedRangeDegrees : 1f;
            return Mathf.Clamp(v * scale, -1f, 1f);
        }
    }
}
