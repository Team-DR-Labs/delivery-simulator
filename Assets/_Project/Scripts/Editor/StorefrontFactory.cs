using System.Collections.Generic;
using DeliveryBot.Delivery;
using UnityEngine;

namespace DeliveryBot.EditorTools
{
    /// <summary>Ground-floor shop façades (window, door, awning, sign, category prop) + the pickup point at the door.</summary>
    public static class StorefrontFactory
    {
        private sealed class Category
        {
            public string Key;
            public string[] Names;
            public Color Awning, Sign, SignText;
            public string Prop;
        }

        private static readonly Category[] Categories =
        {
            new Category { Key = "치킨", Names = new[] { "황금치킨", "옛날통닭", "치킨공방", "바삭치킨" }, Awning = new Color(0.85f, 0.15f, 0.1f), Sign = new Color(1f, 0.85f, 0.2f), SignText = new Color(0.4f, 0.1f, 0.05f), Prop = "chicken" },
            new Category { Key = "피자", Names = new[] { "피자마을", "화덕피자", "피자플래닛", "동네피자" }, Awning = new Color(0.15f, 0.55f, 0.25f), Sign = new Color(0.85f, 0.15f, 0.1f), SignText = Color.white, Prop = "pizza" },
            new Category { Key = "카페", Names = new[] { "카페 라떼", "커피한잔", "모닝카페", "달콤카페" }, Awning = new Color(0.4f, 0.26f, 0.15f), Sign = new Color(0.95f, 0.9f, 0.8f), SignText = new Color(0.3f, 0.2f, 0.1f), Prop = "cup" },
            new Category { Key = "분식", Names = new[] { "김밥천국", "떡볶이집", "분식왕", "라볶이하우스" }, Awning = new Color(1f, 0.5f, 0.1f), Sign = Color.white, SignText = new Color(0.9f, 0.3f, 0.05f), Prop = "bowl" },
            new Category { Key = "편의점", Names = new[] { "편의점 24", "미니마트", "코너마트", "올데이스토어" }, Awning = new Color(0.1f, 0.6f, 0.35f), Sign = new Color(0.1f, 0.6f, 0.35f), SignText = Color.white, Prop = "none" },
            new Category { Key = "약국", Names = new[] { "행복약국", "우리약국", "튼튼약국", "건강약국" }, Awning = Color.white, Sign = new Color(0.1f, 0.65f, 0.3f), SignText = Color.white, Prop = "cross" },
            new Category { Key = "베이커리", Names = new[] { "빵굽는집", "밀크베이커리", "크로와상", "고소한빵집" }, Awning = new Color(0.9f, 0.8f, 0.6f), Sign = new Color(0.45f, 0.28f, 0.15f), SignText = new Color(1f, 0.95f, 0.85f), Prop = "bread" },
            new Category { Key = "꽃집", Names = new[] { "꽃마을", "플라워샵", "장미꽃집", "봄꽃집" }, Awning = new Color(0.95f, 0.55f, 0.7f), Sign = new Color(0.95f, 0.75f, 0.85f), SignText = new Color(0.5f, 0.15f, 0.3f), Prop = "flowers" },
            new Category { Key = "마트", Names = new[] { "우리마트", "과일가게", "정육점", "싱싱마트" }, Awning = new Color(1f, 0.8f, 0.1f), Sign = new Color(0.9f, 0.2f, 0.15f), SignText = Color.white, Prop = "fruit" },
            new Category { Key = "서점", Names = new[] { "동네서점", "문구나라", "책방골목", "연필문구" }, Awning = new Color(0.2f, 0.4f, 0.8f), Sign = new Color(0.2f, 0.35f, 0.7f), SignText = Color.white, Prop = "book" },
        };

        private static readonly Dictionary<string, Material> Mats = new Dictionary<string, Material>();
        private static readonly List<string> UsedNames = new List<string>();

        private static Material M(string name, Color c, bool emissive = false)
        {
            if (!Mats.TryGetValue(name, out var m)) Mats[name] = m = BuildKit.Mat(name, c, emissive);
            return m;
        }

        public static void ResetNames() => UsedNames.Clear();

