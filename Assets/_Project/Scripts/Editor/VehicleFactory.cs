using DeliveryBot.Traffic;
using UnityEngine;

namespace DeliveryBot.EditorTools
{
    /// <summary>Traffic car prefabs (sedan / van / truck) and the pedestrian prefab, from primitives.</summary>
    public static class VehicleFactory
    {
        public static GameObject[] CreateCarPrefabs()
        {
            var paint = BuildKit.Mat("CarPaint", Color.white);
            var glass = BuildKit.Mat("CarGlass", new Color(0.2f, 0.28f, 0.38f));
            var tire = BuildKit.Mat("Tire", new Color(0.08f, 0.08f, 0.08f));
            var light = BuildKit.Mat("Lens", new Color(0.2f, 0.9f, 1f), emissive: true);
            var tail = BuildKit.Mat("TailLight", new Color(1f, 0.15f, 0.1f), emissive: true);
            var cargo = BuildKit.Mat("TruckBox", new Color(0.85f, 0.85f, 0.82f));

            return new[]
            {
                Sedan(paint, glass, tire, light, tail),
                Van(paint, glass, tire, light, tail),
                Truck(paint, glass, tire, light, tail, cargo)
            };
        }

        private static GameObject Base(string name, Vector3 colliderSize, float colliderY)
        {
            var root = new GameObject(name) { tag = "Traffic" };
            var col = root.AddComponent<BoxCollider>();
            col.center = new Vector3(0f, colliderY, 0f);
            col.size = colliderSize;
            var rb = root.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            root.AddComponent<TrafficCar>();
            return root;
        }

        private static void Wheels(Transform t, Material tire, float x, float zFront, float zRear, float r)
        {
            foreach (var (wx, wz) in new[] { (-x, zFront), (x, zFront), (-x, zRear), (x, zRear) })
                BuildKit.Prim(PrimitiveType.Cylinder, "Wheel", t, new Vector3(wx, r, wz), new Vector3(r * 2f, 0.12f, r * 2f), tire,
                    localRot: Quaternion.Euler(0f, 0f, 90f));
        }

        private static void Lights(Transform t, Material light, Material tail, float y, float zFront, float zRear, float x)
        {
            BuildKit.Prim(PrimitiveType.Cube, "HeadL", t, new Vector3(-x, y, zFront), new Vector3(0.3f, 0.12f, 0.03f), light);
            BuildKit.Prim(PrimitiveType.Cube, "HeadR", t, new Vector3(x, y, zFront), new Vector3(0.3f, 0.12f, 0.03f), light);
            BuildKit.Prim(PrimitiveType.Cube, "TailL", t, new Vector3(-x, y, zRear), new Vector3(0.3f, 0.12f, 0.03f), tail);
            BuildKit.Prim(PrimitiveType.Cube, "TailR", t, new Vector3(x, y, zRear), new Vector3(0.3f, 0.12f, 0.03f), tail);
        }

        private static GameObject Sedan(Material paint, Material glass, Material tire, Material light, Material tail)
        {
            var root = Base("Sedan", new Vector3(1.8f, 1.3f, 4.2f), 0.75f);
            var t = root.transform;
            BuildKit.Prim(PrimitiveType.Cube, "Paint", t, new Vector3(0f, 0.6f, 0f), new Vector3(1.8f, 0.55f, 4.2f), paint);
            BuildKit.Prim(PrimitiveType.Cube, "Paint", t, new Vector3(0f, 1.1f, -0.2f), new Vector3(1.6f, 0.5f, 2.1f), paint);
            BuildKit.Prim(PrimitiveType.Cube, "Glass", t, new Vector3(0f, 1.12f, -0.2f), new Vector3(1.62f, 0.32f, 2.12f), glass);
            Wheels(t, tire, 0.85f, 1.35f, -1.35f, 0.34f);
            Lights(t, light, tail, 0.6f, 2.11f, -2.11f, 0.6f);
            return BuildKit.SavePrefab(root, "Car_Sedan");
        }

