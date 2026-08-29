using System.Collections.Generic;
using DeliveryBot.Delivery;
using DeliveryBot.World;
using UnityEngine;

namespace DeliveryBot.EditorTools
{
    /// <summary>Residential lots (gable or flat-roof houses) and apartment towers, each with a drop-off point at the door.</summary>
    public static class HouseFactory
    {
        private static readonly Color[] WallColors =
        {
            new Color(0.96f, 0.93f, 0.86f), new Color(0.9f, 0.82f, 0.7f), new Color(0.82f, 0.88f, 0.94f),
            new Color(0.93f, 0.85f, 0.82f), new Color(0.86f, 0.92f, 0.84f), new Color(0.95f, 0.95f, 0.95f)
        };
        private static readonly Color[] RoofColors =
        {
            new Color(0.7f, 0.3f, 0.2f), new Color(0.3f, 0.32f, 0.36f), new Color(0.25f, 0.45f, 0.35f), new Color(0.45f, 0.3f, 0.5f)
        };
        private static readonly string[] Surnames = { "김", "이", "박", "최", "정", "강", "조", "윤", "장", "임", "한", "오", "서", "신", "권" };
        private static readonly string[] AptNames = { "푸른숲", "햇살마을", "달빛", "강변", "하늘채", "행복" };

        private static readonly Dictionary<string, Material> Mats = new Dictionary<string, Material>();
        private static int _homeCounter;

        private static Material M(string name, Color c)
        {
            if (!Mats.TryGetValue(name, out var m)) Mats[name] = m = BuildKit.Mat(name, c);
            return m;
        }

        public static void ResetCounters() => _homeCounter = 0;

        /// <summary>
        /// 3x3 lots inside a block; the 8 outer lots get a house whose door faces the street,
        /// the centre lot becomes a small garden. Returns the drop-off points.
        /// </summary>
        public static List<DeliveryPoint> ResidentialBlock(Transform blockParent, Transform pointParent, RoadGraph g, int bx, int bz, float sidewalkH, System.Random rng)
        {
            var points = new List<DeliveryPoint>();
            var c = g.BlockCenter(bx, bz);
            var inner = g.BlockSize - g.SidewalkWidth * 2f;
            var lot = inner / 3f;
            for (var ix = -1; ix <= 1; ix++)
            for (var iz = -1; iz <= 1; iz++)
            {
                var center = c + new Vector3(ix * lot, sidewalkH, iz * lot);
                if (ix == 0 && iz == 0)
                {
                    Garden(blockParent, center, rng);
                    continue;
                }
                // Door faces the nearest street: prefer the z side for middle-column lots, x side otherwise.
                var facing = ix == 0 ? new Vector3(0f, 0f, iz) : (iz == 0 ? new Vector3(ix, 0f, 0f) : (rng.NextDouble() < 0.5 ? new Vector3(ix, 0f, 0f) : new Vector3(0f, 0f, iz)));
                var name = $"{Surnames[rng.Next(Surnames.Length)]}씨네 집 {++_homeCounter}호";
                House(blockParent, center, facing, lot - 1.6f, rng);
                var doorPos = center + facing * (lot * 0.5f + 0.2f) + Vector3.up * 0f;
                doorPos.y = sidewalkH;
                points.Add(PointMarkerFactory.Create(pointParent, doorPos, facing, name, PointKind.Home, "주택"));
            }
            return points;
        }