        /// <summary>
        /// Attaches a storefront to one wall of a building. wallCenter is the wall's bottom-centre in world space,
        /// facing points outward, width is the wall length. Returns the pickup point on the sidewalk.
        /// </summary>
        public static DeliveryPoint Attach(Transform buildingRoot, Vector3 wallCenter, Vector3 facing, float width, Transform pointParent, Vector3 pointPos, System.Random rng)
        {
            var cat = Categories[rng.Next(Categories.Length)];
            var name = PickName(cat, rng);
            var root = BuildKit.Node($"Storefront_{name}", buildingRoot, Vector3.zero, isStatic: true);
            root.transform.position = wallCenter;
            root.transform.rotation = Quaternion.LookRotation(facing, Vector3.up);
            var t = root.transform;

            var glass = M("CarGlass", new Color(0.2f, 0.28f, 0.38f));
            var door = M("Door", new Color(0.3f, 0.18f, 0.1f));
            var frame = M("ShopFrame", new Color(0.25f, 0.25f, 0.27f));
            var awning = M($"Awning_{cat.Key}", cat.Awning);
            var sign = M($"Sign_{cat.Key}", cat.Sign);

            var w = Mathf.Min(width * 0.9f, 9f);
            BuildKit.Prim(PrimitiveType.Cube, "Frame", t, new Vector3(0f, 1.9f, 0.04f), new Vector3(w, 3.8f, 0.1f), frame, isStatic: true);
            BuildKit.Prim(PrimitiveType.Cube, "Window", t, new Vector3(-w * 0.17f, 1.4f, 0.12f), new Vector3(w * 0.5f, 2.1f, 0.08f), glass, isStatic: true);
            BuildKit.Prim(PrimitiveType.Cube, "Door", t, new Vector3(w * 0.3f, 1.15f, 0.12f), new Vector3(1.1f, 2.3f, 0.08f), door, isStatic: true);
            BuildKit.Prim(PrimitiveType.Sphere, "Handle", t, new Vector3(w * 0.3f - 0.4f, 1.1f, 0.2f), new Vector3(0.08f, 0.08f, 0.08f), frame, isStatic: true);
            BuildKit.Prim(PrimitiveType.Cube, "Awning", t, new Vector3(0f, 3.05f, 0.7f), new Vector3(w * 0.9f, 0.06f, 1.3f), awning,
                localRot: Quaternion.Euler(-14f, 0f, 0f), isStatic: true);
            BuildKit.Prim(PrimitiveType.Cube, "AwningFront", t, new Vector3(0f, 2.75f, 1.32f), new Vector3(w * 0.9f, 0.3f, 0.05f), awning, isStatic: true);
            BuildKit.Prim(PrimitiveType.Cube, "SignBoard", t, new Vector3(0f, 4.0f, 0.16f), new Vector3(w * 0.75f, 0.8f, 0.14f), sign, isStatic: true);
            PointMarkerFactory.WorldText(t, new Vector3(0f, 4.0f, 0.24f), name, 0.018f, 30, cat.SignText); // default 180° turn = readable from outside
            BuildProp(cat.Prop, t, new Vector3(0f, 4.85f, 0.25f));

            return PointMarkerFactory.Create(pointParent, pointPos, facing, name, PointKind.Shop, cat.Key);
        }

        private static string PickName(Category cat, System.Random rng)
        {
            var free = new List<string>();
            foreach (var n in cat.Names) if (!UsedNames.Contains(n)) free.Add(n);
            var name = free.Count > 0 ? free[rng.Next(free.Count)] : $"{cat.Names[rng.Next(cat.Names.Length)]} {UsedNames.Count}";
            UsedNames.Add(name);
            return name;
        }

