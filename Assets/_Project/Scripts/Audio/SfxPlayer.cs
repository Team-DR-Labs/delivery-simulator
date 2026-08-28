using UnityEngine;

namespace DeliveryBot.Audio
{
    /// <summary>One-shot sound effects, generated lazily.</summary>
    public sealed class SfxPlayer : MonoBehaviour
    {
        public static SfxPlayer Instance { get; private set; }

        private AudioSource _source;
        private AudioClip _pickup, _delivered, _bump, _horn;

        private void Awake()
        {
            Instance = this;
            _source = gameObject.AddComponent<AudioSource>();
            _source.spatialBlend = 0f;
        }

        public void PlayPickup() => Play(_pickup ??= ProceduralAudio.Pickup(), 0.8f);
        public void PlayDelivered() => Play(_delivered ??= ProceduralAudio.Delivered(), 0.9f);
        public void PlayBump() => Play(_bump ??= ProceduralAudio.Bump(), 0.9f);
        public void PlayHorn() => Play(_horn ??= ProceduralAudio.Horn(), 0.5f);

        private void Play(AudioClip clip, float volume)
        {
            if (clip != null) _source.PlayOneShot(clip, volume);
        }
    }
}
