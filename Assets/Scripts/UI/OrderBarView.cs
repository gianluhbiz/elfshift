using System.Collections.Generic;
using Elfshift.Orders;
using UnityEngine;

namespace Elfshift.UI
{
    /// <summary>Mostra in alto una carta per ogni ordine aperto, con le animazioni di consegna e scadenza.</summary>
    public sealed class OrderBarView : MonoBehaviour
    {
        [SerializeField] private OrderManager orders;
        [SerializeField] private ItemIconService icons;
        [SerializeField] private OrderCardView cardPrefab;
        [SerializeField] private RectTransform container;

        private readonly List<OrderCardView> cards = new List<OrderCardView>(8);

        private void OnEnable()
        {
            if (orders == null || cardPrefab == null || container == null)
            {
                Debug.LogError("[Elfshift] OrderBarView: riferimenti mancanti.", this);
                return;
            }
            orders.OrderAdded += OnOrderAdded;
            orders.OrderCompleted += OnOrderCompleted;
            orders.OrderFailed += OnOrderFailed;

            // Ordini già aperti prima dell'attivazione della barra.
            for (int i = 0; i < orders.Active.Count; i++) AddCard(orders.Active[i]);
        }

        private void OnDisable()
        {
            if (orders == null) return;
            orders.OrderAdded -= OnOrderAdded;
            orders.OrderCompleted -= OnOrderCompleted;
            orders.OrderFailed -= OnOrderFailed;
        }

        private void OnOrderAdded(Order order) { AddCard(order); }

        private void OnOrderCompleted(Order order, OrderResult result)
        {
            OrderCardView card = Detach(order);
            if (card != null) card.PlayComplete(null);
        }

        private void OnOrderFailed(Order order)
        {
            OrderCardView card = Detach(order);
            if (card != null) card.PlayFail(null);
        }

        private void AddCard(Order order)
        {
            OrderCardView card = Instantiate(cardPrefab, container);
            Texture icon = icons != null ? icons.GetIcon(order.Definition.LetterItem) : null;
            card.Bind(order, icon);
            cards.Add(card);
        }

        /// <summary>Toglie la carta dall'elenco (continua ad animarsi da sola e poi si distrugge).</summary>
        private OrderCardView Detach(Order order)
        {
            for (int i = cards.Count - 1; i >= 0; i--)
            {
                if (cards[i] == null) { cards.RemoveAt(i); continue; }
                if (cards[i].Order != order) continue;
                OrderCardView card = cards[i];
                cards.RemoveAt(i);
                return card;
            }
            return null;
        }
    }
}