        private static void BuildProp(string prop, Transform t, Vector3 pos)
        {
            switch (prop)
            {
                case "chicken":
                    BuildKit.Prim(PrimitiveType.Sphere, "Prop", t, pos, new Vector3(0.7f, 0.55f, 0.6f), M("PropOrange", new Color(0.95f, 0.55f, 0.15f)), isStatic: true);
                    BuildKit.Prim(PrimitiveType.Cylinder, "PropBone", t, pos + new Vector3(0.45f, 0.1f, 0f), new Vector3(0.12f, 0.3f, 0.12f), M("PropCream", new Color(0.98f, 0.95f, 0.85f)), localRot: Quaternion.Euler(0f, 0f, 60f), isStatic: true);
                    break;
                case "pizza":
                    BuildKit.Prim(PrimitiveType.Cylinder, "Prop", t, pos, new Vector3(1.0f, 0.06f, 1.0f), M("PropYellow", new Color(0.98f, 0.8f, 0.3f)), localRot: Quaternion.Euler(75f, 0f, 0f), isStatic: true);
                    BuildKit.Prim(PrimitiveType.Cylinder, "PropTop", t, pos + new Vector3(0f, 0.02f, -0.02f), new Vector3(0.8f, 0.06f, 0.8f), M("PropRed", new Color(0.85f, 0.15f, 0.1f)), localRot: Quaternion.Euler(75f, 0f, 0f), isStatic: true);
                    break;
                case "cup":
                    BuildKit.Prim(PrimitiveType.Cylinder, "Prop", t, pos, new Vector3(0.5f, 0.3f, 0.5f), M("PropCream", new Color(0.98f, 0.95f, 0.85f)), isStatic: true);
                    BuildKit.Prim(PrimitiveType.Cube, "PropHandle", t, pos + new Vector3(0.35f, 0f, 0f), new Vector3(0.15f, 0.3f, 0.12f), M("PropCream", new Color(0.98f, 0.95f, 0.85f)), isStatic: true);
                    BuildKit.Prim(PrimitiveType.Cylinder, "PropCoffee", t, pos + new Vector3(0f, 0.28f, 0f), new Vector3(0.42f, 0.02f, 0.42f), M("PropBrown", new Color(0.35f, 0.2f, 0.1f)), isStatic: true);
                    break;
                case "bowl":
                    BuildKit.Prim(PrimitiveType.Sphere, "Prop", t, pos, new Vector3(0.8f, 0.4f, 0.8f), M("PropRed", new Color(0.85f, 0.15f, 0.1f)), isStatic: true);
                    break;
                case "cross":
                    BuildKit.Prim(PrimitiveType.Cube, "Prop", t, pos, new Vector3(0.9f, 0.3f, 0.15f), M("PropGreen", new Color(0.1f, 0.75f, 0.35f), true), isStatic: true);
                    BuildKit.Prim(PrimitiveType.Cube, "Prop2", t, pos, new Vector3(0.3f, 0.9f, 0.15f), M("PropGreen", new Color(0.1f, 0.75f, 0.35f), true), isStatic: true);
                    break;
                case "bread":
                    BuildKit.Prim(PrimitiveType.Capsule, "Prop", t, pos, new Vector3(0.45f, 0.45f, 0.45f), M("PropTan", new Color(0.85f, 0.6f, 0.3f)), localRot: Quaternion.Euler(0f, 0f, 90f), isStatic: true);
                    break;
                case "flowers":
                    BuildKit.Prim(PrimitiveType.Sphere, "Prop", t, pos + new Vector3(-0.3f, 0f, 0f), new Vector3(0.4f, 0.4f, 0.4f), M("PropPink", new Color(0.95f, 0.4f, 0.6f)), isStatic: true);
                    BuildKit.Prim(PrimitiveType.Sphere, "Prop2", t, pos + new Vector3(0.1f, 0.15f, 0f), new Vector3(0.4f, 0.4f, 0.4f), M("PropYellow", new Color(0.98f, 0.8f, 0.3f)), isStatic: true);
                    BuildKit.Prim(PrimitiveType.Sphere, "Prop3", t, pos + new Vector3(0.4f, -0.05f, 0f), new Vector3(0.4f, 0.4f, 0.4f), M("PropRed", new Color(0.85f, 0.15f, 0.1f)), isStatic: true);
                    break;
                case "fruit":
                    BuildKit.Prim(PrimitiveType.Sphere, "Prop", t, pos + new Vector3(-0.25f, 0f, 0f), new Vector3(0.45f, 0.45f, 0.45f), M("PropRed", new Color(0.85f, 0.15f, 0.1f)), isStatic: true);
                    BuildKit.Prim(PrimitiveType.Sphere, "Prop2", t, pos + new Vector3(0.25f, 0f, 0f), new Vector3(0.45f, 0.45f, 0.45f), M("PropOrange", new Color(0.95f, 0.55f, 0.15f)), isStatic: true);
                    break;
                case "book":
                    BuildKit.Prim(PrimitiveType.Cube, "Prop", t, pos, new Vector3(0.7f, 0.5f, 0.15f), M("PropBlue", new Color(0.25f, 0.45f, 0.85f)), localRot: Quaternion.Euler(0f, 0f, 12f), isStatic: true);
                    break;
            }
        }
    }
}
