using DeliveryBot.Delivery;
using DeliveryBot.World;
using UnityEngine;
using UnityEngine.UI;

namespace DeliveryBot.EditorTools
{
    /// <summary>Creates a DeliveryPoint (trigger, beam marker, floating name sign) at a doorstep.</summary>
    public static class PointMarkerFactory
    {
        private static Material _marker, _signBoard, _mat;
        private static Font _font;

        private static void Load()
        {
            _marker ??= BuildKit.Mat("Marker", Color.white, emissive: true);
            _signBoard ??= BuildKit.Mat("SignBoard", new Color(0.12f, 0.12f, 0.14f));
            _mat ??= BuildKit.Mat("DoorMat", new Color(0.45f, 0.3f, 0.2f));
            _font ??= Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }

        public static DeliveryPoint Create(Transform parent, Vector3 pos, Vector3 facing, string name, PointKind kind, string category)
        {
            Load();
            var go = BuildKit.Node($"{(kind == PointKind.Shop ? "Shop" : "Home")}_{name}", parent, pos);
            go.transform.rotation = Quaternion.LookRotation(facing, Vector3.up);
            var trigger = go.AddComponent<SphereCollider>();
            trigger.isTrigger = true;
            trigger.radius = 6f;

            BuildKit.Prim(PrimitiveType.Cube, "DoorMat", go.transform, new Vector3(0f, 0.02f, 0f), new Vector3(1.4f, 0.03f, 0.9f), _mat, isStatic: true);

            var marker = BuildKit.Node("Marker", go.transform, Vector3.zero);
            BuildKit.Prim(PrimitiveType.Cylinder, "Pad", marker.transform, new Vector3(0f, 0.04f, 1.5f), new Vector3(5f, 0.03f, 5f), _marker);
            var beam = BuildKit.Prim(PrimitiveType.Cylinder, "Beam", marker.transform, new Vector3(0f, 16f, 1.5f), new Vector3(0.5f, 14f, 0.5f), _marker);
            beam.AddComponent<PulseScale>();
            BuildKit.Prim(PrimitiveType.Cube, "SignBoard", marker.transform, new Vector3(0f, 5.2f, 1.5f), new Vector3(3.6f, 0.9f, 0.1f), _signBoard);
            var text = WorldText(marker.transform, new Vector3(0f, 5.2f, 1.44f), name, 0.02f);
            text.transform.parent.localRotation = Quaternion.identity; // Billboard points marker +z away from camera; canvas front (-z) must face the camera
            marker.AddComponent<Billboard>();

            var dp = go.AddComponent<DeliveryPoint>();
            dp.Configure(name, kind, category, marker, text);
            marker.SetActive(false);
            return dp;
        }

        /// <summary>World-space legacy Text on a tiny canvas (front face = local -z, so rotate parent to face the viewer).</summary>
        public static Text WorldText(Transform parent, Vector3 localPos, string content, float scale, int fontSize = 28, Color? color = null)
        {
            Load();
            var canvasGo = new GameObject("Sign", typeof(RectTransform));
            canvasGo.transform.SetParent(parent, false);
            canvasGo.transform.localPosition = localPos;
            canvasGo.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            canvasGo.transform.localScale = Vector3.one * scale;
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvasGo.GetComponent<RectTransform>().sizeDelta = new Vector2(220f, 40f);

            var textGo = new GameObject("Text", typeof(RectTransform));
            textGo.transform.SetParent(canvasGo.transform, false);
            var trt = textGo.GetComponent<RectTransform>();
            trt.anchorMin = Vector2.zero;
            trt.anchorMax = Vector2.one;
            trt.offsetMin = trt.offsetMax = Vector2.zero;
            var text = textGo.AddComponent<Text>();
            text.font = _font;
            text.fontSize = fontSize;
            text.fontStyle = FontStyle.Bold;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = color ?? Color.white;
            text.text = content;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            return text;
        }
    }
}