        private static void House(Transform parent, Vector3 center, Vector3 facing, float size, System.Random rng)
        {
            var root = BuildKit.Node("House", parent, center, isStatic: true);
            root.transform.rotation = Quaternion.LookRotation(facing, Vector3.up);
            var t = root.transform;
            var wall = M($"HouseWall_{rng.Next(WallColors.Length)}", WallColors[rng.Next(WallColors.Length)]);
            var roof = M($"Roof_{rng.Next(RoofColors.Length)}", RoofColors[rng.Next(RoofColors.Length)]);
            var glass = M("CarGlass", new Color(0.2f, 0.28f, 0.38f));
            var door = M("Door", new Color(0.3f, 0.18f, 0.1f));
            var fence = M("Fence", new Color(0.85f, 0.85f, 0.8f));

            var w = size;
            var d = size;
            var h = 2.9f;
            BuildKit.Prim(PrimitiveType.Cube, "Walls", t, new Vector3(0f, h * 0.5f, 0f), new Vector3(w, h, d), wall, keepCollider: true, isStatic: true);
            BuildKit.Prim(PrimitiveType.Cube, "Door", t, new Vector3(-w * 0.25f, 1.05f, d * 0.5f + 0.03f), new Vector3(0.95f, 2.1f, 0.06f), door, isStatic: true);
            BuildKit.Prim(PrimitiveType.Cube, "WindowF", t, new Vector3(w * 0.22f, 1.6f, d * 0.5f + 0.03f), new Vector3(1.2f, 1.0f, 0.06f), glass, isStatic: true);
            BuildKit.Prim(PrimitiveType.Cube, "WindowL", t, new Vector3(-w * 0.5f - 0.03f, 1.6f, 0f), new Vector3(0.06f, 1.0f, 1.4f), glass, isStatic: true);
            BuildKit.Prim(PrimitiveType.Cube, "WindowR", t, new Vector3(w * 0.5f + 0.03f, 1.6f, 0f), new Vector3(0.06f, 1.0f, 1.4f), glass, isStatic: true);
            BuildKit.Prim(PrimitiveType.Cube, "Step", t, new Vector3(-w * 0.25f, 0.1f, d * 0.5f + 0.4f), new Vector3(1.3f, 0.2f, 0.8f), fence, isStatic: true);

            if (rng.NextDouble() < 0.6)
            {
                // Gable roof: diamond core hidden in the walls + two slabs + ridge cap.
                BuildKit.Prim(PrimitiveType.Cube, "Gable", t, new Vector3(0f, h, 0f), new Vector3(w - 0.1f, d * 0.55f, d * 0.55f), wall,
                    localRot: Quaternion.Euler(45f, 0f, 0f), isStatic: true);
                var span = Mathf.Sqrt(Mathf.Pow(d * 0.55f, 2f) + Mathf.Pow(d * 0.38f, 2f));
                var angle = Mathf.Atan2(d * 0.38f, d * 0.55f) * Mathf.Rad2Deg;
                BuildKit.Prim(PrimitiveType.Cube, "RoofA", t, new Vector3(0f, h + d * 0.19f, d * 0.27f), new Vector3(w + 0.5f, 0.12f, span + 0.2f), roof,
                    localRot: Quaternion.Euler(angle, 0f, 0f), isStatic: true);
                BuildKit.Prim(PrimitiveType.Cube, "RoofB", t, new Vector3(0f, h + d * 0.19f, -d * 0.27f), new Vector3(w + 0.5f, 0.12f, span + 0.2f), roof,
                    localRot: Quaternion.Euler(-angle, 0f, 0f), isStatic: true);
                BuildKit.Prim(PrimitiveType.Cube, "Ridge", t, new Vector3(0f, h + d * 0.385f, 0f), new Vector3(w + 0.55f, 0.22f, 0.45f), roof, isStatic: true);
                BuildKit.Prim(PrimitiveType.Cube, "Chimney", t, new Vector3(w * 0.3f, h + d * 0.3f, -d * 0.2f), new Vector3(0.5f, 0.9f, 0.5f), M("Chimney", new Color(0.5f, 0.3f, 0.25f)), isStatic: true);
            }
            else
            {
                // Flat roof "villa": parapet, water tank, rooftop railing.
                BuildKit.Prim(PrimitiveType.Cube, "Parapet", t, new Vector3(0f, h + 0.15f, 0f), new Vector3(w + 0.3f, 0.3f, d + 0.3f), roof, isStatic: true);
                BuildKit.Prim(PrimitiveType.Cylinder, "Tank", t, new Vector3(w * 0.25f, h + 0.75f, -d * 0.2f), new Vector3(1.0f, 0.5f, 1.0f), M("RoofProp", new Color(0.55f, 0.56f, 0.58f)), isStatic: true);
                BuildKit.Prim(PrimitiveType.Cube, "Band", t, new Vector3(0f, h * 0.5f, 0f), new Vector3(w + 0.08f, 0.25f, d + 0.08f), roof, isStatic: true);
            }

            // Low fence along the street side with a gap at the door.
            var fw = size + 1.2f;
            BuildKit.Prim(PrimitiveType.Cube, "FenceL", t, new Vector3(-fw * 0.5f + fw * 0.18f - 0.5f, 0.35f, d * 0.5f + 1.0f), new Vector3(fw * 0.36f, 0.7f, 0.08f), fence, isStatic: true);
            BuildKit.Prim(PrimitiveType.Cube, "FenceR", t, new Vector3(fw * 0.5f - fw * 0.25f, 0.35f, d * 0.5f + 1.0f), new Vector3(fw * 0.5f, 0.7f, 0.08f), fence, isStatic: true);
        }

