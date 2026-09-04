using DeliveryBot.CameraSystem;
using DeliveryBot.Delivery;
using DeliveryBot.Minimap;
using DeliveryBot.UI;
using DeliveryBot.Vehicle;
using UnityEngine;
using UnityEngine.UI;

namespace DeliveryBot.EditorTools
{
    /// <summary>Builds the screen-space HUD: order card, minimap, arrow, speed, toast, flash, controls hint.</summary>
    public static class HudBuilder
    {
        public static DeliveryHUD Build(DeliveryManager manager, GameObject robot, CameraRig rig, RenderTexture minimapRT, float minimapOrthoSize, GameFlow flow = null)
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
            var root = canvasGo.transform;

            // Full-screen flash (behind everything else).
            var flash = Panel(root, "Flash", new Color(1f, 0f, 0f, 0f));
            Stretch(flash.rectTransform);

            // Order card (top-left).
            var card = Panel(root, "OrderCard", new Color(0.05f, 0.06f, 0.09f, 0.6f));
            Place(card.rectTransform, new Vector2(0f, 1f), new Vector2(28f, -28f), new Vector2(560f, 160f));
            var title = Text(card.transform, "Title", font, 34, new Vector2(0f, 1f), new Vector2(20f, -14f), new Vector2(520f, 46f), FontStyle.Bold);
            var info = Text(card.transform, "Info", font, 28, new Vector2(0f, 1f), new Vector2(20f, -62f), new Vector2(520f, 40f));
            var score = Text(card.transform, "Score", font, 20, new Vector2(0f, 1f), new Vector2(20f, -106f), new Vector2(520f, 40f));
            score.color = new Color(0.85f, 0.85f, 0.85f);

            // Minimap (top-right).
            var border = Image(root, "MinimapBorder", new Vector2(1f, 1f), new Vector2(-28f, -28f), new Vector2(316f, 316f), RuntimeSprite.Shape.Circle);
            border.color = new Color(1f, 1f, 1f, 0.9f);
            var mask = Image(root, "MinimapMask", new Vector2(1f, 1f), new Vector2(-34f, -34f), new Vector2(304f, 304f), RuntimeSprite.Shape.Circle);
            mask.gameObject.AddComponent<Mask>().showMaskGraphic = false;
            var view = new GameObject("MinimapView", typeof(RectTransform)).AddComponent<RawImage>();
            view.transform.SetParent(mask.transform, false);
            Stretch(view.rectTransform);
            view.texture = minimapRT;
            var blip = Image(mask.transform, "TargetBlip", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(22f, 22f), RuntimeSprite.Shape.Circle);
            blip.color = new Color(1f, 0.6f, 0.1f);
            blip.gameObject.AddComponent<UiPulse>();
            var robotIcon = Image(mask.transform, "RobotIcon", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(22f, 22f), RuntimeSprite.Shape.Triangle);
            robotIcon.color = new Color(1f, 0.9f, 0.1f);

            // Arrow (bottom-centre) and speed (bottom-right).
            var arrow = Image(root, "TargetArrow", new Vector2(0.5f, 0f), new Vector2(0f, 120f), new Vector2(90f, 90f), RuntimeSprite.Shape.Triangle);
            arrow.color = new Color(1f, 0.6f, 0.1f);
            var speed = Text(root, "Speed", font, 30, new Vector2(1f, 0f), new Vector2(-30f, 30f), new Vector2(260f, 80f));
            speed.alignment = TextAnchor.LowerRight;

            // Round countdown (top-centre).
            var timer = Text(root, "Timer", font, 52, new Vector2(0.5f, 1f), new Vector2(0f, -28f), new Vector2(600f, 70f), FontStyle.Bold);
            timer.alignment = TextAnchor.UpperCenter;

            // Interaction prompt (above the arrow).
            var prompt = Text(root, "Prompt", font, 40, new Vector2(0.5f, 0f), new Vector2(0f, 230f), new Vector2(900f, 60f), FontStyle.Bold);
            prompt.alignment = TextAnchor.MiddleCenter;
            prompt.color = new Color(1f, 0.95f, 0.6f);
            prompt.gameObject.AddComponent<UiPulse>();
            BuildKit.SetField(prompt.GetComponent<UiPulse>(), "amplitude", 0.04f);

