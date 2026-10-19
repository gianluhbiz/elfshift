using System;
using System.Collections;
using Elfshift.Core;
using Elfshift.Orders;
using UnityEngine;
using UnityEngine.UI;

namespace Elfshift.UI
{
    /// <summary>
    /// La "letterina" mostrata in alto: icona del giocattolo, nome, bambino e barra del tempo.
    /// Entra con un rimbalzo, dondola piano, trema quando il tempo sta per finire e,
    /// alla consegna, fa festa (timbro "OK!") prima di volare via.
    /// Tutta la grafica sta nel figlio "Visual", così il layout non interferisce con le animazioni.
    /// </summary>
    public sealed class OrderCardView : MonoBehaviour
    {
        [SerializeField] private RectTransform visual;
        [SerializeField] private CanvasGroup visualGroup;
        [SerializeField] private RawImage icon;
        [SerializeField] private Text titleText;
        [SerializeField] private Text childText;
        [SerializeField] private Text timeText;
        [SerializeField] private RectTransform timerFill;
        [SerializeField] private Image timerFillImage;
        [SerializeField] private Image background;
        [SerializeField] private RectTransform stamp;
        [SerializeField] private Text stampText;

        private static readonly Color Calm = new Color(0.42f, 0.85f, 0.45f);
        private static readonly Color Warning = new Color(1f, 0.78f, 0.25f);
        private static readonly Color Danger = new Color(0.95f, 0.3f, 0.3f);
        private static readonly Color PaperColor = new Color(1f, 0.95f, 0.8f);
        private static readonly Color DoneColor = new Color(0.62f, 0.93f, 0.55f);
        private static readonly Color FailColor = new Color(1f, 0.55f, 0.5f);

        private Order order;
        private int lastShownSecond = -1;
        private float age;
        private float seed;
        private bool finishing;

        public Order Order => order;

        public void Bind(Order newOrder, Texture iconTexture)
        {
            order = newOrder;
            lastShownSecond = -1;
            age = 0f;
            finishing = false;
            seed = UnityEngine.Random.value * 10f;
            if (titleText != null) titleText.text = newOrder.Definition.RequestedItem.DisplayName;
            if (childText != null) childText.text = UiText.ForChild(newOrder.ChildName);
            if (icon != null)
            {
                icon.texture = iconTexture;
                icon.enabled = iconTexture != null;
            }
            if (stamp != null) stamp.gameObject.SetActive(false);
            if (background != null) background.color = PaperColor;
            if (visualGroup != null) visualGroup.alpha = 1f;
            ApplyPose(0f, 0f);
            Refresh();
        }

        private void Update()
        {
            if (order == null || visual == null || finishing) return;
            age += Time.unscaledDeltaTime;
            Refresh();

            // Entrata: scende dall'alto con rimbalzo elastico.
            float enter = UiAnim.ElasticOut(Mathf.Clamp01(age / 0.7f));
            float yOffset = (1f - enter) * 140f;
            float scale = Mathf.LerpUnclamped(0.5f, 1f, enter);

            // Dondolio leggero e, quando il tempo sta finendo, tremolio nervoso.
            float n = order.Normalized;
            float wobble = Mathf.Sin(Time.unscaledTime * 2f + seed) * 1.4f;
            float shake = 0f;
            if (n <= 0.25f)
            {
                shake = Mathf.Sin(Time.unscaledTime * 38f) * 3.5f;
                wobble += Mathf.Sin(Time.unscaledTime * 24f) * 1.8f;
                if (background != null)
                {
                    float flash = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 9f);
                    background.color = Color.Lerp(PaperColor, FailColor, flash * 0.55f);
                }
            }
            ApplyPose(yOffset, scale, wobble, shake);
        }

        private void ApplyPose(float yOffset, float scale, float rotation = 0f, float xOffset = 0f)
        {
            if (visual == null) return;
            visual.anchoredPosition = new Vector2(xOffset, yOffset);
            visual.localScale = Vector3.one * Mathf.Max(0.01f, scale <= 0f ? 1f : scale);
            visual.localRotation = Quaternion.Euler(0f, 0f, rotation);
        }

        private void Refresh()
        {
            float n = order.Normalized;
            if (timerFill != null) timerFill.anchorMax = new Vector2(Mathf.Max(0.03f, n), 1f);

            Color c = n > 0.5f ? Calm : (n > 0.25f ? Warning : Danger);
            if (timerFillImage != null) timerFillImage.color = c;

            int seconds = Mathf.CeilToInt(order.TimeLeft);
            if (seconds != lastShownSecond)
            {
                lastShownSecond = seconds;
                if (timeText != null)
                {
                    timeText.text = UiText.FormatTime(seconds);
                    timeText.color = n > 0.25f ? Color.white : new Color(1f, 0.8f, 0.8f);
                }
            }
        }

        /// <summary>Consegna riuscita: timbro "OK!", salto di gioia, poi la carta vola via.</summary>
        public void PlayComplete(Action onFinished)
        {
            if (finishing) return;
            finishing = true;
            StartCoroutine(FinishRoutine(true, onFinished));
        }

        /// <summary>Ordine scaduto: la carta diventa rossa, trema e cade.</summary>
        public void PlayFail(Action onFinished)
        {
            if (finishing) return;
            finishing = true;
            StartCoroutine(FinishRoutine(false, onFinished));
        }

        private IEnumerator FinishRoutine(bool success, Action onFinished)
        {
            if (background != null) background.color = success ? DoneColor : FailColor;
            if (stamp != null)
            {
                stamp.gameObject.SetActive(true);
                if (stampText != null) stampText.text = success ? "OK!" : "ZZZ";
            }

            // fase 1: la carta salta (successo) o trema (fallimento)
            float t = 0f;
            float duration = success ? 0.75f : 0.55f;
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(t / duration);
                float stampScale = Mathf.LerpUnclamped(0.2f, 1f, UiAnim.ElasticOut(Mathf.Clamp01(t / 0.45f)));
                if (stamp != null) stamp.localScale = Vector3.one * stampScale;
                if (success)
                {
                    float bounce = Mathf.Sin(k * Mathf.PI * 2f) * (1f - k) * 22f;
                    ApplyPose(Mathf.Max(0f, bounce), 1f + Mathf.Sin(k * Mathf.PI) * 0.18f, Mathf.Sin(k * Mathf.PI * 4f) * (1f - k) * 6f);
                }
                else
                {
                    ApplyPose(0f, 1f, Mathf.Sin(t * 50f) * 5f * (1f - k), Mathf.Sin(t * 60f) * 6f * (1f - k));
                }
                yield return null;
            }

            // fase 2: vola in alto (successo) o scivola in basso (fallimento) e svanisce
            t = 0f;
            const float outDuration = 0.4f;
            while (t < outDuration)
            {
                t += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(t / outDuration);
                float y = success ? UiAnim.OutCubic(k) * 150f : -UiAnim.OutCubic(k) * 110f;
                ApplyPose(y, 1f - k * 0.35f, success ? k * 8f : -k * 10f);
                if (visualGroup != null) visualGroup.alpha = 1f - k;
                yield return null;
            }

            if (onFinished != null) onFinished();
            Destroy(gameObject);
        }
    }
}
