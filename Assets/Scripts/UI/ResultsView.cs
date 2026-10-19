using Elfshift.Core;
using Elfshift.Progression;
using UnityEngine;
using UnityEngine.UI;

namespace Elfshift.UI
{
    /// <summary>Riepilogo di fine giornata: punti, consegne, stelle che si accendono una alla volta.</summary>
    public sealed class ResultsView : MonoBehaviour
    {
        [SerializeField] private DayCycle day;
        [SerializeField] private GameSession session;
        [SerializeField] private GameObject root;
        [SerializeField] private CanvasGroup group;
        [SerializeField] private Text pointsText;
        [SerializeField] private Text detailsText;
        [SerializeField] private Image[] stars = new Image[0];
        [SerializeField] private Color starOn = new Color(1f, 0.8f, 0.3f);
        [SerializeField] private Color starOff = new Color(0.85f, 0.8f, 0.74f);

        private float elapsed = -1f;
        private int earned;

        private void Awake()
        {
            if (root != null) root.SetActive(false);
        }

        private void OnEnable()
        {
            if (day != null) day.Ended += Show;
        }

        private void OnDisable()
        {
            if (day != null) day.Ended -= Show;
        }

        private void Show(int starCount)
        {
            earned = starCount;
            elapsed = 0f;
            if (root != null) root.SetActive(true);
            if (pointsText != null) pointsText.text = "0";
            if (detailsText != null && session != null)
                detailsText.text = UiText.ResultsLine(session.OrdersCompleted, session.OrdersFailed, session.BestStreak);
            for (int i = 0; i < stars.Length; i++)
            {
                if (stars[i] == null) continue;
                stars[i].color = starOff;
                stars[i].transform.localScale = Vector3.one * 0.8f;
            }
        }

        private void Update()
        {
            if (elapsed < 0f) return;
            // Il gioco è in pausa (timeScale = 0): uso il tempo reale.
            elapsed += Time.unscaledDeltaTime;
            if (group != null) group.alpha = Mathf.Clamp01(elapsed / 0.3f);
            if (root != null) root.transform.localScale = Vector3.one * Mathf.LerpUnclamped(0.9f, 1f, UiAnim.BackOut(Mathf.Clamp01(elapsed / 0.4f), 1.6f));

            // I punti salgono come un contatore, poi le stelle si accendono una alla volta.
            int points = session != null ? session.Points : 0;
            float count = Mathf.Clamp01((elapsed - 0.3f) / 1.2f);
            if (pointsText != null) pointsText.text = Mathf.RoundToInt(points * UiAnim.OutCubic(count)).ToString();
            for (int i = 0; i < stars.Length; i++)
            {
                if (stars[i] == null || i >= earned) continue;
                float k = Mathf.Clamp01((elapsed - 1.6f - i * 0.35f) / 0.35f);
                if (k <= 0f) continue;
                stars[i].color = Color.Lerp(starOff, starOn, k);
                stars[i].transform.localScale = Vector3.one * Mathf.LerpUnclamped(0.8f, 1f, UiAnim.BackOut(k, 3f));
            }
        }
    }
}