        private static void Garden(Transform parent, Vector3 center, System.Random rng)
        {
            BuildKit.Prim(PrimitiveType.Cube, "Lawn", parent, center + Vector3.up * 0.01f, new Vector3(5f, 0.02f, 5f), M("Grass", new Color(0.42f, 0.7f, 0.36f)), isStatic: true);
            PropFactory.Tree(parent, center + new Vector3(-1.2f, 0f, 1f), rng);
            PropFactory.Tree(parent, center + new Vector3(1.4f, 0f, -0.8f), rng);
        }

        /// <summary>One apartment tower on the block with a lobby entrance drop-off.</summary>
        public static DeliveryPoint ApartmentBlock(Transform blockParent, Transform pointParent, RoadGraph g, int bx, int bz, float sidewalkH, System.Random rng)
        {
            var c = g.BlockCenter(bx, bz) + Vector3.up * sidewalkH;
            var side = rng.Next(4);
            var facing = RoadGraph.SideNormal(side);
            var root = BuildKit.Node("Apartment", blockParent, c, isStatic: true);
            root.transform.rotation = Quaternion.LookRotation(facing, Vector3.up);
            var t = root.transform;

            var floors = 8 + rng.Next(6);
            var h = floors * 3f;
            var w = 13f;
            var d = 10f;
            var wall = M($"AptWall_{rng.Next(3)}", new[] { new Color(0.9f, 0.9f, 0.88f), new Color(0.82f, 0.86f, 0.9f), new Color(0.88f, 0.84f, 0.78f) }[rng.Next(3)]);
            var window = M("WindowBand", new Color(0.18f, 0.24f, 0.32f));
            var rail = M("Fence", new Color(0.85f, 0.85f, 0.8f));
            var glass = M("CarGlass", new Color(0.2f, 0.28f, 0.38f));

            BuildKit.Prim(PrimitiveType.Cube, "Tower", t, new Vector3(0f, h * 0.5f, 0f), new Vector3(w, h, d), wall, keepCollider: true, isStatic: true);
            for (var f = 1; f < floors; f++)
            {
                BuildKit.Prim(PrimitiveType.Cube, "Windows", t, new Vector3(0f, f * 3f + 1.6f, 0f), new Vector3(w + 0.04f, 1.2f, d + 0.04f), window, isStatic: true);
                BuildKit.Prim(PrimitiveType.Cube, "Balcony", t, new Vector3(0f, f * 3f + 0.05f, d * 0.5f + 0.5f), new Vector3(w * 0.9f, 0.12f, 1.0f), wall, isStatic: true);
                BuildKit.Prim(PrimitiveType.Cube, "Railing", t, new Vector3(0f, f * 3f + 0.55f, d * 0.5f + 0.98f), new Vector3(w * 0.9f, 0.9f, 0.05f), rail, isStatic: true);
            }
            BuildKit.Prim(PrimitiveType.Cube, "Lobby", t, new Vector3(0f, 1.4f, d * 0.5f + 0.15f), new Vector3(5f, 2.8f, 0.3f), glass, isStatic: true);
            BuildKit.Prim(PrimitiveType.Cube, "Canopy", t, new Vector3(0f, 3.2f, d * 0.5f + 1.2f), new Vector3(6f, 0.15f, 2.4f), wall, isStatic: true);
            BuildKit.Prim(PrimitiveType.Cube, "RoofBox", t, new Vector3(w * 0.25f, h + 1.2f, 0f), new Vector3(3f, 2.4f, 3f), wall, isStatic: true);

            var dong = 101 + rng.Next(8);
            var name = $"{AptNames[rng.Next(AptNames.Length)]}아파트 {dong}동";
            PointMarkerFactory.WorldText(t, new Vector3(0f, h - 1.2f, d * 0.5f + 0.1f), name, 0.06f, 30);
            var doorPos = c + facing * (d * 0.5f + 2.6f);
            return PointMarkerFactory.Create(pointParent, doorPos, facing, $"{name} 경비실", PointKind.Home, "아파트");
        }
    }
}
