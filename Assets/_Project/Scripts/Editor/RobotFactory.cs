using System.Collections.Generic;
using DeliveryBot.Audio;
using DeliveryBot.Delivery;
using DeliveryBot.Input;
using DeliveryBot.Vehicle;
using UnityEngine;

namespace DeliveryBot.EditorTools
{
    /// <summary>
    /// Assembles the delivery robot from primitives, following the concept sketch:
    /// white box body, dark chassis band, sloped front window, roof sensor bar, four big spoked wheels.
    /// Saves Assets/_Project/Prefabs/Robot.prefab.
    /// </summary>
    public static class RobotFactory
    {
        public const float WheelRadius = 0.36f;

        public static GameObject CreatePrefab(SteeringWheelProfile profile)
        {
            var body = BuildKit.Mat("RobotBody", new Color(0.96f, 0.96f, 0.95f));
            var chassis = BuildKit.Mat("RobotChassis", new Color(0.18f, 0.19f, 0.21f));
            var glass = BuildKit.Mat("RobotGlass", new Color(0.15f, 0.25f, 0.4f, 0.75f), transparent: true);
            var tire = BuildKit.Mat("Tire", new Color(0.08f, 0.08f, 0.08f));
            var rim = BuildKit.Mat("Rim", new Color(0.75f, 0.76f, 0.78f));
            var accent = BuildKit.Mat("Accent", new Color(1f, 0.78f, 0.1f));
            var tail = BuildKit.Mat("TailLight", new Color(1f, 0.15f, 0.1f), emissive: true);
            var lens = BuildKit.Mat("Lens", new Color(0.2f, 0.9f, 1f), emissive: true);
            var cargo = BuildKit.Mat("CargoBox", new Color(0.8f, 0.6f, 0.35f));

            var root = new GameObject("Robot") { tag = "Player" };
            var t = root.transform;

            // Body: main box + slightly wider lower band + roof cap for a rounded silhouette.
            BuildKit.Prim(PrimitiveType.Cube, "Body", t, new Vector3(0f, 0.95f, 0f), new Vector3(1.0f, 0.75f, 1.55f), body);
            BuildKit.Prim(PrimitiveType.Cube, "BodyUpper", t, new Vector3(0f, 1.34f, -0.08f), new Vector3(0.92f, 0.12f, 1.35f), body);
            BuildKit.Prim(PrimitiveType.Cube, "Chassis", t, new Vector3(0f, 0.55f, 0f), new Vector3(1.04f, 0.16f, 1.5f), chassis);
            BuildKit.Prim(PrimitiveType.Cube, "SidePanelL", t, new Vector3(-0.505f, 0.9f, -0.15f), new Vector3(0.02f, 0.3f, 0.8f), accent);
            BuildKit.Prim(PrimitiveType.Cube, "SidePanelR", t, new Vector3(0.505f, 0.9f, -0.15f), new Vector3(0.02f, 0.3f, 0.8f), accent);

            // Sloped windshield + roof sensor bar with camera.
            BuildKit.Prim(PrimitiveType.Cube, "Windshield", t, new Vector3(0f, 1.12f, 0.72f), new Vector3(0.84f, 0.42f, 0.06f), glass,
                localRot: Quaternion.Euler(-18f, 0f, 0f));
            BuildKit.Prim(PrimitiveType.Cube, "SensorBar", t, new Vector3(0f, 1.46f, 0.55f), new Vector3(0.95f, 0.07f, 0.22f), chassis);
            BuildKit.Prim(PrimitiveType.Cylinder, "SensorCam", t, new Vector3(-0.32f, 1.46f, 0.68f), new Vector3(0.1f, 0.06f, 0.1f), chassis,
                localRot: Quaternion.Euler(90f, 0f, 0f));
            BuildKit.Prim(PrimitiveType.Sphere, "SensorLens", t, new Vector3(-0.32f, 1.46f, 0.75f), new Vector3(0.06f, 0.06f, 0.06f), lens);
            BuildKit.Prim(PrimitiveType.Cube, "Headlight", t, new Vector3(0f, 0.7f, 0.76f), new Vector3(0.6f, 0.06f, 0.02f), lens);
            BuildKit.Prim(PrimitiveType.Cube, "TailLightL", t, new Vector3(-0.35f, 0.8f, -0.78f), new Vector3(0.2f, 0.08f, 0.02f), tail);
            BuildKit.Prim(PrimitiveType.Cube, "TailLightR", t, new Vector3(0.35f, 0.8f, -0.78f), new Vector3(0.2f, 0.08f, 0.02f), tail);

            // Cargo box shown on the roof while carrying a parcel (toggled at runtime).
            var cargoGo = BuildKit.Prim(PrimitiveType.Cube, "Cargo", t, new Vector3(0f, 1.55f, -0.3f), new Vector3(0.5f, 0.35f, 0.5f), cargo);
            cargoGo.SetActive(false);

            // Wheels: pivot (steer) -> spin -> tire, rim, spokes.
            var steerPivots = new List<Transform>();
            var spinPivots = new List<Transform>();
            foreach (var (x, z, front) in new[] { (-0.56f, 0.5f, true), (0.56f, 0.5f, true), (-0.56f, -0.5f, false), (0.56f, -0.5f, false) })
            {
                var pivot = BuildKit.Node(front ? "FrontWheelPivot" : "RearWheelPivot", t, new Vector3(x, WheelRadius, z));
                var spin = BuildKit.Node("Spin", pivot.transform, Vector3.zero);
                BuildWheel(spin.transform, tire, rim);
                if (front) steerPivots.Add(pivot.transform);
                spinPivots.Add(spin.transform);
            }

            // Body box sits above curb height; four sphere "wheels" carry the robot and roll over 12 cm curbs.
            var physics = FrictionlessMaterial(); // drive is velocity-based; ground friction must not fight it
            var col = root.AddComponent<BoxCollider>();
            col.center = new Vector3(0f, 0.95f, 0f);
            col.size = new Vector3(1.15f, 1.1f, 1.65f);
            col.sharedMaterial = physics;
            foreach (var (x, z) in new[] { (-0.56f, 0.5f), (0.56f, 0.5f), (-0.56f, -0.5f), (0.56f, -0.5f) })
            {
                var wheelCol = root.AddComponent<SphereCollider>();
                wheelCol.center = new Vector3(x, WheelRadius, z);
                wheelCol.radius = WheelRadius;
                wheelCol.sharedMaterial = physics;
            }

            var rb = root.AddComponent<Rigidbody>();
            rb.mass = 60f;
            rb.linearDamping = 0f;
            rb.angularDamping = 5f;

            var input = root.AddComponent<DriveInputProvider>();
            if (profile == null) Debug.LogError("[RobotFactory] SteeringWheelProfile is null/destroyed — the robot prefab will ignore G27Profile.asset");
            BuildKit.SetField(input, "wheelProfile", profile);
            var controller = root.AddComponent<RobotController>();
            BuildKit.SetField(controller, "input", input);
            var wheels = root.AddComponent<WheelVisuals>();
            wheels.Configure(steerPivots.ToArray(), spinPivots.ToArray(), WheelRadius);
            BuildKit.SetField(wheels, "robot", controller);
            BuildKit.SetField(wheels, "input", input);
            root.AddComponent<RobotAudio>();
            var cargoVisual = root.AddComponent<RobotCargoVisual>();
            BuildKit.SetField(cargoVisual, "cargo", cargoGo);

            return BuildKit.SavePrefab(root, "Robot");
        }

