using System.Collections.Generic;
using DeliveryBot.World;
using UnityEngine;

namespace DeliveryBot.EditorTools
{
    /// <summary>
    /// Builds ground, roads, crosswalks, sidewalk slabs, buildings with window bands, parks and props
    /// for the whole grid. Returns the set of park blocks so storefronts are not placed there.
    /// </summary>
    public static class CityBuilder
    {
        private static readonly Color[] Palette =
        {
            new Color(0.93f, 0.86f, 0.78f), new Color(0.80f, 0.86f, 0.94f), new Color(0.94f, 0.80f, 0.78f),
            new Color(0.80f, 0.92f, 0.84f), new Color(0.96f, 0.92f, 0.80f), new Color(0.86f, 0.80f, 0.92f),
            new Color(0.97f, 0.88f, 0.70f), new Color(0.78f, 0.90f, 0.93f), new Color(0.90f, 0.90f, 0.90f),
            new Color(0.84f, 0.74f, 0.66f)
        };

        public static HashSet<(int, int)> Build(Transform parent, RoadGraph g, System.Random rng, float sidewalkHeight, float parkChance = 0.15f)
        {
            var ground = BuildKit.Mat("Ground", new Color(0.5f, 0.62f, 0.42f));
            var road = BuildKit.Mat("Road", new Color(0.2f, 0.2f, 0.22f));
            var stripe = BuildKit.Mat("RoadStripe", new Color(0.95f, 0.95f, 0.9f));
            var sidewalk = BuildKit.Mat("Sidewalk", new Color(0.78f, 0.77f, 0.74f));
            var grass = BuildKit.Mat("Grass", new Color(0.42f, 0.7f, 0.36f));
            var window = BuildKit.Mat("WindowBand", new Color(0.18f, 0.24f, 0.32f));
            var roofProp = BuildKit.Mat("RoofProp", new Color(0.55f, 0.56f, 0.58f));
            var buildingMats = new Material[Palette.Length];
            for (var i = 0; i < Palette.Length; i++) buildingMats[i] = BuildKit.Mat($"Building_{i}", Palette[i]);
            PropFactory.LoadMaterials();

            var half = g.Half;
            var total = g.Blocks * g.Pitch + g.RoadWidth;

            var groundGo = GameObject.CreatePrimitive(PrimitiveType.Plane);
            groundGo.name = "Ground";
            groundGo.isStatic = true;
            groundGo.transform.SetParent(parent);
            groundGo.transform.localScale = new Vector3(60f, 1f, 60f);
            groundGo.GetComponent<Renderer>().sharedMaterial = ground;

            BuildRoads(parent, g, road, stripe, half, total);

            var parks = new HashSet<(int, int)>();
            var blocks = BuildKit.Node("Blocks", parent, Vector3.zero, isStatic: true).transform;
            var props = BuildKit.Node("Props", parent, Vector3.zero, isStatic: true).transform;
            for (var bx = 0; bx < g.Blocks; bx++)
            for (var bz = 0; bz < g.Blocks; bz++)
            {
                var isPark = rng.NextDouble() < parkChance;
                if (isPark) parks.Add((bx, bz));
                var c = g.BlockCenter(bx, bz);
                var slab = BuildKit.Prim(PrimitiveType.Cube, isPark ? "ParkSlab" : "Sidewalk", blocks, c + Vector3.up * (sidewalkHeight * 0.5f),
                    new Vector3(g.BlockSize, sidewalkHeight, g.BlockSize), sidewalk, keepCollider: true, isStatic: true);
                if (isPark)
                {
                    var inner = g.BlockSize - g.SidewalkWidth * 2f;
                    BuildKit.Prim(PrimitiveType.Cube, "Lawn", slab.transform.parent, c + Vector3.up * (sidewalkHeight + 0.01f),
                        new Vector3(inner, 0.02f, inner), grass, isStatic: true);
                    BuildPark(props, g, bx, bz, rng, sidewalkHeight);
                }
                else
                {
                    BuildBuildings(blocks, g, bx, bz, rng, buildingMats, window, roofProp, sidewalkHeight);
                }
                BuildBlockProps(props, g, bx, bz, rng, sidewalkHeight);
            }
            return parks;
        }

