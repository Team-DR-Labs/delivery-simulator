using UnityEngine;
using UnityEngine.InputSystem;

namespace DeliveryBot.UI
{
    /// <summary>Controls card shown for the first seconds; H toggles it afterwards.</summary>
    public sealed class ControlsHint : MonoBehaviour
    {
        [SerializeField] private GameObject panel;
        [SerializeField] private float autoHideAfter = 12f;

        private float _hideAt;
        private bool _autoHidden;

        private const string HintText =
            "조작법  [H] 숨기기/보기\n" +
            "W/↑ 또는 RT  가속      S/↓ 또는 LT  브레이크 → 멈추면 후진\n" +
            "A/D ←/→ 또는 왼쪽 스틱  조향      Space  핸드브레이크\n" +
            "V (R스틱 클릭)  시점 전환      R  재시작      F1  입력 디버그\n" +
            "※ 키가 안 먹으면 게임 화면을 한 번 클릭";

        private void Start()
        {
            _hideAt = Time.time + autoHideAfter;
            if (panel == null) return;
            panel.SetActive(true);
            var text = panel.GetComponentInChildren<UnityEngine.UI.Text>();
            if (text != null) text.text = HintText;
        }

        private void Update()
        {
            if (panel == null) return;
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