        private static GameObject Van(Material paint, Material glass, Material tire, Material light, Material tail)
        {
            var root = Base("Van", new Vector3(1.9f, 1.9f, 4.6f), 1.0f);
            var t = root.transform;
            BuildKit.Prim(PrimitiveType.Cube, "Paint", t, new Vector3(0f, 1.05f, 0f), new Vector3(1.9f, 1.5f, 4.6f), paint);
            BuildKit.Prim(PrimitiveType.Cube, "Glass", t, new Vector3(0f, 1.35f, 1.2f), new Vector3(1.92f, 0.5f, 2.3f), glass);
            Wheels(t, tire, 0.9f, 1.5f, -1.5f, 0.36f);
            Lights(t, light, tail, 0.7f, 2.31f, -2.31f, 0.65f);
            return BuildKit.SavePrefab(root, "Car_Van");
        }

        private static GameObject Truck(Material paint, Material glass, Material tire, Material light, Material tail, Material cargo)
        {
            var root = Base("Truck", new Vector3(2.1f, 2.6f, 6.4f), 1.4f);
            var t = root.transform;
            BuildKit.Prim(PrimitiveType.Cube, "Paint", t, new Vector3(0f, 1.2f, 2.1f), new Vector3(2.0f, 1.9f, 2.0f), paint);
            BuildKit.Prim(PrimitiveType.Cube, "Glass", t, new Vector3(0f, 1.6f, 2.6f), new Vector3(1.9f, 0.6f, 1.0f), glass);
            BuildKit.Prim(PrimitiveType.Cube, "Cargo", t, new Vector3(0f, 1.6f, -1.2f), new Vector3(2.1f, 2.4f, 4.2f), cargo);
            Wheels(t, tire, 0.95f, 2.2f, -1.6f, 0.42f);
            Lights(t, light, tail, 0.7f, 3.11f, -3.31f, 0.7f);
            return BuildKit.SavePrefab(root, "Car_Truck");
        }

        public static GameObject CreatePedestrianPrefab()
        {
            var shirt = BuildKit.Mat("Shirt", Color.white);
            var pants = BuildKit.Mat("Pants", new Color(0.2f, 0.22f, 0.3f));
            var skin = BuildKit.Mat("Skin", new Color(0.93f, 0.78f, 0.65f));
            var alert = BuildKit.Mat("Alert", new Color(1f, 0.85f, 0.1f), emissive: true);

            var root = new GameObject("Pedestrian") { tag = "Pedestrian" };
            var col = root.AddComponent<CapsuleCollider>();
            col.center = new Vector3(0f, 0.9f, 0f);
            col.height = 1.8f;
            col.radius = 0.3f;
            var rb = root.AddComponent<Rigidbody>();
            rb.isKinematic = true;

            var body = BuildKit.Node("Body", root.transform, Vector3.zero);
            BuildKit.Prim(PrimitiveType.Capsule, "Pants", body.transform, new Vector3(0f, 0.45f, 0f), new Vector3(0.42f, 0.45f, 0.42f), pants);
            BuildKit.Prim(PrimitiveType.Capsule, "Shirt", body.transform, new Vector3(0f, 1.1f, 0f), new Vector3(0.5f, 0.38f, 0.5f), shirt);
            BuildKit.Prim(PrimitiveType.Sphere, "Head", body.transform, new Vector3(0f, 1.65f, 0f), new Vector3(0.36f, 0.36f, 0.36f), skin);
            var bubble = BuildKit.Prim(PrimitiveType.Cube, "Alert", root.transform, new Vector3(0f, 2.15f, 0f), new Vector3(0.12f, 0.4f, 0.12f), alert);
            BuildKit.Prim(PrimitiveType.Cube, "AlertDot", bubble.transform, new Vector3(0f, -0.9f, 0f), new Vector3(1f, 0.3f, 1f), alert);
            bubble.SetActive(false);

            var ped = root.AddComponent<Pedestrian>();
            BuildKit.SetField(ped, "body", body.transform);
            BuildKit.SetField(ped, "alertBubble", bubble);
            return BuildKit.SavePrefab(root, "Pedestrian");
        }
    }
}
