using UnityEngine;
using UnityEngine.UI;

namespace Elfshift.UI
{
    /// <summary>Messaggio breve al centro dello schermo che compare e svanisce.</summary>
    public sealed class ToastView : MonoBehaviour
    {
        [SerializeField] private Text label;
        [SerializeField] private CanvasGroup group;
        [SerializeField] private float visibleSeconds = 1.8f;

        private float timer;
        private float totalTime;
        private Vector2 basePosition;

        private void Awake()
        {
            if (group != null) group.alpha = 0f;
            basePosition = ((RectTransform)transform).anchoredPosition;
        }

        /// <summary>Mostra un messaggio per il tempo predefinito.</summary>
        public void Show(string message)
        {
            Show(message, visibleSeconds);
        }

        /// <summary>Mostra un messaggio per un tempo scelto (secondi).</summary>
        public void Show(string message, float seconds)
        {
            if (label == null || group == null || string.IsNullOrEmpty(message)) return;
            label.text = message;
            totalTime = Mathf.Max(0.5f, seconds);
            timer = totalTime;
        }

        private void Update()
        {
            if (group == null) return;
            if (timer <= 0f)
            {
                if (group.alpha > 0f) group.alpha = 0f;
                return;
            }
            timer -= Time.unscaledDeltaTime;
            float fadeIn = Mathf.Clamp01((totalTime - timer) / 0.12f);
            float fadeOut = Mathf.Clamp01(timer / 0.35f);
            group.alpha = Mathf.Min(fadeIn, fadeOut);
            float rise = (1f - Mathf.Clamp01(timer / totalTime)) * 16f;
            ((RectTransform)transform).anchoredPosition = basePosition + new Vector2(0f, rise);
        }
    }
}
