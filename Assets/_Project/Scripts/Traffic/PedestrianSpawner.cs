using DeliveryBot.World;
using UnityEngine;

namespace DeliveryBot.Traffic
{
    /// <summary>Spawns pedestrians on random block sidewalks with random clothing colours.</summary>
    public sealed class PedestrianSpawner : MonoBehaviour
    {
        [SerializeField] private GameObject pedestrianPrefab;
        [SerializeField] private int count = 40;
        [SerializeField] private Color[] shirtPalette =
        {
            new Color(0.9f, 0.3f, 0.3f), new Color(0.3f, 0.5f, 0.9f), new Color(0.95f, 0.85f, 0.3f),
            new Color(0.3f, 0.8f, 0.5f), new Color(0.9f, 0.9f, 0.9f), new Color(0.6f, 0.3f, 0.7f), new Color(0.2f, 0.2f, 0.25f)
        };

        private void Start()
        {
            var layout = CityLayout.Instance;
            if (layout == null || pedestrianPrefab == null) return;
            var g = layout.Graph;
            var rng = new System.Random();
            var player = GameObject.FindWithTag("Player");
            var robot = player != null ? player.transform : null;
            var parent = new GameObject("Pedestrians").transform;

            for (var n = 0; n < count; n++)
            {
                var loop = g.SidewalkLoop(rng.Next(g.Blocks), rng.Next(g.Blocks));
                var go = Instantiate(pedestrianPrefab, parent);
                var ped = go.GetComponent<Pedestrian>();
                ped.Init(loop, rng.Next(4), (float)rng.NextDouble(), rng.NextDouble() < 0.5, layout.SidewalkHeight, robot, rng.Next());
                Tint(go, shirtPalette[rng.Next(shirtPalette.Length)]);
            }
        }

        private static void Tint(GameObject go, Color color)
        {
            var block = new MaterialPropertyBlock();
            block.SetColor("_Color", color);
            block.SetColor("_BaseColor", color);
            foreach (var r in go.GetComponentsInChildren<Renderer>())
                if (r.name == "Shirt") r.SetPropertyBlock(block);
        }

        public void SetPrefab(GameObject prefab) => pedestrianPrefab = prefab;
    }
}
