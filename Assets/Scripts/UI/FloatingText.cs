using UnityEngine;
using UnityEngine.UI;

namespace Elfshift.UI
{
    /// <summary>Scritta (es. "+100") che salta fuori, sale e svanisce. Si crea con FloatingTextSpawner.</summary>
    public sealed class FloatingText : MonoBehaviour
    {
        private Text label;
        private float time;
        private Vector3 start;

        public void Begin(string text, Color color)
        {
            label = GetComponent<Text>();
            label.text = text;
            label.color = color;
            start = transform.position;
            time = 0f;
        }

        private void Update()
        {
            time += Time.unscaledDeltaTime;
            const float life = 1.25f;
            float k = time / life;
            if (k >= 1f)
            {
                Destroy(gameObject);
                return;
            }
            float pop = UiAnim.BackOut(Mathf.Clamp01(time / 0.25f), 3f);
            transform.localScale = Vector3.one * Mathf.Lerp(0.2f, 1.1f, pop) * (1f - Mathf.Max(0f, k - 0.7f) * 0.6f);
            transform.position = start + Vector3.up * (UiAnim.OutCubic(k) * 70f * (Screen.height / 720f));
            Color c = label.color;
            c.a = k < 0.6f ? 1f : 1f - (k - 0.6f) / 0.4f;
            label.color = c;
        }
    }
}
