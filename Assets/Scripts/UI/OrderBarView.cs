using System.Collections.Generic;
using Elfshift.Orders;
using UnityEngine;

namespace Elfshift.UI
{
    /// <summary>Mostra in alto una carta per ogni ordine aperto.</summary>
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
        private void OnOrderCompleted(Order order, OrderResult result) { RemoveCard(order); }
        private void OnOrderFailed(Order order) { RemoveCard(order); }

        private void AddCard(Order order)
        {
            OrderCardView card = Instantiate(cardPrefab, container);
            Texture icon = icons != null ? icons.GetIcon(order.Definition.RequestedItem) : null;
            card.Bind(order, icon);
            cards.Add(card);
        }

        private void RemoveCard(Order order)
        {
            for (int i = cards.Count - 1; i >= 0; i--)
            {
                if (cards[i] == null) { cards.RemoveAt(i); continue; }
                if (cards[i].Order != order) continue;
                Destroy(cards[i].gameObject);
                cards.RemoveAt(i);
            }
        }
    }
}
