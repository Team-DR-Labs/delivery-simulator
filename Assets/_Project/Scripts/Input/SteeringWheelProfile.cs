using UnityEngine;

namespace DeliveryBot.Input
{
    /// <summary>
    /// Per-device steering wheel configuration. Edit this asset (no code change) once the
    /// real G27 is plugged in and the WheelDebugOverlay (F1) shows which axis is which.
    /// </summary>
    [CreateAssetMenu(menuName = "DeliveryBot/Steering Wheel Profile", fileName = "SteeringWheelProfile")]
    public sealed class SteeringWheelProfile : ScriptableObject
    {
        [Header("Axes")]
        public SteerAxisMapping steer = new SteerAxisMapping();
        // G27 (Input System HID layout, measured 2026-09-05): wheel = stick/x, accelerator = rz, brake = z,
        // clutch = slider (all pedals rest at +1; stick/y stays 0 and must not be used as a pedal),
        // top-right rim button = button7.
        public PedalAxisMapping throttle = new PedalAxisMapping { controlPath = "<Joystick>/rz" };
        public PedalAxisMapping brake = new PedalAxisMapping { controlPath = "<Joystick>/z" };
        public PedalAxisMapping clutch = new PedalAxisMapping { controlPath = "<Joystick>/slider" };

        [Header("Buttons (Input System control paths)")]
        public string interactButtonPath = "<Joystick>/button7";
        public string reverseButtonPath = "<Joystick>/button2";
        public string handbrakeButtonPath = "<Joystick>/button3";
        public string viewButtonPath = "<Joystick>/button4";
    }
}