        private static void BuildRoads(Transform parent, RoadGraph g, Material road, Material stripe, float half, float total)
        {
            var roads = BuildKit.Node("Roads", parent, Vector3.zero, isStatic: true).transform;
            for (var i = 0; i <= g.Blocks; i++)
            {
                var line = -half + i * g.Pitch;
                BuildKit.Prim(PrimitiveType.Cube, "RoadNS", roads, new Vector3(line, 0.01f, 0f), new Vector3(g.RoadWidth, 0.02f, total), road, isStatic: true);
                BuildKit.Prim(PrimitiveType.Cube, "RoadEW", roads, new Vector3(0f, 0.01f, line), new Vector3(total, 0.02f, g.RoadWidth), road, isStatic: true);
            }

            // Centre dashes along each edge + crosswalk stripes at every intersection approach.
            var marks = BuildKit.Node("RoadMarks", parent, Vector3.zero, isStatic: true).transform;
            foreach (var n in g.AllNodes())
            {
                var p = g.NodePosition(n);
                foreach (var m in g.Neighbors(n))
                {
                    if (m.I < n.I || m.J < n.J) continue; // each edge once
                    var dir = g.EdgeDirection(n, m);
                    var right = Vector3.Cross(Vector3.up, dir);
                    for (var d = g.RoadWidth * 0.5f + 4f; d < g.Pitch - g.RoadWidth * 0.5f - 3f; d += 4f)
                        BuildKit.Prim(PrimitiveType.Cube, "Dash", marks, p + dir * d + Vector3.up * 0.025f,
                            new Vector3(0.18f, 0.01f, 2f), stripe, localRot: Quaternion.LookRotation(dir), isStatic: true);
                    Crosswalk(marks, p + dir * (g.RoadWidth * 0.5f + 1.2f), dir, right, g.RoadWidth, stripe);
                    Crosswalk(marks, g.NodePosition(m) - dir * (g.RoadWidth * 0.5f + 1.2f), dir, right, g.RoadWidth, stripe);
                }
            }
        }

        private static void Crosswalk(Transform parent, Vector3 center, Vector3 dir, Vector3 right, float roadWidth, Material stripe)
        {
            var count = 7;
            var spacing = (roadWidth - 2f) / (count - 1);
            for (var i = 0; i < count; i++)
            {
                var offset = -(roadWidth - 2f) * 0.5f + i * spacing;
                BuildKit.Prim(PrimitiveType.Cube, "Zebra", parent, center + right * offset + Vector3.up * 0.025f,
                    new Vector3(0.6f, 0.01f, 1.8f), stripe, localRot: Quaternion.LookRotation(dir), isStatic: true);
            }
        }

