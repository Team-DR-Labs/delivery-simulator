using DeliveryBot.Delivery;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace DeliveryBot.UI
{
    /// <summary>Round-over screen: this run's summary plus the local top-10 with the new row highlighted. Enter or click continues.</summary>
    public sealed class ResultsPanel : MonoBehaviour
    {
        [SerializeField] private GameObject panel;
        [SerializeField] private Text titleText;
        [SerializeField] private Text summaryText;
        [SerializeField] private Text[] rowLeft;
        [SerializeField] private Text[] rowRight;
        [SerializeField] private Text footerText;
        [SerializeField] private GameFlow flow;
        [SerializeField] private Color highlight = new Color(1f, 0.85f, 0.3f);
        [Tooltip("Ignore Enter/click for this long after the screen appears so a held key does not skip it.")]
        [SerializeField] private float inputGrace = 0.4f;

        private float _shownAt;

        private void Start()
        {
            if (flow == null) flow = GameFlow.Instance;
            if (flow != null)
            {
                flow.StateChanged += OnStateChanged;
                OnStateChanged(flow.State);
            }
            else if (panel != null)
            {
                panel.SetActive(false);
            }
        }

        private void OnDestroy()
        {
            if (flow != null) flow.StateChanged -= OnStateChanged;
        }

        private void OnStateChanged(FlowState state)
        {
            var show = state == FlowState.Results;
            if (panel != null) panel.SetActive(show);
            if (!show) return;
            _shownAt = Time.time;
            Populate();
        }

        private void Populate()
        {
            var e = flow.LastEntry;
            if (titleText != null) titleText.text = "라운드 종료";
            if (summaryText != null && e != null)
                summaryText.text = $"{e.name}   배달 {e.deliveries}건   페널티 {e.penaltySeconds:F0}초";

            var top = Leaderboard.Top(flow.Board, Leaderboard.TopCount);
            var rows = Mathf.Min(rowLeft?.Length ?? 0, rowRight?.Length ?? 0);
            for (var i = 0; i < rows; i++)
            {
                var has = i < top.Count;
                var mine = has && ReferenceEquals(top[i], e);
                var color = mine ? highlight : Color.white;
                rowLeft[i].text = has ? $"{i + 1}.  {top[i].name}" : "";
                rowRight[i].text = has ? $"{top[i].deliveries}건   {Leaderboard.FormatClock(top[i].lastDeliverySeconds)}" : "";
                rowLeft[i].color = color;
                rowRight[i].color = color;
            }

            if (footerText != null)
            {
                var rank = flow.LastRank;
                var mine = rank < 0 ? "순위권 밖" : rank >= Leaderboard.TopCount ? $"내 순위: {rank + 1}위" : "";
                footerText.text = string.IsNullOrEmpty(mine) ? "Enter / 클릭  →  다시 하기" : $"{mine}      Enter / 클릭  →  다시 하기";
            }
        }

        private void Update()
        {
            if (flow == null || flow.State != FlowState.Results) return;
            if (Time.time - _shownAt < inputGrace) return;
            var kb = Keyboard.current;
            var enter = kb != null && (kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame);
            var click = Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
            if (enter || click) flow.ReturnToNameEntry();
        }
    }
}
