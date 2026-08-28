using UnityEngine;
using UnityEngine.InputSystem;

namespace DeliveryBot.Input
{
    /// <summary>
    /// Single entry point for driving input. Builds Input System actions in code so no
    /// .inputactions asset is required. Keyboard and gamepad always work; a steering
    /// wheel is layered on top when a Joystick device is connected, using the profile.
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

        private InputAction _kbSteer, _kbThrottle, _kbBrake, _kbReverse, _kbHandbrake, _kbInteract;
        private InputAction _wSteer, _wThrottle, _wBrake, _wReverse, _wHandbrake, _wInteract;

        public DriveInputState Current { get; private set; } = DriveInputState.None;
        public string ActiveSourceName => activeSource;
        public bool WheelConnected => Joystick.all.Count > 0;

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
            Current = WheelConnected ? Merge(kb, ReadWheel()) : kb;

            steer = Current.Steer;
            throttle = Current.Throttle;
            brake = Current.Brake;
        }

        private void BuildKeyboardAndGamepadActions()
        {
            _kbSteer = new InputAction("Steer", InputActionType.Value);
            _kbSteer.AddCompositeBinding("1DAxis")
                .With("Negative", "<Keyboard>/a").With("Positive", "<Keyboard>/d")
                .With("Negative", "<Keyboard>/leftArrow").With("Positive", "<Keyboard>/rightArrow");
            _kbSteer.AddBinding("<Gamepad>/leftStick/x");

            _kbThrottle = new InputAction("Throttle", InputActionType.Value);
            _kbThrottle.AddBinding("<Keyboard>/w");
            _kbThrottle.AddBinding("<Keyboard>/upArrow");
            _kbThrottle.AddBinding("<Gamepad>/rightTrigger");

            _kbBrake = new InputAction("Brake", InputActionType.Value);
            _kbBrake.AddBinding("<Keyboard>/s");
            _kbBrake.AddBinding("<Keyboard>/downArrow");
            _kbBrake.AddBinding("<Gamepad>/leftTrigger");

            _kbReverse = new InputAction("Reverse", InputActionType.Button);
            _kbReverse.AddBinding("<Keyboard>/leftShift");
            _kbReverse.AddBinding("<Gamepad>/buttonWest");

            _kbHandbrake = new InputAction("Handbrake", InputActionType.Button);
            _kbHandbrake.AddBinding("<Keyboard>/space");
            _kbHandbrake.AddBinding("<Gamepad>/buttonEast");

            _kbInteract = new InputAction("Interact", InputActionType.Button);
            _kbInteract.AddBinding("<Keyboard>/e");
            _kbInteract.AddBinding("<Gamepad>/buttonSouth");
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
            wheelProfile = p;
        }

        private static InputAction Axis(string name, string path)
        {
            var a = new InputAction(name, InputActionType.Value);
            if (!string.IsNullOrWhiteSpace(path)) a.AddBinding(path);
            return a;
        }

        private static InputAction Button(string name, string path)
        {
            var a = new InputAction(name, InputActionType.Button);
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
                _kbInteract.WasPressedThisFrame());
            if (Mathf.Abs(s.Steer) > 0.01f || s.Throttle > 0.01f || s.Brake > 0.01f)
                activeSource = Gamepad.current != null && _kbSteer.activeControl?.device is Gamepad ? "gamepad" : "keyboard";
            return s;
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
                _wInteract.WasPressedThisFrame());
            if (Mathf.Abs(s.Steer) > 0.01f || s.Throttle > 0.01f || s.Brake > 0.01f)
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
                kb.Interact || wheel.Interact);
        }

        private InputAction[] AllActions() => new[]
        {
            _kbSteer, _kbThrottle, _kbBrake, _kbReverse, _kbHandbrake, _kbInteract,
            _wSteer, _wThrottle, _wBrake, _wReverse, _wHandbrake, _wInteract
        };
    }
}