        private static void BuildBuildings(Transform parent, RoadGraph g, int bx, int bz, System.Random rng,
            Material[] mats, Material window, Material roofProp, float sidewalkHeight)
        {
            var c = g.BlockCenter(bx, bz);
            var inner = g.BlockSize - g.SidewalkWidth * 2f;
            var gap = 1.6f;
            var footprint = (inner - gap) * 0.5f;
            var offset = (footprint + gap) * 0.5f;
            for (var sx = -1; sx <= 1; sx += 2)
            for (var sz = -1; sz <= 1; sz += 2)
            {
                var floors = 2 + (int)(Mathf.Pow((float)rng.NextDouble(), 1.6f) * 8f);
                var h = floors * 3f;
                var w = footprint - (float)rng.NextDouble() * 1.5f;
                var d = footprint - (float)rng.NextDouble() * 1.5f;
                var pos = c + new Vector3(sx * offset, sidewalkHeight, sz * offset);
                var b = BuildKit.Node($"Building_{bx}_{bz}_{sx}_{sz}", parent, pos, isStatic: true);
                var mat = mats[rng.Next(mats.Length)];
                BuildKit.Prim(PrimitiveType.Cube, "Walls", b.transform, new Vector3(0f, h * 0.5f, 0f), new Vector3(w, h, d), mat, keepCollider: true, isStatic: true);
                for (var f = 0; f < floors; f++)
                    BuildKit.Prim(PrimitiveType.Cube, "Windows", b.transform, new Vector3(0f, f * 3f + 1.9f, 0f), new Vector3(w + 0.04f, 1.1f, d + 0.04f), window, isStatic: true);
                BuildKit.Prim(PrimitiveType.Cube, "Parapet", b.transform, new Vector3(0f, h + 0.15f, 0f), new Vector3(w + 0.3f, 0.3f, d + 0.3f), mat, isStatic: true);
                var roll = rng.NextDouble();
                if (roll < 0.35)
                    BuildKit.Prim(PrimitiveType.Cube, "AC", b.transform, new Vector3(w * 0.25f, h + 0.7f, d * 0.2f), new Vector3(1.6f, 1.2f, 1.2f), roofProp, isStatic: true);
                else if (roll < 0.6)
                    BuildKit.Prim(PrimitiveType.Cylinder, "Tank", b.transform, new Vector3(-w * 0.25f, h + 1.2f, -d * 0.2f), new Vector3(1.6f, 1.2f, 1.6f), roofProp, isStatic: true);
            }
        }

        private static void BuildPark(Transform parent, RoadGraph g, int bx, int bz, System.Random rng, float sidewalkHeight)
        {
            var c = g.BlockCenter(bx, bz);
            var inner = g.BlockSize * 0.5f - g.SidewalkWidth - 1.5f;
            var trees = 7 + rng.Next(6);
            for (var i = 0; i < trees; i++)
            {
                var p = c + new Vector3(((float)rng.NextDouble() * 2f - 1f) * inner, sidewalkHeight, ((float)rng.NextDouble() * 2f - 1f) * inner);
                PropFactory.Tree(parent, p, rng);
            }
            PropFactory.Bench(parent, c + new Vector3(0f, sidewalkHeight, inner * 0.5f), 180f);
            PropFactory.Bench(parent, c + new Vector3(0f, sidewalkHeight, -inner * 0.5f), 0f);
        }

        private static void BuildBlockProps(Transform parent, RoadGraph g, int bx, int bz, System.Random rng, float sidewalkHeight)
        {
            var c = g.BlockCenter(bx, bz);
            var edge = g.BlockSize * 0.5f - g.SidewalkWidth * 0.5f;
            var y = sidewalkHeight;

            // Trees along each side, lamps at two opposite corners, a few small items.
            for (var side = 0; side < 4; side++)
            {
                var normal = RoadGraph.SideNormal(side);
                var along = Vector3.Cross(Vector3.up, normal);
                for (var t = -edge + 4f; t <= edge - 4f; t += 7f)
                {
                    if (rng.NextDouble() < 0.25) continue;
                    PropFactory.Tree(parent, c + normal * edge + along * t + Vector3.up * y, rng);
                }
                if (rng.NextDouble() < 0.35) PropFactory.TrashBin(parent, c + normal * edge + along * (edge - 2f) + Vector3.up * y);
                if (rng.NextDouble() < 0.25) PropFactory.Hydrant(parent, c + normal * (edge + 0.5f) + along * 1.5f + Vector3.up * y);
                if (rng.NextDouble() < 0.3) PropFactory.Bench(parent, c + normal * (edge - 0.6f) + along * ((float)rng.NextDouble() * 6f - 3f) + Vector3.up * y, Quaternion.LookRotation(normal).eulerAngles.y);
            }
            PropFactory.StreetLamp(parent, c + new Vector3(edge, y, edge), Vector3.forward + Vector3.right);
            PropFactory.StreetLamp(parent, c + new Vector3(-edge, y, -edge), Vector3.back + Vector3.left);
        }
    }
}
