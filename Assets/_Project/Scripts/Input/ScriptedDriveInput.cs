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

        public DriveInputState Current => new DriveInputState(Steer, Throttle, Brake, Reverse, Handbrake, false, false);
        public string ActiveSourceName => "scripted";
    }
}
