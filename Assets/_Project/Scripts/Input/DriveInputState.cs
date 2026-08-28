namespace DeliveryBot.Input
{
    /// <summary>
    /// Device-agnostic snapshot of driving input for one frame.
    /// Produced by DriveInputProvider from keyboard / gamepad / steering wheel.
    /// </summary>
    public readonly struct DriveInputState
    {
        /// <summary>-1 (full left) .. +1 (full right)</summary>
        public readonly float Steer;
        /// <summary>0 .. 1</summary>
        public readonly float Throttle;
        /// <summary>0 .. 1</summary>
        public readonly float Brake;
        public readonly bool Reverse;
        public readonly bool Handbrake;
        public readonly bool Interact;

        public DriveInputState(float steer, float throttle, float brake, bool reverse, bool handbrake, bool interact)
        {
            Steer = steer;
            Throttle = throttle;
            Brake = brake;
            Reverse = reverse;
            Handbrake = handbrake;
            Interact = interact;
        }

        public static DriveInputState None => new DriveInputState(0f, 0f, 0f, false, false, false);
    }

    public interface IDriveInput
    {
        DriveInputState Current { get; }
        /// <summary>Human readable name of the device currently driving (for HUD/debug).</summary>
        string ActiveSourceName { get; }
    }
}