            // Toast (centre).
            var toastGo = new GameObject("Toast", typeof(RectTransform), typeof(CanvasGroup));
            toastGo.transform.SetParent(root, false);
            Place(toastGo.GetComponent<RectTransform>(), new Vector2(0.5f, 0.7f), Vector2.zero, new Vector2(1200f, 80f));
            var toastText = Text(toastGo.transform, "Text", font, 44, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1200f, 80f), FontStyle.Bold);
            toastText.alignment = TextAnchor.MiddleCenter;
            var toast = toastGo.AddComponent<HudToast>();
            BuildKit.SetField(toast, "text", toastText);

            // Controls hint (bottom-left, above the debug status line).
            var hint = Panel(root, "ControlsHint", new Color(0.05f, 0.06f, 0.09f, 0.6f));
            Place(hint.rectTransform, new Vector2(0f, 0f), new Vector2(28f, 70f), new Vector2(430f, 190f));
            var hintText = Text(hint.transform, "Text", font, 20, new Vector2(0f, 1f), new Vector2(16f, -12f), new Vector2(400f, 170f));
            hintText.text = "조작법  [H] 숨기기/보기\nW/↑ 가속   S/↓ 브레이크   A/D ←/→ 조향\nShift+W 후진   Space 핸드브레이크\nV 시점 전환   R 재시작   F1 입력 디버그\n※ 키가 안 먹으면 게임 화면을 한 번 클릭";
            var hintComp = canvasGo.AddComponent<ControlsHint>();
            BuildKit.SetField(hintComp, "panel", hint.gameObject);

            var hud = canvasGo.AddComponent<DeliveryHUD>();
            BuildKit.SetField(hud, "manager", manager);
            BuildKit.SetField(hud, "robot", robot.transform);
            BuildKit.SetField(hud, "robotController", robot.GetComponent<RobotController>());
            BuildKit.SetField(hud, "cameraRig", rig);
            BuildKit.SetField(hud, "titleText", title);
            BuildKit.SetField(hud, "infoText", info);
            BuildKit.SetField(hud, "scoreText", score);
            BuildKit.SetField(hud, "speedText", speed);
            BuildKit.SetField(hud, "promptText", prompt);
            BuildKit.SetField(hud, "arrow", arrow.rectTransform);
            BuildKit.SetField(hud, "minimapBlip", blip.rectTransform);
            BuildKit.SetField(hud, "minimapRadiusPx", 152f);
            BuildKit.SetField(hud, "minimapOrthoSize", minimapOrthoSize);
            BuildKit.SetField(hud, "flash", flash);
            BuildKit.SetField(hud, "toast", toast);
            BuildKit.SetField(hud, "timerText", timer);
            BuildKit.SetField(hud, "flow", flow);

