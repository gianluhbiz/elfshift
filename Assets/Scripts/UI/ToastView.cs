using UnityEngine;
using UnityEngine.UI;

namespace Elfshift.UI
{
    /// <summary>Messaggio breve al centro dello schermo: salta fuori con un rimbalzo e poi si rimpicciolisce svanendo.</summary>
    public sealed class ToastView : MonoBehaviour
    {
        [SerializeField] private Text label;
        [SerializeField] private CanvasGroup group;
        [SerializeField] private float visibleSeconds = 2.2f;

        private float timer;
        private float totalTime;

        private void Awake()
        {
            if (group != null) group.alpha = 0f;
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
            totalTime = Mathf.Max(0.8f, seconds);
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
            float elapsed = totalTime - timer;
            float pop = Mathf.LerpUnclamped(0.85f, 1f, UiAnim.BackOut(Mathf.Clamp01(elapsed / 0.35f), 2f));
            float outK = Mathf.Clamp01(1f - timer / 0.3f); // ultimi 0,3 s
            group.alpha = Mathf.Clamp01(elapsed / 0.08f) * (1f - outK);
            transform.localScale = Vector3.one * (pop * (1f - outK * 0.15f));
        }
    }
}
