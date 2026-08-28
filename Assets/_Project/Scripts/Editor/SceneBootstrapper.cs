using DeliveryBot.Audio;
using DeliveryBot.CameraSystem;
using DeliveryBot.Delivery;
using DeliveryBot.Input;
using DeliveryBot.Minimap;
using DeliveryBot.Traffic;
using DeliveryBot.UI;
using DeliveryBot.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace DeliveryBot.EditorTools
{
    /// <summary>
    /// Orchestrates the reproducible build of the City scene: assets → world → actors → managers → HUD.
    /// Menu: DeliveryBot > Build City Scene. Headless: -executeMethod DeliveryBot.EditorTools.SceneBootstrapper.Build
    /// </summary>
    public static class SceneBootstrapper
    {
        private const string ScenePath = BuildKit.Root + "/Scenes/City.unity";
        private const int Blocks = 6;
        private const float BlockSize = 24f;
        private const float RoadWidth = 10f;
        private const float LaneOffset = 2.6f;
        private const float SidewalkWidth = 2.5f;
        private const float SidewalkHeight = 0.12f;
        private const int DeliveryPointCount = 30;
        private const float MinimapOrthoSize = 45f;

        [MenuItem("DeliveryBot/Build City Scene")]
        public static void Build()
        {
            foreach (var f in new[] { "Scenes", "Settings", "Materials", "Prefabs" }) BuildKit.EnsureFolder(f);
            BuildKit.EnsureTag("Traffic");
            BuildKit.EnsureTag("Pedestrian");

            var profile = LoadOrCreate<SteeringWheelProfile>($"{BuildKit.Root}/Settings/G27Profile.asset");
            var minimapRT = CreateRenderTexture();
            var skybox = CreateSkybox();
            var rng = new System.Random(20260828);

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            CreateLighting(skybox);

            var cityGo = new GameObject("City");
            var layout = cityGo.AddComponent<CityLayout>();
            layout.Configure(Blocks, BlockSize, RoadWidth, LaneOffset, SidewalkWidth, SidewalkHeight);
            var graph = layout.Graph;
            var parks = CityBuilder.Build(cityGo.transform, graph, rng, SidewalkHeight);

            var robotPrefab = RobotFactory.CreatePrefab(profile);
            var carPrefabs = VehicleFactory.CreateCarPrefabs();
            var pedestrianPrefab = VehicleFactory.CreatePedestrianPrefab();

            var robot = (GameObject)PrefabUtility.InstantiatePrefab(robotPrefab);
            robot.transform.position = new Vector3(0f, 0.05f, 0f);

            var rig = CreateCameraRig(robot.transform);
            CreateMinimapCamera(robot.transform, minimapRT);

            var pointsParent = new GameObject("DeliveryPoints").transform;
            var points = DeliveryPointFactory.Create(pointsParent, graph, parks, DeliveryPointCount, SidewalkHeight, rng);

            var managers = new GameObject("GameManager");
            var manager = managers.AddComponent<DeliveryManager>();
            manager.SetPoints(points);
            manager.SetRobot(robot.transform);
            managers.AddComponent<GameBootstrap>();
            managers.AddComponent<SfxPlayer>();
            var overlay = managers.AddComponent<WheelDebugOverlay>();
            BuildKit.SetField(overlay, "provider", robot.GetComponent<DriveInputProvider>());
            var traffic = managers.AddComponent<TrafficSpawner>();
            traffic.SetPrefabs(carPrefabs);
            var pedestrians = managers.AddComponent<PedestrianSpawner>();
            pedestrians.SetPrefab(pedestrianPrefab);

            var hud = HudBuilder.Build(manager, robot, rig, minimapRT, MinimapOrthoSize);
            var feedback = managers.AddComponent<GameFeedback>();
            BuildKit.SetField(feedback, "manager", manager);
            BuildKit.SetField(feedback, "hud", hud);
            BuildKit.SetField(feedback, "cameraRig", rig);
            BuildKit.SetField(feedback, "robot", robot.transform);
            EditorUtility.SetDirty(manager);
            EditorUtility.SetDirty(traffic);
            EditorUtility.SetDirty(pedestrians);

            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            PlayerSettings.productName = "DeliveryBotSim";
            AssetDatabase.SaveAssets();
            Debug.Log($"[SceneBootstrapper] Built {ScenePath}: {points.Count} delivery points, {parks.Count} parks, {carPrefabs.Length} car prefabs");
        }

        private static T LoadOrCreate<T>(string path) where T : ScriptableObject
        {
            var existing = AssetDatabase.LoadAssetAtPath<T>(path);
            if (existing != null) return existing;
            var asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        private static RenderTexture CreateRenderTexture()
        {
            var path = $"{BuildKit.Root}/Settings/MinimapRT.renderTexture";
            var existing = AssetDatabase.LoadAssetAtPath<RenderTexture>(path);
            if (existing != null) return existing;
            var rt = new RenderTexture(512, 512, 16) { name = "MinimapRT" };
            AssetDatabase.CreateAsset(rt, path);
            return rt;
        }

        private static Material CreateSkybox()
        {
            var path = $"{BuildKit.Root}/Settings/Skybox.mat";
            var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null) return existing;
            var mat = new Material(Shader.Find("Skybox/Procedural")) { name = "Skybox" };
            mat.SetColor("_SkyTint", new Color(0.55f, 0.75f, 1f));
            mat.SetColor("_GroundColor", new Color(0.75f, 0.72f, 0.65f));
            mat.SetFloat("_Exposure", 1.25f);
            mat.SetFloat("_AtmosphereThickness", 0.9f);
            AssetDatabase.CreateAsset(mat, path);
            return mat;
        }

        private static void CreateLighting(Material skybox)
        {
            var sun = new GameObject("Sun").AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.color = new Color(1f, 0.95f, 0.85f);
            sun.intensity = 1.35f;
            sun.shadows = LightShadows.Soft;
            sun.transform.rotation = Quaternion.Euler(48f, -35f, 0f);
            RenderSettings.sun = sun;
            RenderSettings.skybox = skybox;
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.62f, 0.72f, 0.9f);
            RenderSettings.ambientEquatorColor = new Color(0.6f, 0.6f, 0.62f);
            RenderSettings.ambientGroundColor = new Color(0.35f, 0.33f, 0.3f);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = new Color(0.75f, 0.83f, 0.95f);
            RenderSettings.fogStartDistance = 120f;
            RenderSettings.fogEndDistance = 420f;
        }

        private static CameraRig CreateCameraRig(Transform robot)
        {
            var go = new GameObject("CameraRig") { tag = "MainCamera" };
            var cam = go.AddComponent<Camera>();
            cam.fieldOfView = 65f;
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 600f;
            go.AddComponent<AudioListener>();
            var rig = go.AddComponent<CameraRig>();
            rig.SetTarget(robot);
            go.transform.position = robot.position + new Vector3(0f, 2.4f, -5f);
            EditorUtility.SetDirty(rig);
            return rig;
        }

        private static void CreateMinimapCamera(Transform target, RenderTexture rt)
        {
            var go = new GameObject("MinimapCamera");
            var cam = go.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = MinimapOrthoSize;
            cam.targetTexture = rt;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.12f, 0.12f, 0.14f);
            cam.nearClipPlane = 1f;
            cam.farClipPlane = 200f;
            var follow = go.AddComponent<MinimapFollow>();
            BuildKit.SetField(follow, "target", target);
        }
    }
}
