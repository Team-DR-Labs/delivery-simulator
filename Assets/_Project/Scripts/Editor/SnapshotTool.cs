using System.IO;
using DeliveryBot.Delivery;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace DeliveryBot.EditorTools
{
    /// <summary>
    /// Renders a handful of viewpoints (shops, houses, apartment, street) of the City scene to PNG files
    /// so the look can be reviewed without opening the editor. Output dir: -snapshotDir &lt;path&gt; or Snapshots/.
    /// </summary>
    public static class SnapshotTool
    {
        [MenuItem("DeliveryBot/Capture Snapshots")]
        public static void Capture()
        {
            var dir = ArgAfter("-snapshotDir") ?? Path.Combine(Directory.GetCurrentDirectory(), "Snapshots");
            Directory.CreateDirectory(dir);
            EditorSceneManager.OpenScene(BuildKit.Root + "/Scenes/City.unity", OpenSceneMode.Single);

            var camGo = new GameObject("SnapshotCamera");
            var cam = camGo.AddComponent<Camera>();
            cam.fieldOfView = 60f;
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 600f;
            var rt = new RenderTexture(1280, 720, 24);
            cam.targetTexture = rt;

            var points = Object.FindObjectsByType<DeliveryPoint>();
            var shot = 0;
            foreach (var kind in new[] { PointKind.Shop, PointKind.Home })
            {
                var n = 0;
                foreach (var p in points)
                {
                    if (p.Kind != kind || n >= 3) continue;
                    var t = p.transform;
                    var eye = t.position + t.forward * 9f + Vector3.up * 2.2f + t.right * 3f;
                    var look = t.position - t.forward * 3f + Vector3.up * 2.5f;
                    Save(cam, rt, eye, look, Path.Combine(dir, $"{++shot:00}_{kind}_{Sanitize(p.DisplayName)}.png"));
                    n++;
                }
            }
            // Street overview from the robot's default third-person height.
            var robot = GameObject.FindWithTag("Player");
            if (robot != null)
                Save(cam, rt, robot.transform.position + new Vector3(0f, 2.4f, -5f), robot.transform.position + Vector3.up + robot.transform.forward * 10f, Path.Combine(dir, $"{++shot:00}_street.png"));
            Save(cam, rt, new Vector3(0f, 90f, -60f), Vector3.zero, Path.Combine(dir, $"{++shot:00}_overview.png"));

            Object.DestroyImmediate(camGo);
            Object.DestroyImmediate(rt);
            Debug.Log($"[SnapshotTool] wrote {shot} snapshots to {dir}");
        }

        private static void Save(Camera cam, RenderTexture rt, Vector3 eye, Vector3 look, string path)
        {
            cam.transform.position = eye;
            cam.transform.rotation = Quaternion.LookRotation(look - eye, Vector3.up);
            cam.Render();
            var prev = RenderTexture.active;
            RenderTexture.active = rt;
            var tex = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
            tex.Apply();
            RenderTexture.active = prev;
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
        }

        private static string ArgAfter(string flag)
        {
            var args = System.Environment.GetCommandLineArgs();
            for (var i = 0; i < args.Length - 1; i++)
                if (args[i] == flag) return args[i + 1];
            return null;
        }

        private static string Sanitize(string s)
        {
            foreach (var c in Path.GetInvalidFileNameChars()) s = s.Replace(c, '_');
            return s.Replace(' ', '_');
        }
    }
}
