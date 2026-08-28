using System.Collections.Generic;
using DeliveryBot.World;
using UnityEngine;

namespace DeliveryBot.Traffic
{
    /// <summary>Populates the road graph with cars at random lane positions, keeping a minimum spacing.</summary>
    public sealed class TrafficSpawner : MonoBehaviour
    {
        [SerializeField] private GameObject[] carPrefabs;
        [SerializeField] private int count = 26;
        [SerializeField] private float minSpacing = 14f;
        [SerializeField] private Vector2 speedRange = new Vector2(5f, 8f);
        [SerializeField] private Color[] palette =
        {
            new Color(0.9f, 0.2f, 0.2f), new Color(0.2f, 0.45f, 0.9f), new Color(0.95f, 0.95f, 0.95f),
            new Color(0.15f, 0.15f, 0.17f), new Color(0.95f, 0.75f, 0.1f), new Color(0.2f, 0.7f, 0.4f),
            new Color(0.6f, 0.6f, 0.65f), new Color(0.8f, 0.4f, 0.7f)
        };

        private readonly List<Vector3> _placed = new List<Vector3>();

        private void Start()
        {
            var layout = CityLayout.Instance;
            if (layout == null || carPrefabs == null || carPrefabs.Length == 0) return;
            var g = layout.Graph;
            var rng = new System.Random();
            var parent = new GameObject("TrafficCars").transform;

            var attempts = 0;
            while (_placed.Count < count && attempts++ < count * 20)
            {
                var from = new RoadGraph.Node(rng.Next(g.NodesPerAxis), rng.Next(g.NodesPerAxis));
                var neighbors = g.Neighbors(from);
                var to = neighbors[rng.Next(neighbors.Count)];
                var dist = (float)rng.NextDouble() * g.EdgeLength;
                var pos = g.LanePoint(from, to, dist / g.EdgeLength);
                if (TooClose(pos)) continue;

                var prefab = carPrefabs[rng.Next(carPrefabs.Length)];
                var go = Instantiate(prefab, parent);
                var car = go.GetComponent<TrafficCar>();
                var speed = Mathf.Lerp(speedRange.x, speedRange.y, (float)rng.NextDouble());
                car.Init(g, from, to, dist, speed, rng.Next());
                car.SetColor(palette[rng.Next(palette.Length)]);
                _placed.Add(pos);
            }
        }

        private bool TooClose(Vector3 pos)
        {
            foreach (var p in _placed)
                if (Vector3.Distance(p, pos) < minSpacing) return true;
            return false;
        }

        public void SetPrefabs(GameObject[] prefabs) => carPrefabs = prefabs;
    }
}
