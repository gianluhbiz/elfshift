using Elfshift.Core;
using Elfshift.Orders;
using Elfshift.Player;
using Elfshift.Production;
using Elfshift.Progression;
using UnityEngine;
using UnityEngine.UI;

namespace Elfshift.UI
{
    /// <summary>
    /// Collega gli eventi del gioco all'interfaccia: punteggio, biscotti e messaggi.
    /// (Le carte degli ordini, i suggerimenti e l'oggetto in mano hanno i loro componenti.)
    /// </summary>
    public sealed class HudController : MonoBehaviour
    {
        [SerializeField] private GameSession session;
        [SerializeField] private OrderManager orders;
        [SerializeField] private PlayerInteractor interactor;
        [SerializeField] private ToastView toast;
        [SerializeField] private Text pointsText;
        [SerializeField] private Text cookiesText;
        [Header("Animazioni (facoltative)")]
        [SerializeField] private FloatingTextSpawner floatingTexts;
        [SerializeField] private UiPop pointsIconPop;
        [SerializeField] private UiPop cookiesIconPop;
        [Header("Avvisi di gioco (facoltativi)")]
        [SerializeField] private Fireplace fireplace;
        [SerializeField] private DayCycle day;

        private float pointsPunch;
        private float cookiesPunch;
        private int shownPoints = -1;
        private int shownCookies = -1;
        private bool warnedFireLow;
        private bool warnedFireOut;
        private bool warnedLastMinute;

        private void OnEnable()
        {
            if (session == null || orders == null || interactor == null)
            {
                Debug.LogError("[Elfshift] HudController: riferimenti mancanti.", this);
                return;
            }
            session.Changed += RefreshStats;
            orders.OrderCompleted += OnOrderCompleted;
            orders.OrderFailed += OnOrderFailed;
            orders.MailArrived += OnMailArrived;
            session.StreakChanged += OnStreakChanged;
            interactor.MessageRaised += OnMessage;
            if (day != null) day.Closing += OnDayClosing;
            RefreshStats();
        }

        private void Start()
        {
            // Piccolo suggerimento iniziale per chi gioca la prima volta.
            if (toast != null) toast.Show(UiText.Intro, 6f);
        }

        private void OnDisable()
        {
            if (session != null) session.Changed -= RefreshStats;
            if (orders != null)
            {
                orders.OrderCompleted -= OnOrderCompleted;
                orders.OrderFailed -= OnOrderFailed;
                orders.MailArrived -= OnMailArrived;
            }
            if (session != null) session.StreakChanged -= OnStreakChanged;
            if (day != null) day.Closing -= OnDayClosing;
            if (interactor != null) interactor.MessageRaised -= OnMessage;
        }

        private void Update()
        {
            AnimatePunch(pointsText, ref pointsPunch);
            AnimatePunch(cookiesText, ref cookiesPunch);
            CheckWarnings();
        }

        /// <summary>Avvisi una sola volta per "episodio": fuoco basso, fuoco spento, ultimo minuto.</summary>
        private void CheckWarnings()
        {
            if (toast == null) return;
            if (fireplace != null)
            {
                float w = fireplace.Warmth01;
                if (w < 0.35f && !warnedFireLow) { warnedFireLow = true; toast.Show(UiText.FireLow, 3.5f); }
                if (w > 0.5f) warnedFireLow = false;
                if (fireplace.IsCold && !warnedFireOut) { warnedFireOut = true; toast.Show(UiText.FireOut, 3.5f); }
                if (!fireplace.IsCold) warnedFireOut = false;
            }
            if (day != null && !warnedLastMinute && !day.IsClosing && !day.IsOver && day.TimeLeft <= 60f)
            {
                warnedLastMinute = true;
                toast.Show(UiText.LastMinute, 3f);
            }
        }

        private void OnMailArrived(Order order)
        {
            if (toast != null) toast.Show(UiText.NewLetter, 2.5f);
        }

        private void OnStreakChanged(int streak)
        {
            // La serie si vede nella pillola "Serie xN" e nel messaggio di consegna: qui niente avvisi doppi.
        }

        private void OnDayClosing()
        {
            if (toast != null) toast.Show(UiText.DayClosing, 4f);
        }

        private void RefreshStats()
        {
            if (session == null) return;
            if (session.Points != shownPoints)
            {
                if (shownPoints >= 0)
                {
                    pointsPunch = 1f;
                    if (pointsIconPop != null) pointsIconPop.Punch(0.4f);
                    int delta = session.Points - shownPoints;
                    if (floatingTexts != null && pointsText != null)
                        floatingTexts.Spawn((delta > 0 ? "+" : "") + delta, pointsText.transform, delta > 0 ? new Color(1f, 0.85f, 0.3f) : new Color(1f, 0.5f, 0.45f), new Vector2(40f, -10f));
                }
                shownPoints = session.Points;
                if (pointsText != null) pointsText.text = shownPoints.ToString();
            }
            if (session.Cookies != shownCookies)
            {
                if (shownCookies >= 0)
                {
                    cookiesPunch = 1f;
                    if (cookiesIconPop != null) cookiesIconPop.Punch(0.4f);
                    int delta = session.Cookies - shownCookies;
                    if (floatingTexts != null && cookiesText != null && delta > 0)
                        floatingTexts.Spawn("+" + delta, cookiesText.transform, new Color(1f, 0.78f, 0.45f), new Vector2(40f, -10f));
                }
                shownCookies = session.Cookies;
                if (cookiesText != null) cookiesText.text = shownCookies.ToString();
            }
        }

        private void OnOrderCompleted(Order order, OrderResult result)
        {
            if (toast != null)
            {
                string text = UiText.Delivered(order.Definition.LetterItem.DisplayName, result.Points, result.Cookies);
                if (result.Multiplier > 1.001f) text += "   (" + UiText.StreakTitle + " x" + result.Multiplier.ToString("0.##") + ")";
                toast.Show(text);
            }
        }

        private void OnOrderFailed(Order order)
        {
            if (toast != null) toast.Show(UiText.OrderExpired);
        }

        private void OnMessage(string message)
        {
            if (toast != null) toast.Show(message);
        }

        private static void AnimatePunch(Text text, ref float punch)
        {
            if (text == null) return;
            if (punch > 0f) punch = Mathf.MoveTowards(punch, 0f, Time.unscaledDeltaTime * 3.5f);
            text.transform.localScale = Vector3.one * (1f + punch * 0.35f);
        }
    }
}
