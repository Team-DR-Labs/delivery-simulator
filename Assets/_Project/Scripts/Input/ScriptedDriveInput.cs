namespace DeliveryBot.Input
{
    /// <summary>Hand-set input source for tests and scripted sequences.</summary>
    public sealed class ScriptedDriveInput : IDriveInput
    {
        public float Steer;
        public float Throttle;
        public float Brake;
        public bool Reverse;
        public bool Handbrake;
        /// <summary>Set true for exactly one frame to simulate an Interact press.</summary>
        public bool Interact;

        public DriveInputState Current => new DriveInputState(Steer, Throttle, Brake, Reverse, Handbrake, Interact, false);
        public string ActiveSourceName => "scripted";
    }
}
