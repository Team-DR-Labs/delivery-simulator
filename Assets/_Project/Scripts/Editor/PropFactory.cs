using DeliveryBot.World;
using UnityEngine;

namespace DeliveryBot.EditorTools
{
    /// <summary>Street furniture and vegetation built from primitives. All static except lamp glow pulse.</summary>
    public static class PropFactory
    {
        private static Material _trunk, _leavesA, _leavesB, _pole, _lampLight, _wood, _metal, _hydrant, _bin;

        public static void LoadMaterials()
        {
            _trunk = BuildKit.Mat("Trunk", new Color(0.42f, 0.28f, 0.16f));
            _leavesA = BuildKit.Mat("LeavesA", new Color(0.3f, 0.62f, 0.28f));
            _leavesB = BuildKit.Mat("LeavesB", new Color(0.45f, 0.7f, 0.3f));
            _pole = BuildKit.Mat("LampPole", new Color(0.25f, 0.26f, 0.28f));
            _lampLight = BuildKit.Mat("LampLight", new Color(1f, 0.9f, 0.6f), emissive: true);
            _wood = BuildKit.Mat("BenchWood", new Color(0.6f, 0.4f, 0.22f));
            _metal = BuildKit.Mat("Metal", new Color(0.4f, 0.42f, 0.45f));
            _hydrant = BuildKit.Mat("Hydrant", new Color(0.9f, 0.15f, 0.12f));
            _bin = BuildKit.Mat("TrashBin", new Color(0.2f, 0.45f, 0.3f));
        }

        /// <summary>
        /// Sloped slab from road level up to the sidewalk. curbPos is on the curb line at sidewalk height,
        /// outward points from the block into the road. Collider kept so the robot can drive up it.
        /// </summary>
        public static void CurbRamp(Transform parent, Vector3 curbPos, Vector3 outward, float width, float sidewalkHeight)
        {
            var sidewalkMat = BuildKit.Mat("Sidewalk", new Color(0.78f, 0.77f, 0.74f));
            const float length = 1.8f;
            const float thickness = 0.08f;
            var roadTop = 0.02f;
            var drop = sidewalkHeight - roadTop;
            var angle = Mathf.Atan2(drop, length) * Mathf.Rad2Deg;
            var root = BuildKit.Node("CurbRamp", parent, curbPos, isStatic: true);
            root.transform.rotation = Quaternion.LookRotation(outward, Vector3.up);
            // Local +z is outward and tilts downward; centre so the top surface meets the curb top and the road.
            var centre = new Vector3(0f, -drop * 0.5f - thickness * 0.5f + 0.005f, length * 0.5f - 0.15f);
            BuildKit.Prim(PrimitiveType.Cube, "Slope", root.transform, centre, new Vector3(width, thickness, length + 0.3f), sidewalkMat,
                keepCollider: true, localRot: Quaternion.Euler(angle, 0f, 0f), isStatic: true);
        }

        public static void Tree(Transform parent, Vector3 pos, System.Random rng)
        {
            var root = BuildKit.Node("Tree", parent, pos, isStatic: true);
            var scale = 0.8f + (float)rng.NextDouble() * 0.6f;
            root.transform.localScale = Vector3.one * scale;
            var trunk = BuildKit.Prim(PrimitiveType.Cylinder, "Trunk", root.transform, new Vector3(0f, 1.1f, 0f), new Vector3(0.35f, 1.1f, 0.35f), _trunk, isStatic: true);
            var col = trunk.AddComponent<CapsuleCollider>();
            col.radius = 0.6f;
            col.height = 2.2f;
            if (rng.NextDouble() < 0.6)
            {
                BuildKit.Prim(PrimitiveType.Sphere, "Canopy", root.transform, new Vector3(0f, 3.0f, 0f), new Vector3(2.8f, 2.4f, 2.8f), rng.NextDouble() < 0.5 ? _leavesA : _leavesB, isStatic: true);
            }
            else
            {
                BuildKit.Prim(PrimitiveType.Sphere, "Canopy1", root.transform, new Vector3(0f, 2.6f, 0f), new Vector3(2.6f, 1.4f, 2.6f), _leavesA, isStatic: true);
                BuildKit.Prim(PrimitiveType.Sphere, "Canopy2", root.transform, new Vector3(0f, 3.5f, 0f), new Vector3(2.0f, 1.3f, 2.0f), _leavesB, isStatic: true);
                BuildKit.Prim(PrimitiveType.Sphere, "Canopy3", root.transform, new Vector3(0f, 4.3f, 0f), new Vector3(1.2f, 1.1f, 1.2f), _leavesA, isStatic: true);
            }
        }

