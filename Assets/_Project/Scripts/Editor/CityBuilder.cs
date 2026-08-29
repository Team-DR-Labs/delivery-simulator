using System.Collections.Generic;
using DeliveryBot.Delivery;
using DeliveryBot.World;
using UnityEngine;

namespace DeliveryBot.EditorTools
{
    public enum BlockType { Park, Commercial, Residential, Apartment }

    /// <summary>
    /// Builds ground, roads, crosswalks, sidewalks and every block (shops, houses, apartments, parks) plus props.
    /// Returns all delivery points (shops = pickup, homes = drop-off).
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

        /// <summary>No prop (tree, lamp, bench, bin, hydrant) may stand this close to a doorstep.</summary>
        private const float KeepClearRadius = 4.5f;

        private static bool IsClear(Vector3 pos, List<DeliveryPoint> points)
        {
            foreach (var p in points)
            {
                var d = p.transform.position - pos;
                d.y = 0f;
                if (d.sqrMagnitude < KeepClearRadius * KeepClearRadius) return false;
            }
            return true;
        }

        public static List<DeliveryPoint> Build(Transform parent, Transform pointParent, RoadGraph g, System.Random rng, float sidewalkHeight)
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
            StorefrontFactory.ResetNames();
            HouseFactory.ResetCounters();

            var half = g.Half;
            var total = g.Blocks * g.Pitch + g.RoadWidth;
            var groundGo = GameObject.CreatePrimitive(PrimitiveType.Plane);
            groundGo.name = "Ground";
            groundGo.isStatic = true;
            groundGo.transform.SetParent(parent);
            groundGo.transform.localScale = new Vector3(60f, 1f, 60f);
            groundGo.GetComponent<Renderer>().sharedMaterial = ground;

            BuildRoads(parent, g, road, stripe, half, total);

