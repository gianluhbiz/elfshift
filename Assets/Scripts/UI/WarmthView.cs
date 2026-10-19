using Elfshift.Production;
using UnityEngine;
using UnityEngine.UI;

namespace Elfshift.UI
{
    /// <summary>Indicatore del calore del camino: una barretta che si svuota e cambia colore, con la fiammella che trema.</summary>
    public sealed class WarmthView : MonoBehaviour
    {
        [SerializeField] private Fireplace fireplace;
        [SerializeField] private RectTransform fill;
        [SerializeField] private Image fillImage;
        [SerializeField] private RectTransform icon;
        [SerializeField] private Color coldColor = new Color(0.45f, 0.65f, 0.9f);
        [SerializeField] private Color warmColor = new Color(0.98f, 0.66f, 0.27f);
        [SerializeField] private Color cozyColor = new Color(0.93f, 0.42f, 0.27f);

        private float shown = -1f;

        private void Update()
        {
            if (fireplace == null) return;
            float target = fireplace.Warmth01;
            shown = shown < 0f ? target : Mathf.Lerp(shown, target, 1f - Mathf.Exp(-6f * Time.unscaledDeltaTime));
            if (fill != null) fill.anchorMax = new Vector2(Mathf.Max(0.04f, shown), 1f);
            if (fillImage != null) fillImage.color = fireplace.IsCold ? coldColor : (fireplace.IsCozy ? cozyColor : warmColor);
            if (icon != null)
            {
                // Con il freddo l'icona trema; con il fuoco vivace "respira".
                float t = Time.unscaledTime;
                float s = fireplace.IsCold ? 1f : 1f + Mathf.Sin(t * 4f) * 0.06f * shown;
                icon.localScale = Vector3.one * s;
                icon.localRotation = Quaternion.Euler(0f, 0f, fireplace.IsCold ? Mathf.Sin(t * 30f) * 6f : 0f);
            }
        }
    }
}
