using UnityEngine;
using UnityEngine.InputSystem;

namespace DeliveryBot.Input
{
    /// <summary>
    /// Single entry point for driving input. Builds Input System actions in code so no
    /// .inputactions asset is required. Keyboard and gamepad always work; a steering
    /// wheel is layered on top when a Joystick device is connected, using the profile.
    /// A legacy Input.GetKey fallback guarantees WASD works even if the Input System
    /// backend delivers nothing (project uses activeInputHandler = Both).
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public sealed class DriveInputProvider : MonoBehaviour, IDriveInput
    {
        [SerializeField] private SteeringWheelProfile wheelProfile;

        [Header("Debug (read only)")]
        [SerializeField] private float steer;
        [SerializeField] private float throttle;
        [SerializeField] private float brake;
        [SerializeField] private string activeSource = "none";

        private InputAction _kbSteer, _kbThrottle, _kbBrake, _kbReverse, _kbHandbrake, _kbInteract, _kbView;
        private InputAction _wSteer, _wThrottle, _wBrake, _wReverse, _wHandbrake, _wInteract, _wView;

        public DriveInputState Current { get; private set; } = DriveInputState.None;
        public string ActiveSourceName => activeSource;
        public bool WheelConnected => Joystick.all.Count > 0;
        public bool KeyboardPresent => Keyboard.current != null;

        private void Awake()
        {
            BuildKeyboardAndGamepadActions();
            BuildWheelActions();
        }

        private void OnEnable()
        {
            foreach (var a in AllActions()) a?.Enable();
        }

        private void OnDisable()
        {
            foreach (var a in AllActions()) a?.Disable();
        }

        private void Update()
        {
            var kb = ReadKeyboardAndGamepad();
            if (!kb.HasAnalogInput) kb = MergeLegacyKeyboard(kb);
            Current = WheelConnected ? Merge(kb, ReadWheel()) : kb;

            steer = Current.Steer;
            throttle = Current.Throttle;
            brake = Current.Brake;
        }

        private void BuildKeyboardAndGamepadActions()
        {
            _kbSteer = new InputAction("Steer", InputActionType.Value);
            _kbSteer.AddCompositeBinding("1DAxis")
                .With("Negative", "<Keyboard>/a").With("Positive", "<Keyboard>/d");
            _kbSteer.AddCompositeBinding("1DAxis")
                .With("Negative", "<Keyboard>/leftArrow").With("Positive", "<Keyboard>/rightArrow");
            _kbSteer.AddBinding("<Gamepad>/leftStick/x");

            _kbThrottle = Axis("Throttle", "<Keyboard>/w", "<Keyboard>/upArrow", "<Gamepad>/rightTrigger");
            _kbBrake = Axis("Brake", "<Keyboard>/s", "<Keyboard>/downArrow", "<Gamepad>/leftTrigger");
            _kbReverse = Button("Reverse", "<Keyboard>/leftShift", "<Keyboard>/rightShift", "<Gamepad>/buttonWest");
            _kbHandbrake = Button("Handbrake", "<Keyboard>/space", "<Gamepad>/buttonEast");
            _kbInteract = Button("Interact", "<Keyboard>/e", "<Gamepad>/buttonSouth");
            _kbView = Button("ToggleView", "<Keyboard>/v", "<Gamepad>/rightStickPress");
        }

        private void BuildWheelActions()
        {
            var p = wheelProfile != null ? wheelProfile : ScriptableObject.CreateInstance<SteeringWheelProfile>();
            _wSteer = Axis("WheelSteer", p.steer.controlPath);
            _wThrottle = Axis("WheelThrottle", p.throttle.controlPath);
            _wBrake = Axis("WheelBrake", p.brake.controlPath);
            _wReverse = Button("WheelReverse", p.reverseButtonPath);
            _wHandbrake = Button("WheelHandbrake", p.handbrakeButtonPath);
            _wInteract = Button("WheelInteract", p.interactButtonPath);
            _wView = Button("WheelView", p.viewButtonPath);
            wheelProfile = p;
        }

        private static InputAction Axis(string name, params string[] paths)
        {
            var a = new InputAction(name, InputActionType.Value);
            foreach (var path in paths)
                if (!string.IsNullOrWhiteSpace(path)) a.AddBinding(path);
            return a;
        }

        private static InputAction Button(string name, params string[] paths)
        {
            var a = new InputAction(name, InputActionType.Button);
            foreach (var path in paths)
                if (!string.IsNullOrWhiteSpace(path)) a.AddBinding(path);
            return a;
        }

        private DriveInputState ReadKeyboardAndGamepad()
        {
            var s = new DriveInputState(
                Mathf.Clamp(_kbSteer.ReadValue<float>(), -1f, 1f),
                Mathf.Clamp01(_kbThrottle.ReadValue<float>()),
                Mathf.Clamp01(_kbBrake.ReadValue<float>()),
                _kbReverse.IsPressed(),
                _kbHandbrake.IsPressed(),
                _kbInteract.WasPressedThisFrame(),
                _kbView.WasPressedThisFrame());
            if (s.HasAnalogInput)
                activeSource = _kbSteer.activeControl?.device is Gamepad || _kbThrottle.activeControl?.device is Gamepad ? "gamepad" : "keyboard";
            return s;
        }

        /// <summary>Legacy Input Manager fallback so WASD/arrows always work.</summary>
        private DriveInputState MergeLegacyKeyboard(DriveInputState s)
        {
            var legacySteer = (LegacyKey(KeyCode.D) || LegacyKey(KeyCode.RightArrow) ? 1f : 0f)
                            - (LegacyKey(KeyCode.A) || LegacyKey(KeyCode.LeftArrow) ? 1f : 0f);
            var legacyThrottle = LegacyKey(KeyCode.W) || LegacyKey(KeyCode.UpArrow) ? 1f : 0f;
            var legacyBrake = LegacyKey(KeyCode.S) || LegacyKey(KeyCode.DownArrow) ? 1f : 0f;
            if (legacySteer == 0f && legacyThrottle == 0f && legacyBrake == 0f) return s;

            activeSource = "keyboard(legacy)";
            return new DriveInputState(legacySteer, legacyThrottle, legacyBrake,
                s.Reverse || LegacyKey(KeyCode.LeftShift), s.Handbrake || LegacyKey(KeyCode.Space), s.Interact, s.ToggleView);
        }

        private static bool LegacyKey(KeyCode key)
        {
            try { return UnityEngine.Input.GetKey(key); }
            catch (System.InvalidOperationException) { return false; } // Input Manager disabled
        }

        private DriveInputState ReadWheel()
        {
            var p = wheelProfile;
            var s = new DriveInputState(
                p.steer.Normalize(_wSteer.ReadValue<float>()),
                p.throttle.Normalize(_wThrottle.ReadValue<float>()),
                p.brake.Normalize(_wBrake.ReadValue<float>()),
                _wReverse.IsPressed(),
                _wHandbrake.IsPressed(),
                _wInteract.WasPressedThisFrame(),
                _wView.WasPressedThisFrame());
            if (s.HasAnalogInput)
                activeSource = Joystick.current != null ? Joystick.current.displayName : "wheel";
            return s;
        }

        /// <summary>Wheel wins for analog axes when it is being used; buttons are OR-ed.</summary>
        private static DriveInputState Merge(DriveInputState kb, DriveInputState wheel)
        {
            var steerValue = Mathf.Abs(wheel.Steer) > Mathf.Abs(kb.Steer) ? wheel.Steer : kb.Steer;
            return new DriveInputState(
                steerValue,
                Mathf.Max(kb.Throttle, wheel.Throttle),
                Mathf.Max(kb.Brake, wheel.Brake),
                kb.Reverse || wheel.Reverse,
                kb.Handbrake || wheel.Handbrake,
                kb.Interact || wheel.Interact,
                kb.ToggleView || wheel.ToggleView);
        }

        private InputAction[] AllActions() => new[]
        {
            _kbSteer, _kbThrottle, _kbBrake, _kbReverse, _kbHandbrake, _kbInteract, _kbView,
            _wSteer, _wThrottle, _wBrake, _wReverse, _wHandbrake, _wInteract, _wView
        };
    }
}
