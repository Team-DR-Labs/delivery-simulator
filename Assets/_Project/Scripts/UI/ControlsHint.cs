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

        private void Start()
        {
            _hideAt = Time.time + autoHideAfter;
            if (panel != null) panel.SetActive(true);
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
