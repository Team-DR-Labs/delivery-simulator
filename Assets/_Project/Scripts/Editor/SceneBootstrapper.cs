using System.Collections.Generic;
using DeliveryBot.Delivery;
using DeliveryBot.Input;
using DeliveryBot.Minimap;
using DeliveryBot.UI;
using DeliveryBot.Vehicle;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace DeliveryBot.EditorTools
{
    /// <summary>
    /// Builds the greybox city scene from code so the whole prototype is reproducible
    /// (and can be generated headless: -executeMethod DeliveryBot.EditorTools.SceneBootstrapper.Build).
    /// Menu: DeliveryBot > Build Greybox Scene.
    /// </summary>
    public static class SceneBootstrapper
    {
        private const string Root = "Assets/_Project";
        private const string ScenePath = Root + "/Scenes/City.unity";
        private const int Blocks = 6;
        private const float BlockSize = 24f;
        private const float RoadWidth = 10f;
        private const float Pitch = BlockSize + RoadWidth;
        private const int DeliveryPointCount = 8;

        [MenuItem("DeliveryBot/Build Greybox Scene")]
        public static void Build()
        {
            EnsureFolders();
            var mats = CreateMaterials();
            var minimapRT = CreateRenderTexture();
            var profile = CreateOrLoadWheelProfile();

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var rng = new System.Random(42);

            CreateLighting();
            CreateGround(mats.Ground);
            CreateCity(mats, rng);
            var robot = CreateRobot(mats.Robot, mats.Icon, profile);
            var minimapCam = CreateMinimapCamera(robot.transform, minimapRT);
            var points = CreateDeliveryPoints(mats.Marker, rng);
            var manager = CreateManagers(robot, points);
            CreateHud(manager, robot.transform, minimapRT);

            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            PlayerSettings.productName = "DeliveryBotSim";
            AssetDatabase.SaveAssets();
            Debug.Log($"[SceneBootstrapper] Built {ScenePath} with {points.Count} delivery points (minimap cam: {minimapCam.name})");
        }

        // ---------------------------------------------------------------- assets

        private struct Materials
        {
            public Material Ground, Road, Robot, Marker, Icon;
            public Material[] Buildings;
        }

        private static void EnsureFolders()
        {
            foreach (var f in new[] { "Scenes", "Settings", "Materials" })
                if (!AssetDatabase.IsValidFolder($"{Root}/{f}")) AssetDatabase.CreateFolder(Root, f);
        }

        private static Materials CreateMaterials()
        {
            var m = new Materials
            {
                Ground = Mat("Ground", new Color(0.55f, 0.58f, 0.52f)),
                Road = Mat("Road", new Color(0.22f, 0.22f, 0.24f)),
                Robot = Mat("Robot", new Color(0.95f, 0.95f, 0.95f)),
                Marker = Mat("Marker", Color.white, emissive: true),
                Icon = Mat("Icon", new Color(1f, 0.9f, 0.1f), emissive: true),
                Buildings = new[]
                {
                    Mat("Building_A", new Color(0.80f, 0.76f, 0.70f)),
                    Mat("Building_B", new Color(0.70f, 0.74f, 0.80f)),
                    Mat("Building_C", new Color(0.78f, 0.68f, 0.66f)),
                    Mat("Building_D", new Color(0.66f, 0.76f, 0.70f)),
                    Mat("Building_E", new Color(0.84f, 0.82f, 0.74f)),
                }
            };
            return m;
        }

        private static Material Mat(string name, Color color, bool emissive = false)
        {
            var path = $"{Root}/Materials/{name}.mat";
            var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null) return existing;

            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            var mat = new Material(shader) { name = name };
            mat.SetColor("_BaseColor", color);
            mat.SetColor("_Color", color);
            if (emissive)
            {
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", color);
            }
            AssetDatabase.CreateAsset(mat, path);
            return mat;
        }

        private static RenderTexture CreateRenderTexture()
        {
            var path = $"{Root}/Settings/MinimapRT.renderTexture";
            var existing = AssetDatabase.LoadAssetAtPath<RenderTexture>(path);
            if (existing != null) return existing;
            var rt = new RenderTexture(512, 512, 16) { name = "MinimapRT" };
            AssetDatabase.CreateAsset(rt, path);
            return rt;
        }

        private static SteeringWheelProfile CreateOrLoadWheelProfile()
        {
            var path = $"{Root}/Settings/G27Profile.asset";
            var existing = AssetDatabase.LoadAssetAtPath<SteeringWheelProfile>(path);
            if (existing != null) return existing;
            var p = ScriptableObject.CreateInstance<SteeringWheelProfile>();
            AssetDatabase.CreateAsset(p, path);
            return p;
        }

        // ---------------------------------------------------------------- world

        private static void CreateLighting()
        {
            var sun = new GameObject("Sun").AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.color = new Color(1f, 0.96f, 0.9f);
            sun.intensity = 1.3f;
            sun.shadows = LightShadows.Soft;
            sun.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.55f, 0.58f, 0.65f);
        }

        private static void CreateGround(Material mat)
        {
            var g = GameObject.CreatePrimitive(PrimitiveType.Plane);
            g.name = "Ground";
            g.isStatic = true;
            g.transform.localScale = new Vector3(40f, 1f, 40f);
            g.GetComponent<Renderer>().sharedMaterial = mat;
        }

        private static void CreateCity(Materials mats, System.Random rng)
        {
            var city = new GameObject("City");
            city.isStatic = true;
            var half = Blocks * Pitch * 0.5f;
            var totalLen = Blocks * Pitch + RoadWidth;

            // Roads: strips along both axes on every grid line.
            var roads = new GameObject("Roads") { isStatic = true };
            roads.transform.SetParent(city.transform);
            for (var i = 0; i <= Blocks; i++)
            {
                var line = -half + i * Pitch;
                MakeRoad(roads.transform, mats.Road, new Vector3(line, 0.01f, 0f), new Vector3(RoadWidth, 0.02f, totalLen));
                MakeRoad(roads.transform, mats.Road, new Vector3(0f, 0.01f, line), new Vector3(totalLen, 0.02f, RoadWidth));
            }

            // Buildings: 2x2 per block with random heights.
            var buildings = new GameObject("Buildings") { isStatic = true };
            buildings.transform.SetParent(city.transform);
            var footprint = BlockSize * 0.5f - 2f;
            for (var bx = 0; bx < Blocks; bx++)
            for (var bz = 0; bz < Blocks; bz++)
            {
                var cx = -half + Pitch * 0.5f + bx * Pitch;
                var cz = -half + Pitch * 0.5f + bz * Pitch;
                for (var sx = -1; sx <= 1; sx += 2)
                for (var sz = -1; sz <= 1; sz += 2)
                {
                    var h = 5f + (float)rng.NextDouble() * 18f;
                    var b = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    b.name = $"Building_{bx}_{bz}_{(sx > 0 ? "E" : "W")}{(sz > 0 ? "N" : "S")}";
                    b.isStatic = true;
                    b.transform.SetParent(buildings.transform);
                    b.transform.position = new Vector3(cx + sx * BlockSize * 0.25f, h * 0.5f, cz + sz * BlockSize * 0.25f);
                    b.transform.localScale = new Vector3(footprint, h, footprint);
                    b.GetComponent<Renderer>().sharedMaterial = mats.Buildings[rng.Next(mats.Buildings.Length)];
                }
            }
        }

        private static void MakeRoad(Transform parent, Material mat, Vector3 pos, Vector3 scale)
        {
            var r = GameObject.CreatePrimitive(PrimitiveType.Cube);
            r.name = "Road";
            r.isStatic = true;
            r.transform.SetParent(parent);
            r.transform.position = pos;
            r.transform.localScale = scale;
            r.GetComponent<Renderer>().sharedMaterial = mat;
            Object.DestroyImmediate(r.GetComponent<Collider>());
        }

        // ---------------------------------------------------------------- robot

        private static GameObject CreateRobot(Material bodyMat, Material iconMat, SteeringWheelProfile profile)
        {
            var robot = new GameObject("Robot") { tag = "Player" };
            robot.transform.position = new Vector3(0f, 0.3f, 0f);

            var body = GameObject.CreatePrimitive(PrimitiveType.Cube);
            body.name = "Body";
            body.transform.SetParent(robot.transform, false);
            body.transform.localPosition = new Vector3(0f, 0.55f, 0f);
            body.transform.localScale = new Vector3(0.9f, 0.7f, 1.3f);
            body.GetComponent<Renderer>().sharedMaterial = bodyMat;
            Object.DestroyImmediate(body.GetComponent<Collider>());

            foreach (var (x, z) in new[] { (-0.5f, 0.45f), (0.5f, 0.45f), (-0.5f, -0.45f), (0.5f, -0.45f) })
            {
                var w = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                w.name = "Wheel";
                w.transform.SetParent(robot.transform, false);
                w.transform.localPosition = new Vector3(x, 0.2f, z);
                w.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
                w.transform.localScale = new Vector3(0.4f, 0.08f, 0.4f);
                Object.DestroyImmediate(w.GetComponent<Collider>());
            }

            // Top icon (visible from the minimap camera only in practice: it sits above the FP camera).
            var icon = GameObject.CreatePrimitive(PrimitiveType.Cube);
            icon.name = "MinimapIcon";
            icon.transform.SetParent(robot.transform, false);
            icon.transform.localPosition = new Vector3(0f, 1.7f, 0f);
            icon.transform.localScale = new Vector3(0.6f, 0.05f, 1.2f);
            icon.GetComponent<Renderer>().sharedMaterial = iconMat;
            Object.DestroyImmediate(icon.GetComponent<Collider>());

            var col = robot.AddComponent<BoxCollider>();
            col.center = new Vector3(0f, 0.5f, 0f);
            col.size = new Vector3(0.9f, 1f, 1.3f);

            var rb = robot.AddComponent<Rigidbody>();
            rb.mass = 60f;
            rb.linearDamping = 0f;
            rb.angularDamping = 5f;

            var input = robot.AddComponent<DriveInputProvider>();
            SetField(input, "wheelProfile", profile);
            var controller = robot.AddComponent<RobotController>();
            SetField(controller, "input", input);

            var camGo = new GameObject("FPCamera") { tag = "MainCamera" };
            camGo.transform.SetParent(robot.transform, false);
            camGo.transform.localPosition = new Vector3(0f, 1.1f, 0.35f);
            var cam = camGo.AddComponent<Camera>();
            cam.fieldOfView = 75f;
            cam.nearClipPlane = 0.1f;
            camGo.AddComponent<AudioListener>();
            var fp = camGo.AddComponent<FirstPersonCamera>();
            SetField(fp, "robot", controller);
            SetField(fp, "input", input);

            return robot;
        }

        private static GameObject CreateMinimapCamera(Transform target, RenderTexture rt)
        {
            var go = new GameObject("MinimapCamera");
            var cam = go.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 45f;
            cam.targetTexture = rt;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.12f, 0.12f, 0.14f);
            cam.nearClipPlane = 1f;
            cam.farClipPlane = 200f;
            var follow = go.AddComponent<MinimapFollow>();
            SetField(follow, "target", target);
            return go;
        }

        // ---------------------------------------------------------------- delivery

        private static List<DeliveryPoint> CreateDeliveryPoints(Material markerMat, System.Random rng)
        {
            var half = Blocks * Pitch * 0.5f;
            var intersections = new List<Vector3>();
            for (var i = 0; i <= Blocks; i++)
            for (var j = 0; j <= Blocks; j++)
            {
                var p = new Vector3(-half + i * Pitch, 0f, -half + j * Pitch);
                if (p.sqrMagnitude > 1f) intersections.Add(p);
            }

            var parent = new GameObject("DeliveryPoints");
            var points = new List<DeliveryPoint>();
            for (var n = 0; n < DeliveryPointCount && intersections.Count > 0; n++)
            {
                var idx = rng.Next(intersections.Count);
                var pos = intersections[idx];
                intersections.RemoveAt(idx);

                var go = new GameObject($"DeliveryPoint_{(char)('A' + n)}");
                go.transform.SetParent(parent.transform);
                go.transform.position = pos;
                var trigger = go.AddComponent<SphereCollider>();
                trigger.isTrigger = true;
                trigger.radius = 3.5f;

                var marker = new GameObject("Marker");
                marker.transform.SetParent(go.transform, false);
                var pad = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                pad.name = "Pad";
                pad.transform.SetParent(marker.transform, false);
                pad.transform.localPosition = new Vector3(0f, 0.04f, 0f);
                pad.transform.localScale = new Vector3(6f, 0.03f, 6f);
                pad.GetComponent<Renderer>().sharedMaterial = markerMat;
                Object.DestroyImmediate(pad.GetComponent<Collider>());
                var beam = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                beam.name = "Beam";
                beam.transform.SetParent(marker.transform, false);
                beam.transform.localPosition = new Vector3(0f, 15f, 0f);
                beam.transform.localScale = new Vector3(0.5f, 15f, 0.5f);
                beam.GetComponent<Renderer>().sharedMaterial = markerMat;
                Object.DestroyImmediate(beam.GetComponent<Collider>());

                var dp = go.AddComponent<DeliveryPoint>();
                SetField(dp, "displayName", $"{(char)('A' + n)} 지점");
                SetField(dp, "marker", marker);
                points.Add(dp);
            }
            return points;
        }

        private static DeliveryManager CreateManagers(GameObject robot, List<DeliveryPoint> points)
        {
            var go = new GameObject("GameManager");
            var manager = go.AddComponent<DeliveryManager>();
            manager.SetPoints(points);
            manager.SetRobot(robot.transform);
            go.AddComponent<GameBootstrap>();
            var overlay = go.AddComponent<WheelDebugOverlay>();
            SetField(overlay, "provider", robot.GetComponent<DriveInputProvider>());
            EditorUtility.SetDirty(manager);
            return manager;
        }

        // ---------------------------------------------------------------- HUD

        private static void CreateHud(DeliveryManager manager, Transform robot, RenderTexture minimapRT)
        {
            var canvasGo = new GameObject("HUD");
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            canvasGo.AddComponent<GraphicRaycaster>();

            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            // Minimap (top-right circle)
            var border = UiImage(canvasGo.transform, "MinimapBorder", new Vector2(1f, 1f), new Vector2(-30f, -30f), new Vector2(332f, 332f), RuntimeSprite.Shape.Circle);
            border.color = new Color(1f, 1f, 1f, 0.9f);
            var mask = UiImage(canvasGo.transform, "MinimapMask", new Vector2(1f, 1f), new Vector2(-36f, -36f), new Vector2(320f, 320f), RuntimeSprite.Shape.Circle);
            mask.gameObject.AddComponent<Mask>().showMaskGraphic = false;
            var view = new GameObject("MinimapView", typeof(RectTransform)).AddComponent<RawImage>();
            view.transform.SetParent(mask.transform, false);
            Stretch(view.rectTransform);
            view.texture = minimapRT;
            var robotIcon = UiImage(mask.transform, "RobotIcon", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(22f, 22f), RuntimeSprite.Shape.Triangle);
            robotIcon.color = new Color(1f, 0.9f, 0.1f);

            // Texts (top-left)
            var status = UiText(canvasGo.transform, "StatusText", font, 30, new Vector2(0f, 1f), new Vector2(30f, -30f), new Vector2(900f, 110f));
            var score = UiText(canvasGo.transform, "ScoreText", font, 26, new Vector2(0f, 1f), new Vector2(30f, -150f), new Vector2(600f, 40f));

            // Direction arrow (bottom-center)
            var arrow = UiImage(canvasGo.transform, "TargetArrow", new Vector2(0.5f, 0f), new Vector2(0f, 110f), new Vector2(90f, 90f), RuntimeSprite.Shape.Triangle);
            arrow.color = new Color(1f, 0.6f, 0.1f);

            var hud = canvasGo.AddComponent<DeliveryHUD>();
            SetField(hud, "manager", manager);
            SetField(hud, "robot", robot);
            SetField(hud, "statusText", status);
            SetField(hud, "scoreText", score);
            SetField(hud, "arrow", arrow.rectTransform);
        }

        private static Image UiImage(Transform parent, string name, Vector2 anchor, Vector2 pos, Vector2 size, RuntimeSprite.Shape shape)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = rt.pivot = anchor;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            var img = go.AddComponent<Image>();
            var rs = go.AddComponent<RuntimeSprite>();
            SetField(rs, "shape", (int)shape);
            return img;
        }

        private static Text UiText(Transform parent, string name, Font font, int size, Vector2 anchor, Vector2 pos, Vector2 rect)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = rt.pivot = anchor;
            rt.anchoredPosition = pos;
            rt.sizeDelta = rect;
            var t = go.AddComponent<Text>();
            t.font = font;
            t.fontSize = size;
            t.color = Color.white;
            t.alignment = TextAnchor.UpperLeft;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            var shadow = go.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.8f);
            shadow.effectDistance = new Vector2(2f, -2f);
            return t;
        }

        private static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }

        // ---------------------------------------------------------------- helpers

        /// <summary>Sets a private [SerializeField] on a component through SerializedObject.</summary>
        private static void SetField(Object target, string field, object value)
        {
            var so = new SerializedObject(target);
            var prop = so.FindProperty(field);
            if (prop == null)
            {
                Debug.LogWarning($"[SceneBootstrapper] Field '{field}' not found on {target.GetType().Name}");
                return;
            }
            switch (value)
            {
                case Object obj: prop.objectReferenceValue = obj; break;
                case int i: prop.enumValueIndex = i; break;
                case string s: prop.stringValue = s; break;
                default: Debug.LogWarning($"[SceneBootstrapper] Unsupported value type for '{field}'"); break;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
