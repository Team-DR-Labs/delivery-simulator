using DeliveryBot.Delivery;
using UnityEngine;
using UnityEngine.InputSystem;

namespace DeliveryBot.UI
{
    /// <summary>Controls card shown for the first seconds of a round; H toggles it afterwards. Hidden while a menu is open.</summary>
    public sealed class ControlsHint : MonoBehaviour
    {
        [SerializeField] private GameObject panel;
        [SerializeField] private float autoHideAfter = 12f;

        private float _hideAt;
        private bool _autoHidden;
        private GameFlow _flow;

        private const string HintText =
            "조작법  [H] 숨기기/보기\n" +
            "W/↑ 또는 RT  가속      S/↓ 또는 LT  브레이크 → 멈추면 후진\n" +
            "A/D ←/→ 또는 왼쪽 스틱  조향      Space  핸드브레이크\n" +
            "V (R스틱 클릭)  시점 전환      R  라운드 재시작      F1  입력 디버그\n" +
            "※ 키가 안 먹으면 게임 화면을 한 번 클릭";

        private void Start()
        {
            if (panel == null) return;
            var text = panel.GetComponentInChildren<UnityEngine.UI.Text>();
            if (text != null) text.text = HintText;

            _flow = GameFlow.Instance;
            if (_flow != null)
            {
                _flow.StateChanged += OnStateChanged;
                OnStateChanged(_flow.State);
            }
            else
            {
                ShowForAWhile();
            }
        }

        private void OnDestroy()
        {
            if (_flow != null) _flow.StateChanged -= OnStateChanged;
        }

        private void OnStateChanged(FlowState state)
        {
            if (state == FlowState.Playing) ShowForAWhile();
            else panel.SetActive(false);
        }

        private void ShowForAWhile()
        {
            _autoHidden = false;
            _hideAt = Time.time + autoHideAfter;
            panel.SetActive(true);
        }

        private void Update()
        {
            if (panel == null || GameFlow.MenuOpen) return;
            if (!_autoHidden && Time.time > _hideAt)
            {
                _autoHidden = true;
                panel.SetActive(false);
            }
            var kb = Keyboard.current;
            if (kb != null && kb.hKey.wasPressedThisFrame)
            {
                _autoHidden = true;
                panel.SetActive(!panel.activeSelf);
            }
        }
    }
}
