using UnityEngine;

namespace DeliveryBot.Minimap
{
    /// <summary>Generates simple UI sprites at runtime so the prototype needs no image assets.</summary>
    public static class SpriteFactory
    {
        public static Sprite Circle(int size = 256)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { name = "CircleMask" };
            var r = size * 0.5f;
            var pixels = new Color32[size * size];
            for (var y = 0; y < size; y++)
            for (var x = 0; x < size; x++)
            {
                var dx = x + 0.5f - r;
                var dy = y + 0.5f - r;
                var inside = dx * dx + dy * dy <= r * r;
                pixels[y * size + x] = inside ? new Color32(255, 255, 255, 255) : new Color32(255, 255, 255, 0);
            }
            tex.SetPixels32(pixels);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
        }

        /// <summary>Upward pointing triangle.</summary>
        public static Sprite Triangle(int size = 64)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { name = "Triangle" };
            var pixels = new Color32[size * size];
            for (var y = 0; y < size; y++)
            for (var x = 0; x < size; x++)
            {
                var t = y / (float)size;                 // 0 bottom .. 1 top
                var halfWidth = (1f - t) * size * 0.5f;  // wide at bottom, point at top
                var inside = Mathf.Abs(x + 0.5f - size * 0.5f) <= halfWidth;
                pixels[y * size + x] = inside ? new Color32(255, 255, 255, 255) : new Color32(255, 255, 255, 0);
            }
            tex.SetPixels32(pixels);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
        }
    }
}