        public static void StreetLamp(Transform parent, Vector3 pos, Vector3 armDirection)
        {
            var root = BuildKit.Node("StreetLamp", parent, pos, isStatic: true);
            root.transform.rotation = Quaternion.LookRotation(armDirection, Vector3.up);
            var pole = BuildKit.Prim(PrimitiveType.Cylinder, "Pole", root.transform, new Vector3(0f, 2.3f, 0f), new Vector3(0.14f, 2.3f, 0.14f), _pole, isStatic: true);
            var col = pole.AddComponent<CapsuleCollider>();
            col.radius = 0.5f;
            col.height = 2f;
            BuildKit.Prim(PrimitiveType.Cube, "Arm", root.transform, new Vector3(0f, 4.55f, 0.6f), new Vector3(0.1f, 0.1f, 1.3f), _pole, isStatic: true);
            var light = BuildKit.Prim(PrimitiveType.Sphere, "Light", root.transform, new Vector3(0f, 4.45f, 1.2f), new Vector3(0.35f, 0.25f, 0.5f), _lampLight);
            light.AddComponent<PulseScale>();
            BuildKit.SetField(light.GetComponent<PulseScale>(), "amplitude", 0.06f);
            BuildKit.SetField(light.GetComponent<PulseScale>(), "frequency", 1.3f);
        }

        public static void Bench(Transform parent, Vector3 pos, float yaw)
        {
            var root = BuildKit.Node("Bench", parent, pos, isStatic: true);
            root.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
            BuildKit.Prim(PrimitiveType.Cube, "Seat", root.transform, new Vector3(0f, 0.45f, 0f), new Vector3(1.6f, 0.08f, 0.45f), _wood, isStatic: true);
            BuildKit.Prim(PrimitiveType.Cube, "Back", root.transform, new Vector3(0f, 0.75f, -0.2f), new Vector3(1.6f, 0.45f, 0.06f), _wood, isStatic: true);
            BuildKit.Prim(PrimitiveType.Cube, "LegL", root.transform, new Vector3(-0.65f, 0.22f, 0f), new Vector3(0.06f, 0.44f, 0.4f), _metal, isStatic: true);
            BuildKit.Prim(PrimitiveType.Cube, "LegR", root.transform, new Vector3(0.65f, 0.22f, 0f), new Vector3(0.06f, 0.44f, 0.4f), _metal, isStatic: true);
        }

        public static void TrashBin(Transform parent, Vector3 pos)
        {
            BuildKit.Prim(PrimitiveType.Cylinder, "TrashBin", parent, pos + new Vector3(0f, 0.45f, 0f), new Vector3(0.5f, 0.45f, 0.5f), _bin, isStatic: true);
        }

        public static void Hydrant(Transform parent, Vector3 pos)
        {
            var root = BuildKit.Node("Hydrant", parent, pos, isStatic: true);
            BuildKit.Prim(PrimitiveType.Cylinder, "Body", root.transform, new Vector3(0f, 0.35f, 0f), new Vector3(0.28f, 0.35f, 0.28f), _hydrant, isStatic: true);
            BuildKit.Prim(PrimitiveType.Sphere, "Cap", root.transform, new Vector3(0f, 0.72f, 0f), new Vector3(0.3f, 0.2f, 0.3f), _hydrant, isStatic: true);
            BuildKit.Prim(PrimitiveType.Cube, "Nozzles", root.transform, new Vector3(0f, 0.45f, 0f), new Vector3(0.5f, 0.12f, 0.12f), _hydrant, isStatic: true);
        }
    }
}
