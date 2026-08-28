using UnityEngine;

namespace DeliveryBot.World
{
    /// <summary>Scene component that stores the parameters the city was built with and exposes the RoadGraph at runtime.</summary>
    public sealed class CityLayout : MonoBehaviour
    {
        public static CityLayout Instance { get; private set; }

        [SerializeField] private int blocks = 6;
        [SerializeField] private float blockSize = 24f;
        [SerializeField] private float roadWidth = 10f;
        [SerializeField] private float laneOffset = 2.6f;
        [SerializeField] private float sidewalkWidth = 2.5f;
        [SerializeField] private float sidewalkHeight = 0.12f;

        private RoadGraph _graph;

        public RoadGraph Graph => _graph ??= new RoadGraph(blocks, blockSize, roadWidth, laneOffset, sidewalkWidth);
        public float SidewalkHeight => sidewalkHeight;

        private void Awake() => Instance = this;

        public void Configure(int b, float block, float road, float lane, float sidewalk, float sidewalkH)
        {
            blocks = b;
            blockSize = block;
            roadWidth = road;
            laneOffset = lane;
            sidewalkWidth = sidewalk;
            sidewalkHeight = sidewalkH;
            _graph = null;
        }
    }
}