            var points = new List<DeliveryPoint>();
            var blocks = BuildKit.Node("Blocks", parent, Vector3.zero, isStatic: true).transform;
            var props = BuildKit.Node("Props", parent, Vector3.zero, isStatic: true).transform;
            for (var bx = 0; bx < g.Blocks; bx++)
            for (var bz = 0; bz < g.Blocks; bz++)
            {
                var type = PickType(rng);
                var c = g.BlockCenter(bx, bz);
                var blockPoints = new List<DeliveryPoint>();
                var blockNode = BuildKit.Node($"Block_{bx}_{bz}_{type}", blocks, Vector3.zero, isStatic: true).transform;
                BuildKit.Prim(PrimitiveType.Cube, "Sidewalk", blockNode, c + Vector3.up * (sidewalkHeight * 0.5f),
                    new Vector3(g.BlockSize, sidewalkHeight, g.BlockSize), sidewalk, keepCollider: true, isStatic: true);

                switch (type)
                {
                    case BlockType.Park:
                        var inner = g.BlockSize - g.SidewalkWidth * 2f;
                        BuildKit.Prim(PrimitiveType.Cube, "Lawn", blockNode, c + Vector3.up * (sidewalkHeight + 0.01f), new Vector3(inner, 0.02f, inner), grass, isStatic: true);
                        BuildPark(props, g, bx, bz, rng, sidewalkHeight);
                        break;
                    case BlockType.Commercial:
                        blockPoints.AddRange(BuildCommercial(blockNode, pointParent, g, bx, bz, rng, buildingMats, window, roofProp, sidewalkHeight));
                        break;
                    case BlockType.Residential:
                        blockPoints.AddRange(HouseFactory.ResidentialBlock(blockNode, pointParent, g, bx, bz, sidewalkHeight, rng));
                        break;
                    case BlockType.Apartment:
                        blockPoints.Add(HouseFactory.ApartmentBlock(blockNode, pointParent, g, bx, bz, sidewalkHeight, rng));
                        break;
                }
                points.AddRange(blockPoints);
                BuildBlockProps(props, g, bx, bz, rng, sidewalkHeight, blockPoints);
            }
            return points;
        }

        private static BlockType PickType(System.Random rng)
        {
            var r = rng.NextDouble();
            if (r < 0.12) return BlockType.Park;
            if (r < 0.24) return BlockType.Apartment;
            if (r < 0.62) return BlockType.Commercial;
            return BlockType.Residential;
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

            var marks = BuildKit.Node("RoadMarks", parent, Vector3.zero, isStatic: true).transform;
            foreach (var n in g.AllNodes())
            {
                var p = g.NodePosition(n);
                foreach (var m in g.Neighbors(n))
                {
                    if (m.I < n.I || m.J < n.J) continue;
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
            const int count = 7;
            var spacing = (roadWidth - 2f) / (count - 1);
            for (var i = 0; i < count; i++)
            {
                var offset = -(roadWidth - 2f) * 0.5f + i * spacing;
                BuildKit.Prim(PrimitiveType.Cube, "Zebra", parent, center + right * offset + Vector3.up * 0.025f,
                    new Vector3(0.6f, 0.01f, 1.8f), stripe, localRot: Quaternion.LookRotation(dir), isStatic: true);
            }
        }

        /// <summary>2x2 mid-rise buildings; each gets one storefront on a street-facing wall with a pickup point on the sidewalk.</summary>
        private static List<DeliveryPoint> BuildCommercial(Transform parent, Transform pointParent, RoadGraph g, int bx, int bz, System.Random rng,
            Material[] mats, Material window, Material roofProp, float sidewalkHeight)
        {
            var points = new List<DeliveryPoint>();
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
                var w = footprint - (float)rng.NextDouble() * 1.0f;
                var d = footprint - (float)rng.NextDouble() * 1.0f;
                var pos = c + new Vector3(sx * offset, sidewalkHeight, sz * offset);
                var b = BuildKit.Node($"Building_{bx}_{bz}_{sx}_{sz}", parent, pos, isStatic: true);
                var mat = mats[rng.Next(mats.Length)];
                BuildKit.Prim(PrimitiveType.Cube, "Walls", b.transform, new Vector3(0f, h * 0.5f, 0f), new Vector3(w, h, d), mat, keepCollider: true, isStatic: true);
                for (var f = 1; f < floors; f++)
                    BuildKit.Prim(PrimitiveType.Cube, "Windows", b.transform, new Vector3(0f, f * 3f + 1.9f, 0f), new Vector3(w + 0.04f, 1.1f, d + 0.04f), window, isStatic: true);
                BuildKit.Prim(PrimitiveType.Cube, "Parapet", b.transform, new Vector3(0f, h + 0.15f, 0f), new Vector3(w + 0.3f, 0.3f, d + 0.3f), mat, isStatic: true);
                var roll = rng.NextDouble();
                if (roll < 0.35)
                    BuildKit.Prim(PrimitiveType.Cube, "AC", b.transform, new Vector3(w * 0.25f, h + 0.7f, d * 0.2f), new Vector3(1.6f, 1.2f, 1.2f), roofProp, isStatic: true);
                else if (roll < 0.6)
                    BuildKit.Prim(PrimitiveType.Cylinder, "Tank", b.transform, new Vector3(-w * 0.25f, h + 1.2f, -d * 0.2f), new Vector3(1.6f, 1.2f, 1.6f), roofProp, isStatic: true);

                // Storefront on one of the two street-facing walls.
                var useX = rng.NextDouble() < 0.5;
                var facing = useX ? new Vector3(sx, 0f, 0f) : new Vector3(0f, 0f, sz);
                var wallCenter = pos + facing * (useX ? w * 0.5f : d * 0.5f);
                var wallWidth = useX ? d : w;
                var curb = g.BlockSize * 0.5f - g.SidewalkWidth * 0.4f;
                var pointPos = (useX ? new Vector3(c.x + sx * curb, sidewalkHeight, pos.z) : new Vector3(pos.x, sidewalkHeight, c.z + sz * curb));
                points.Add(StorefrontFactory.Attach(b.transform, wallCenter, facing, wallWidth, pointParent, pointPos, rng));
            }
            return points;
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

        private static void BuildBlockProps(Transform parent, RoadGraph g, int bx, int bz, System.Random rng, float sidewalkHeight, List<DeliveryPoint> keepClear)
        {
            var c = g.BlockCenter(bx, bz);
            var edge = g.BlockSize * 0.5f - g.SidewalkWidth * 0.5f;
            var y = sidewalkHeight;
            for (var side = 0; side < 4; side++)
            {
                var normal = RoadGraph.SideNormal(side);
                var along = Vector3.Cross(Vector3.up, normal);
                for (var t = -edge + 4f; t <= edge - 4f; t += 7f)
                {
                    if (rng.NextDouble() < 0.35) continue;
                    var pos = c + normal * edge + along * t + Vector3.up * y;
                    if (IsClear(pos, keepClear)) PropFactory.Tree(parent, pos, rng);
                }
                var bin = c + normal * edge + along * (edge - 2f) + Vector3.up * y;
                if (rng.NextDouble() < 0.35 && IsClear(bin, keepClear)) PropFactory.TrashBin(parent, bin);
                var hydrant = c + normal * (edge + 0.5f) + along * 1.5f + Vector3.up * y;
                if (rng.NextDouble() < 0.25 && IsClear(hydrant, keepClear)) PropFactory.Hydrant(parent, hydrant);
                var bench = c + normal * (edge - 0.6f) + along * ((float)rng.NextDouble() * 6f - 3f) + Vector3.up * y;
                if (rng.NextDouble() < 0.3 && IsClear(bench, keepClear)) PropFactory.Bench(parent, bench, Quaternion.LookRotation(normal).eulerAngles.y);
            }
            var lampA = c + new Vector3(edge, y, edge);
            var lampB = c + new Vector3(-edge, y, -edge);
            if (IsClear(lampA, keepClear)) PropFactory.StreetLamp(parent, lampA, Vector3.forward + Vector3.right);
            if (IsClear(lampB, keepClear)) PropFactory.StreetLamp(parent, lampB, Vector3.back + Vector3.left);
        }
    }
}
