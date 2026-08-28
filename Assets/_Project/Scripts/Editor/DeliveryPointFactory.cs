using System.Collections.Generic;
using DeliveryBot.Delivery;
using DeliveryBot.World;
using UnityEngine;
using UnityEngine.UI;

namespace DeliveryBot.EditorTools
{
    /// <summary>Places storefront delivery points on sidewalks (never on park blocks) with markers and name signs.</summary>
    public static class DeliveryPointFactory
    {
        private static readonly string[] Names =
        {
            "김밥천국", "편의점", "약국", "치킨집", "피자가게", "카페 라떼", "떡볶이집", "분식왕", "베이커리", "꽃집",
            "서점", "문구점", "세탁소", "마트", "정육점", "국밥집", "초밥집", "버거하우스", "아이스크림", "반찬가게",
            "핸드폰 대리점", "미용실", "안경점", "과일가게", "도넛샵", "라멘집", "쌀국수", "타코집", "샐러드바", "빙수집"
        };

        public static List<DeliveryPoint> Create(Transform parent, RoadGraph g, HashSet<(int, int)> parks, int count, float sidewalkHeight, System.Random rng)
        {
            var marker = BuildKit.Mat("Marker", Color.white, emissive: true);
            var sign = BuildKit.Mat("SignBoard", new Color(0.12f, 0.12f, 0.14f));
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            var candidates = new List<(int bx, int bz, int side)>();
            for (var bx = 0; bx < g.Blocks; bx++)
            for (var bz = 0; bz < g.Blocks; bz++)
            {
                if (parks.Contains((bx, bz))) continue;
                for (var side = 0; side < 4; side++) candidates.Add((bx, bz, side));
            }

            var names = new List<string>(Names);
            var points = new List<DeliveryPoint>();
            for (var n = 0; n < count && candidates.Count > 0; n++)
            {
                var pick = candidates[rng.Next(candidates.Count)];
                candidates.RemoveAll(c => c.bx == pick.bx && c.bz == pick.bz && c.side == pick.side);
                var pos = g.StorefrontPoint(pick.bx, pick.bz, pick.side) + Vector3.up * sidewalkHeight;
                var normal = RoadGraph.SideNormal(pick.side);
                var name = names.Count > 0 ? names[rng.Next(names.Count)] : $"지점 {n}";
                names.Remove(name);
                points.Add(CreateOne(parent, pos, normal, name, marker, sign, font));
            }
            return points;
        }

        private static DeliveryPoint CreateOne(Transform parent, Vector3 pos, Vector3 normal, string name, Material markerMat, Material signMat, Font font)
        {
            var go = BuildKit.Node($"DeliveryPoint_{name}", parent, pos);
            go.transform.rotation = Quaternion.LookRotation(normal, Vector3.up);
            var trigger = go.AddComponent<SphereCollider>();
            trigger.isTrigger = true;
            trigger.radius = 4f;

            var marker = BuildKit.Node("Marker", go.transform, Vector3.zero);
            BuildKit.Prim(PrimitiveType.Cylinder, "Pad", marker.transform, new Vector3(0f, 0.04f, 1.2f), new Vector3(5f, 0.03f, 5f), markerMat);
            var beam = BuildKit.Prim(PrimitiveType.Cylinder, "Beam", marker.transform, new Vector3(0f, 14f, 0f), new Vector3(0.5f, 14f, 0.5f), markerMat);
            beam.AddComponent<PulseScale>();
            BuildKit.Prim(PrimitiveType.Cube, "SignBoard", marker.transform, new Vector3(0f, 3.2f, 0f), new Vector3(3.4f, 0.9f, 0.1f), signMat);
            var text = WorldText(marker.transform, new Vector3(0f, 3.2f, -0.06f), name, font);

            // Permanent small shop sign so the city looks lived-in even when the point is not active.
            var shopSign = BuildKit.Prim(PrimitiveType.Cube, "ShopSign", go.transform, new Vector3(0f, 2.2f, -0.9f), new Vector3(2.6f, 0.6f, 0.08f), signMat);
            var shopText = WorldText(shopSign.transform, Vector3.zero, name, font, 0.012f);
            shopText.transform.localPosition = new Vector3(0f, 0f, -0.55f);

            var dp = go.AddComponent<DeliveryPoint>();
            dp.Configure(name, marker, text);
            marker.SetActive(false);
            return dp;
        }

        private static Text WorldText(Transform parent, Vector3 localPos, string content, Font font, float scale = 0.02f)
        {
            var canvasGo = new GameObject("Sign", typeof(RectTransform));
            canvasGo.transform.SetParent(parent, false);
            canvasGo.transform.localPosition = localPos;
            canvasGo.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            canvasGo.transform.localScale = Vector3.one * scale;
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            var rt = canvasGo.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(200f, 40f);

            var textGo = new GameObject("Text", typeof(RectTransform));
            textGo.transform.SetParent(canvasGo.transform, false);
            var trt = textGo.GetComponent<RectTransform>();
            trt.anchorMin = Vector2.zero;
            trt.anchorMax = Vector2.one;
            trt.offsetMin = trt.offsetMax = Vector2.zero;
            var text = textGo.AddComponent<Text>();
            text.font = font;
            text.fontSize = 28;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.text = content;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            return text;
        }
    }
}
