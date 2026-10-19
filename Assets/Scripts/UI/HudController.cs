using Elfshift.Core;
using Elfshift.Orders;
using Elfshift.Player;
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

        private float pointsPunch;
        private float cookiesPunch;
        private int shownPoints = -1;
        private int shownCookies = -1;

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
            interactor.MessageRaised += OnMessage;
            RefreshStats();
        }

        private void Start()
        {
            // Piccolo suggerimento iniziale per chi gioca la prima volta.
            if (toast != null) toast.Show(UiText.Intro, 7f);
        }

        private void OnDisable()
        {
            if (session != null) session.Changed -= RefreshStats;
            if (orders != null)
            {
                orders.OrderCompleted -= OnOrderCompleted;
                orders.OrderFailed -= OnOrderFailed;
            }
            if (interactor != null) interactor.MessageRaised -= OnMessage;
        }

        private void Update()
        {
            AnimatePunch(pointsText, ref pointsPunch);
            AnimatePunch(cookiesText, ref cookiesPunch);
        }

        private void RefreshStats()
        {
            if (session == null) return;
            if (session.Points != shownPoints)
            {
                if (shownPoints >= 0) pointsPunch = 1f;
                shownPoints = session.Points;
                if (pointsText != null) pointsText.text = shownPoints.ToString();
            }
            if (session.Cookies != shownCookies)
            {
                if (shownCookies >= 0) cookiesPunch = 1f;
                shownCookies = session.Cookies;
                if (cookiesText != null) cookiesText.text = shownCookies.ToString();
            }
        }

        private void OnOrderCompleted(Order order, OrderResult result)
        {
            if (toast != null)
                toast.Show(UiText.Delivered(order.Definition.RequestedItem.DisplayName, result.Points, result.Cookies));
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
