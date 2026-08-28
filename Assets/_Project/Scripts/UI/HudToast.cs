using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace DeliveryBot.UI
{
    /// <summary>Centre-screen message that pops in, holds, and fades out.</summary>
    [RequireComponent(typeof(CanvasGroup))]
    public sealed class HudToast : MonoBehaviour
    {
        [SerializeField] private Text text;
        [SerializeField] private float hold = 1.4f;

        private CanvasGroup _group;
        private Coroutine _running;

        private void Awake()
        {
            _group = GetComponent<CanvasGroup>();
            _group.alpha = 0f;
            if (text == null) text = GetComponentInChildren<Text>();
        }

        public void Show(string message, Color color)
        {
            if (text != null)
            {
                text.text = message;
                text.color = color;
            }
            if (_running != null) StopCoroutine(_running);
            _running = StartCoroutine(Animate());
        }

        private IEnumerator Animate()
        {
            var t = 0f;
            while (t < 0.25f)
            {
                t += Time.deltaTime;
                var k = Mathf.SmoothStep(0f, 1f, t / 0.25f);
                _group.alpha = k;
                transform.localScale = Vector3.one * Mathf.Lerp(0.6f, 1f, k);
                yield return null;
            }
            yield return new WaitForSeconds(hold);
            t = 0f;
            while (t < 0.5f)
            {
                t += Time.deltaTime;
                _group.alpha = 1f - t / 0.5f;
                yield return null;
            }
            _group.alpha = 0f;
            _running = null;
        }
    }
}