            // Menus last so they draw over everything else (sibling order = draw order).
            BuildNicknamePanel(root, font, canvasGo, flow);
            BuildResultsPanel(root, font, canvasGo, flow);
            return hud;
        }

        private static readonly Color DimColor = new Color(0.02f, 0.03f, 0.05f, 0.82f);
        private static readonly Color CardColor = new Color(0.05f, 0.06f, 0.09f, 0.6f);
        private static readonly Color AccentColor = new Color(1f, 0.6f, 0.1f);

        private static void BuildNicknamePanel(Transform root, Font font, GameObject canvasGo, GameFlow flow)
        {
            var dim = Panel(root, "NameEntry", DimColor);
            Stretch(dim.rectTransform);
            var title = Text(dim.transform, "Title", font, 48, new Vector2(0.5f, 0.62f), Vector2.zero, new Vector2(1000f, 70f), FontStyle.Bold);
            title.alignment = TextAnchor.MiddleCenter;
            title.text = "닉네임을 입력하세요";
            var name = Text(dim.transform, "Name", font, 64, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1000f, 90f), FontStyle.Bold);
            name.alignment = TextAnchor.MiddleCenter;
            name.color = AccentColor;
            var underline = Panel(dim.transform, "Underline", new Color(1f, 1f, 1f, 0.5f));
            Place(underline.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, -52f), new Vector2(640f, 3f));
            var hint = Text(dim.transform, "Hint", font, 26, new Vector2(0.5f, 0.38f), Vector2.zero, new Vector2(1200f, 40f));
            hint.alignment = TextAnchor.MiddleCenter;
            hint.color = new Color(0.85f, 0.85f, 0.85f);

            var comp = canvasGo.AddComponent<NicknamePanel>();
            BuildKit.SetField(comp, "panel", dim.gameObject);
            BuildKit.SetField(comp, "nameText", name);
            BuildKit.SetField(comp, "hintText", hint);
            BuildKit.SetField(comp, "flow", flow);
        }

        private static void BuildResultsPanel(Transform root, Font font, GameObject canvasGo, GameFlow flow)
        {
            var dim = Panel(root, "Results", DimColor);
            Stretch(dim.rectTransform);
            var title = Text(dim.transform, "Title", font, 48, new Vector2(0.5f, 0.9f), Vector2.zero, new Vector2(1000f, 70f), FontStyle.Bold);
            title.alignment = TextAnchor.MiddleCenter;
            title.text = "라운드 종료";
            var summary = Text(dim.transform, "Summary", font, 30, new Vector2(0.5f, 0.82f), Vector2.zero, new Vector2(1200f, 44f));
            summary.alignment = TextAnchor.MiddleCenter;
            summary.color = AccentColor;

            var card = Panel(dim.transform, "Card", CardColor);
            Place(card.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, -20f), new Vector2(900f, 560f));
            var header = Text(card.transform, "Header", font, 24, new Vector2(0f, 1f), new Vector2(40f, -14f), new Vector2(820f, 34f));
            header.text = "순위   닉네임";
            header.color = new Color(0.7f, 0.7f, 0.7f);
            var headerR = Text(card.transform, "HeaderR", font, 24, new Vector2(1f, 1f), new Vector2(-40f, -14f), new Vector2(400f, 34f));
            headerR.alignment = TextAnchor.UpperRight;
            headerR.text = "배달   마지막 배달 시각";
            headerR.color = new Color(0.7f, 0.7f, 0.7f);

            var rowLeft = new Text[Leaderboard.TopCount];
            var rowRight = new Text[Leaderboard.TopCount];
            for (var i = 0; i < Leaderboard.TopCount; i++)
            {
                var y = -56f - i * 48f;
                rowLeft[i] = Text(card.transform, $"RowL{i}", font, 30, new Vector2(0f, 1f), new Vector2(40f, y), new Vector2(520f, 44f));
                rowRight[i] = Text(card.transform, $"RowR{i}", font, 30, new Vector2(1f, 1f), new Vector2(-40f, y), new Vector2(400f, 44f));
                rowRight[i].alignment = TextAnchor.UpperRight;
            }

            var footer = Text(dim.transform, "Footer", font, 28, new Vector2(0.5f, 0.1f), Vector2.zero, new Vector2(1200f, 40f));
            footer.alignment = TextAnchor.MiddleCenter;
            footer.color = new Color(1f, 0.95f, 0.6f);
            footer.gameObject.AddComponent<UiPulse>();
            BuildKit.SetField(footer.GetComponent<UiPulse>(), "amplitude", 0.03f);

            var comp = canvasGo.AddComponent<ResultsPanel>();
            BuildKit.SetField(comp, "panel", dim.gameObject);
            BuildKit.SetField(comp, "titleText", title);
            BuildKit.SetField(comp, "summaryText", summary);
            BuildKit.SetField(comp, "rowLeft", rowLeft);
            BuildKit.SetField(comp, "rowRight", rowRight);
            BuildKit.SetField(comp, "footerText", footer);
            BuildKit.SetField(comp, "flow", flow);
        }

        private static Image Panel(Transform parent, string name, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var img = go.AddComponent<Image>();
            img.color = color;
            img.raycastTarget = false;
            return img;
        }

        private static Image Image(Transform parent, string name, Vector2 anchor, Vector2 pos, Vector2 size, RuntimeSprite.Shape shape)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            Place(go.GetComponent<RectTransform>(), anchor, pos, size);
            var img = go.AddComponent<Image>();
            img.raycastTarget = false;
            var rs = go.AddComponent<RuntimeSprite>();
            BuildKit.SetEnum(rs, "shape", (int)shape);
            return img;
        }

        private static Text Text(Transform parent, string name, Font font, int size, Vector2 anchor, Vector2 pos, Vector2 rect, FontStyle style = FontStyle.Normal)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            Place(go.GetComponent<RectTransform>(), anchor, pos, rect);
            var t = go.AddComponent<Text>();
            t.font = font;
            t.fontSize = size;
            t.fontStyle = style;
            t.color = Color.white;
            t.alignment = TextAnchor.UpperLeft;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.raycastTarget = false;
            var shadow = go.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.7f);
            shadow.effectDistance = new Vector2(1.5f, -1.5f);
            return t;
        }

        private static void Place(RectTransform rt, Vector2 anchor, Vector2 pos, Vector2 size)
        {
            rt.anchorMin = rt.anchorMax = rt.pivot = anchor;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
        }

        private static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }
    }
}
