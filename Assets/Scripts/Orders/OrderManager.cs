using System;
using System.Collections.Generic;
using Elfshift.Core;
using UnityEngine;

namespace Elfshift.Orders
{
    /// <summary>
    /// Crea gli ordini, ne fa scorrere il tempo e li chiude (consegnati o scaduti).
    /// Non sa nulla di grafica: l'interfaccia si iscrive agli eventi.
    /// </summary>
    public sealed class OrderManager : MonoBehaviour
    {
        [SerializeField] private GameSession session;
        [Tooltip("Ordini possibili. Vengono proposti a rotazione.")]
        [SerializeField] private OrderDefinition[] pool = new OrderDefinition[0];
        [Min(1)]
        [Tooltip("Quanti ordini possono essere aperti contemporaneamente.")]
        [SerializeField] private int maxActive = 1;
        [Min(0f)] [SerializeField] private float firstOrderDelay = 2f;
        [Min(0f)] [SerializeField] private float nextOrderDelay = 5f;
        [SerializeField] private string[] childNames = { "Marta", "Luca", "Sofia", "Tommaso", "Giulia", "Pietro" };

        private readonly List<Order> active = new List<Order>(8);
        private int nextId = 1;
        private int poolIndex;
        private int nameIndex;
        private float spawnTimer;

        /// <summary>Se falso non vengono creati nuovi ordini (utile per pause, fine giornata...).</summary>
        public bool AutoSpawn { get; set; } = true;

        public IReadOnlyList<Order> Active => active;

        public event Action<Order> OrderAdded;
        public event Action<Order, OrderResult> OrderCompleted;
        public event Action<Order> OrderFailed;

        private void Awake()
        {
            if (session == null) Debug.LogError("[Elfshift] OrderManager: manca il riferimento a GameSession.", this);
            if (pool == null || pool.Length == 0) Debug.LogError("[Elfshift] OrderManager: nessun ordine nel pool.", this);
            spawnTimer = firstOrderDelay;
        }

        private void Update()
        {
            float dt = Time.deltaTime;

            // Il tempo scorre dall'ultimo ordine al primo, così si può rimuovere mentre si scorre.
            for (int i = active.Count - 1; i >= 0; i--)
            {
                Order order = active[i];
                order.TimeLeft -= dt;
                if (order.TimeLeft <= 0f)
                {
                    order.TimeLeft = 0f;
                    active.RemoveAt(i);
                    if (session != null) session.RegisterFailure(order.Definition.FailPenaltyPoints);
                    OrderFailed?.Invoke(order);
                }
            }

            if (AutoSpawn && active.Count < maxActive && pool != null && pool.Length > 0)
            {
                spawnTimer -= dt;
                if (spawnTimer <= 0f)
                {
                    SpawnNext();
                    spawnTimer = nextOrderDelay;
                }
            }
        }

        private void SpawnNext()
        {
            OrderDefinition definition = pool[poolIndex % pool.Length];
            poolIndex++;
            if (definition == null || definition.RequestedItem == null)
            {
                Debug.LogError("[Elfshift] Ordine nel pool senza oggetto richiesto.", this);
                return;
            }

            string child = (childNames != null && childNames.Length > 0) ? childNames[nameIndex % childNames.Length] : "un bambino";
            nameIndex++;

            Order order = new Order(nextId++, definition, child);
            active.Add(order);
            OrderAdded?.Invoke(order);
        }

        /// <summary>Trova l'ordine aperto (il più urgente) che chiede questo oggetto, oppure null.</summary>
        public Order FindOpenOrderFor(ItemDefinition item)
        {
            if (item == null) return null;
            Order best = null;
            for (int i = 0; i < active.Count; i++)
            {
                Order order = active[i];
                if (!ReferenceEquals(order.Definition.RequestedItem, item)) continue;
                if (best == null || order.TimeLeft < best.TimeLeft) best = order;
            }
            return best;
        }

        /// <summary>Chiude un ordine come consegnato e assegna punti e biscotti.</summary>
        public OrderResult Complete(Order order)
        {
            if (order == null || !active.Remove(order)) return new OrderResult(0, 0, 0);

            int bonus = Mathf.RoundToInt(order.Definition.SpeedBonusPoints * order.Normalized);
            int points = order.Definition.RewardPoints + bonus;
            int cookies = order.Definition.RewardCookies;
            if (session != null) session.RegisterDelivery(points, cookies);

            OrderResult result = new OrderResult(points, cookies, bonus);
            OrderCompleted?.Invoke(order, result);
            return result;
        }
    }
}