        private static PhysicsMaterial FrictionlessMaterial()
        {
            var path = $"{BuildKit.Root}/Settings/RobotPhysics.physicsMaterial";
            var existing = UnityEditor.AssetDatabase.LoadAssetAtPath<PhysicsMaterial>(path);
            if (existing != null) return existing;
            var pm = new PhysicsMaterial("RobotPhysics")
            {
                dynamicFriction = 0f,
                staticFriction = 0f,
                bounciness = 0f,
                frictionCombine = PhysicsMaterialCombine.Minimum,
                bounceCombine = PhysicsMaterialCombine.Minimum
            };
            UnityEditor.AssetDatabase.CreateAsset(pm, path);
            return pm;
        }

        private static void BuildWheel(Transform spin, Material tire, Material rim)
        {
            var d = WheelRadius * 2f;
            var rot = Quaternion.Euler(0f, 0f, 90f);
            BuildKit.Prim(PrimitiveType.Cylinder, "Tire", spin, Vector3.zero, new Vector3(d, 0.09f, d), tire, localRot: rot);
            BuildKit.Prim(PrimitiveType.Cylinder, "Hub", spin, Vector3.zero, new Vector3(d * 0.35f, 0.1f, d * 0.35f), rim, localRot: rot);
            for (var i = 0; i < 3; i++)
            {
                var angle = i * 60f;
                BuildKit.Prim(PrimitiveType.Cube, "Spoke", spin, Vector3.zero, new Vector3(0.2f, d * 0.85f, 0.04f), rim,
                    localRot: Quaternion.Euler(angle, 0f, 0f));
            }
        }
    }
}
