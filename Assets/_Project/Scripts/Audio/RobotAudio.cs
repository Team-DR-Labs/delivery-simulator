using DeliveryBot.Input;
using DeliveryBot.Vehicle;
using UnityEngine;

namespace DeliveryBot.Audio
{
    /// <summary>Motor hum whose pitch and volume follow speed and throttle.</summary>
    public sealed class RobotAudio : MonoBehaviour
    {
        [SerializeField] private RobotController robot;
        [SerializeField] private DriveInputProvider input;
        [SerializeField] private float baseVolume = 0.08f;
        [SerializeField] private float maxVolume = 0.4f;

        private AudioSource _source;

        private void Awake()
        {
            if (robot == null) robot = GetComponent<RobotController>();
            if (input == null) input = GetComponent<DriveInputProvider>();
            _source = gameObject.AddComponent<AudioSource>();
            _source.clip = ProceduralAudio.MotorLoop();
            _source.loop = true;
            _source.spatialBlend = 0f;
            _source.volume = baseVolume;
            _source.Play();
        }

        private void Update()
        {
            var speed01 = robot != null ? Mathf.Clamp01(Mathf.Abs(robot.ForwardSpeed) / Mathf.Max(0.01f, robot.MaxForwardSpeed)) : 0f;
            var throttle = input != null ? input.Current.Throttle : 0f;
            _source.pitch = Mathf.Lerp(_source.pitch, 0.7f + speed01 * 0.9f + throttle * 0.15f, Time.deltaTime * 4f);
            _source.volume = Mathf.Lerp(_source.volume, Mathf.Lerp(baseVolume, maxVolume, Mathf.Max(speed01, throttle * 0.6f)), Time.deltaTime * 4f);
        }
    }
}
