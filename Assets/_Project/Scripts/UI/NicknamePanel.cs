using DeliveryBot.Delivery;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace DeliveryBot.UI
{
    /// <summary>
    /// Full-screen nickname prompt shown before a round. Reads characters straight from the
    /// Input System keyboard (no EventSystem/InputField needed); Backspace deletes, Enter starts.
    /// Verified with ASCII input; Hangul IME composition through onTextInput is best-effort.
    /// </summary>
    public sealed class NicknamePanel : MonoBehaviour
    {
        [SerializeField] private GameObject panel;
        [SerializeField] private Text nameText;
        [SerializeField] private Text hintText;
        [SerializeField] private GameFlow flow;
        [SerializeField] private float caretBlink = 0.5f;

        private NicknameBuffer _buffer;
        private Keyboard _subscribedTo;

        private void Start()
        {
            if (flow == null) flow = GameFlow.Instance;
            _buffer = new NicknameBuffer(Leaderboard.MaxNameLength, GameFlow.LastNickname);
            if (hintText != null)
                hintText.text = $"Enter  시작      Backspace  지우기      최대 {Leaderboard.MaxNameLength}자 (비우면 '{Leaderboard.DefaultName}')";
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

        private void OnDisable() => Unsubscribe();

        private void OnDestroy()
        {
            Unsubscribe();
            if (flow != null) flow.StateChanged -= OnStateChanged;
        }

        private void OnStateChanged(FlowState state)
        {
            if (panel != null) panel.SetActive(state == FlowState.NameEntry);
        }

        private void Update()
        {
            if (flow == null || flow.State != FlowState.NameEntry) { Unsubscribe(); return; }
            var kb = Keyboard.current;
            if (kb != _subscribedTo)
            {
                Unsubscribe();
                if (kb != null) { kb.onTextInput += OnChar; _subscribedTo = kb; }
            }
            if (kb != null)
            {
                if (kb.backspaceKey.wasPressedThisFrame) _buffer.Backspace();
                if (kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame)
                {
                    flow.BeginRound(_buffer.Commit());
                    return;
                }
            }
            if (nameText != null)
            {
                var caret = Mathf.Repeat(Time.time, caretBlink * 2f) < caretBlink ? "_" : " ";
                nameText.text = _buffer.Text + caret;
            }
        }

        private void OnChar(char c)
        {
            if (flow != null && flow.State == FlowState.NameEntry) _buffer.Append(c);
        }

        private void Unsubscribe()
        {
            if (_subscribedTo != null) _subscribedTo.onTextInput -= OnChar;
            _subscribedTo = null;
        }
    }
}
