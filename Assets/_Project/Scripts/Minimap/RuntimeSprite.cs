using UnityEngine;
using UnityEngine.UI;

namespace DeliveryBot.Minimap
{
    /// <summary>Assigns a procedurally generated sprite to an Image on Awake.</summary>
    [RequireComponent(typeof(Image))]
    public sealed class RuntimeSprite : MonoBehaviour
    {
        public enum Shape { Circle, Triangle }

        [SerializeField] private Shape shape = Shape.Circle;

        private void Awake()
        {
            GetComponent<Image>().sprite = shape == Shape.Circle ? SpriteFactory.Circle() : SpriteFactory.Triangle();
        }
    }
}
