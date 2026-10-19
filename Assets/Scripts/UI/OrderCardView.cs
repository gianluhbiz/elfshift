using Elfshift.Core;
using Elfshift.Orders;
using UnityEngine;
using UnityEngine.UI;

namespace Elfshift.UI
{
    /// <summary>La "letterina" mostrata in alto: icona del giocattolo, nome, bambino e barra del tempo.</summary>
    public sealed class OrderCardView : MonoBehaviour
    {
        [SerializeField] private RawImage icon;
        [SerializeField] private Text titleText;
        [SerializeField] private Text childText;
        [SerializeField] private Text timeText;
        [SerializeField] private RectTransform timerFill;
        [SerializeField] private Image timerFillImage;
        [SerializeField] private Image background;

        private static readonly Color Calm = new Color(0.35f, 0.78f, 0.45f);
        private static readonly Color Warning = new Color(0.98f, 0.76f, 0.25f);
        private static readonly Color Danger = new Color(0.92f, 0.28f, 0.28f);

        private Order order;
        private int lastShownSecond = -1;
        private float appearTimer;

        public Order Order => order;

        public void Bind(Order newOrder, Texture iconTexture)
        {
            order = newOrder;
            lastShownSecond = -1;
            appearTimer = 0f;
            if (titleText != null) titleText.text = newOrder.Definition.RequestedItem.DisplayName;
            if (childText != null) childText.text = UiText.ForChild(newOrder.ChildName);
            if (icon != null)
            {
                icon.texture = iconTexture;
                icon.enabled = iconTexture != null;
            }
            transform.localScale = Vector3.one * 0.6f;
            Refresh();
        }

        private void Update()
        {
            if (order == null) return;

            // Comparsa con piccolo rimbalzo.
            if (appearTimer < 1f)
            {
                appearTimer = Mathf.Min(1f, appearTimer + Time.unscaledDeltaTime / 0.3f);
                float k = appearTimer;
                float s = 1f + Mathf.Sin(k * Mathf.PI) * 0.12f - (1f - k) * (1f - k) * 0.4f;
                transform.localScale = Vector3.one * s;
            }

            Refresh();
        }

        private void Refresh()
        {
            float n = order.Normalized;
            if (timerFill != null) timerFill.anchorMax = new Vector2(Mathf.Max(0.02f, n), 1f);

            Color c = n > 0.5f ? Calm : (n > 0.25f ? Warning : Danger);
            if (timerFillImage != null) timerFillImage.color = c;

            int seconds = Mathf.CeilToInt(order.TimeLeft);
            if (seconds != lastShownSecond)
            {
                lastShownSecond = seconds;
                if (timeText != null)
                {
                    timeText.text = UiText.FormatTime(seconds);
                    timeText.color = n > 0.25f ? Color.white : Danger;
                }
            }

            // Sotto il 25% la carta pulsa leggermente per attirare l'attenzione.
            if (appearTimer >= 1f)
            {
                float pulse = n <= 0.25f ? 1f + Mathf.Sin(Time.unscaledTime * 8f) * 0.025f : 1f;
                transform.localScale = Vector3.one * pulse;
            }
        }
    }
}
