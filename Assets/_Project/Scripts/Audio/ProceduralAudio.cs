using UnityEngine;

namespace DeliveryBot.Audio
{
    /// <summary>Generates all sound effects in code so the prototype ships without audio files.</summary>
    public static class ProceduralAudio
    {
        private const int Rate = 44100;

        /// <summary>Seamless 1 s motor hum: two low sines plus a little noise.</summary>
        public static AudioClip MotorLoop()
        {
            var n = Rate;
            var data = new float[n];
            var rng = new System.Random(1);
            for (var i = 0; i < n; i++)
            {
                var t = i / (float)Rate;
                var v = Mathf.Sin(2f * Mathf.PI * 70f * t) * 0.5f
                      + Mathf.Sin(2f * Mathf.PI * 140f * t) * 0.3f
                      + Mathf.Sin(2f * Mathf.PI * 210f * t) * 0.1f
                      + ((float)rng.NextDouble() * 2f - 1f) * 0.08f;
                data[i] = v * 0.6f;
            }
            return Make("Motor", data);
        }

        /// <summary>Short sequence of decaying sine notes.</summary>
        public static AudioClip Chime(float[] freqs, float noteSeconds = 0.13f)
        {
            var perNote = (int)(Rate * noteSeconds);
            var data = new float[perNote * freqs.Length + Rate / 4];
            for (var k = 0; k < freqs.Length; k++)
            for (var i = 0; i < perNote + Rate / 4 && k * perNote + i < data.Length; i++)
            {
                var t = i / (float)Rate;
                var env = Mathf.Exp(-t * 9f);
                data[k * perNote + i] += Mathf.Sin(2f * Mathf.PI * freqs[k] * t) * env * 0.5f;
            }
            return Make("Chime", data);
        }

        public static AudioClip Pickup() => Chime(new[] { 659.25f, 880f });
        public static AudioClip Delivered() => Chime(new[] { 523.25f, 659.25f, 783.99f, 1046.5f }, 0.11f);

        /// <summary>Dull thud: noise burst + low sine, fast decay.</summary>
        public static AudioClip Bump()
        {
            var n = Rate / 4;
            var data = new float[n];
            var rng = new System.Random(7);
            for (var i = 0; i < n; i++)
            {
                var t = i / (float)Rate;
                var env = Mathf.Exp(-t * 18f);
                data[i] = (((float)rng.NextDouble() * 2f - 1f) * 0.5f + Mathf.Sin(2f * Mathf.PI * 60f * t) * 0.6f) * env;
            }
            return Make("Bump", data);
        }

        public static AudioClip Horn()
        {
            var n = (int)(Rate * 0.35f);
            var data = new float[n];
            for (var i = 0; i < n; i++)
            {
                var t = i / (float)Rate;
                var env = Mathf.Min(1f, t * 40f) * Mathf.Min(1f, (n - i) / (Rate * 0.05f));
                data[i] = (Mathf.Sin(2f * Mathf.PI * 420f * t) + Mathf.Sin(2f * Mathf.PI * 520f * t)) * 0.25f * env;
            }
            return Make("Horn", data);
        }

        private static AudioClip Make(string name, float[] data)
        {
            var clip = AudioClip.Create(name, data.Length, 1, Rate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
