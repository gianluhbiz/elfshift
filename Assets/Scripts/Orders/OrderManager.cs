using System;
using System.Collections.Generic;
using Elfshift.Core;
using UnityEngine;

namespace Elfshift.Orders
{
    /// <summary>
    /// Gestisce le letterine dei bambini (gli ordini).
    /// Le nuove letterine arrivano nella CASSETTA DELLA POSTA (in attesa): il tempo parte solo quando
    /// il giocatore le ritira. Gli ordini aperti hanno un timer: consegnati in tempo danno punti
    /// (moltiplicati dalla serie di consegne), scaduti tolgono punti e interrompono la serie.
    /// </summary>
    public sealed class OrderManager : MonoBehaviour
    {
        [SerializeField] private GameSession session;
        [Tooltip("Ordini possibili. Vengono scelti a caso, evitando di ripetere sempre lo stesso.")]
        [SerializeField] private OrderDefinition[] pool = new OrderDefinition[0];
        [Min(1)]
        [Tooltip("Quanti ordini possono essere aperti contemporaneamente.")]
        [SerializeField] private int maxActive = 3;
        [Min(0)]
        [Tooltip("Quante letterine possono aspettare nella cassetta.")]
        [SerializeField] private int maxPending = 3;
        [Tooltip("Se vero le letterine arrivano nella cassetta e vanno ritirate; se falso si aprono da sole.")]
        [SerializeField] private bool useMailbox = true;
        [Min(0f)] [SerializeField] private float firstOrderDelay = 2f;
        [Min(0f)] [SerializeField] private float nextOrderDelay = 20f;
        [SerializeField] private string[] childNames = { "Marta", "Luca", "Sofia", "Tommaso", "Giulia", "Pietro", "Anna", "Leo", "Bianca", "Matteo", "Chiara", "Davide" };

        private readonly List<Order> active = new List<Order>(8);
        private readonly List<Order> pending = new List<Order>(8);
        private int nextId = 1;
        private int lastPoolIndex = -1;
        private int nameIndex;
        private float spawnTimer;

        /// <summary>Se falso non arrivano nuove letterine (es. a fine giornata).</summary>
        public bool AutoSpawn { get; set; } = true;

        public IReadOnlyList<Order> Active => active;
        public IReadOnlyList<Order> Pending => pending;
        public int PendingCount => pending.Count;
        public bool UsesMailbox => useMailbox;

        /// <summary>Un ordine è stato aperto (il suo timer parte ora).</summary>
        public event Action<Order> OrderAdded;
        /// <summary>Una nuova letterina è arrivata nella cassetta.</summary>
        public event Action<Order> MailArrived;
        /// <summary>È cambiato il numero di letterine in attesa nella cassetta.</summary>
        public event Action<int> PendingChanged;
        public event Action<Order, OrderResult> OrderCompleted;
        public event Action<Order> OrderFailed;

        private void Awake()
        {
            if (session == null) Debug.LogError("[Elfshift] OrderManager: manca il riferimento a GameSession.", this);
            if (pool == null || pool.Length == 0) Debug.LogError("[Elfshift] OrderManager: nessun ordine nel pool.", this);
            spawnTimer = firstOrderDelay;
            nameIndex = UnityEngine.Random.Range(0, Mathf.Max(1, childNames != null ? childNames.Length : 1));
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

            bool room = useMailbox ? pending.Count < maxPending && active.Count + pending.Count < maxActive + maxPending : active.Count < maxActive;
            if (AutoSpawn && room && pool != null && pool.Length > 0)
            {
                // Se non c'è niente da fare la prossima letterina arriva prima.
                float speed = active.Count + pending.Count == 0 ? 3f : 1f;
                spawnTimer -= dt * speed;
                if (spawnTimer <= 0f)
                {
                    SpawnNext();
                    spawnTimer = nextOrderDelay;
                }
            }
        }

        private void SpawnNext()
        {
            OrderDefinition definition = PickDefinition();
            if (definition == null || definition.RequestedItem == null)
            {
                Debug.LogError("[Elfshift] Ordine nel pool senza oggetto richiesto.", this);
                return;
            }

            string child = (childNames != null && childNames.Length > 0) ? childNames[nameIndex % childNames.Length] : "un bambino";
            nameIndex++;

            Order order = new Order(nextId++, definition, child);
            if (useMailbox)
            {
                pending.Add(order);
                MailArrived?.Invoke(order);
                PendingChanged?.Invoke(pending.Count);
            }
            else
            {
                Open(order);
            }
        }

        /// <summary>Sceglie un ordine a caso, evitando (se possibile) quello appena uscito.</summary>
        private OrderDefinition PickDefinition()
        {
            if (pool.Length == 1) return pool[0];
            int index = UnityEngine.Random.Range(0, pool.Length);
            if (index == lastPoolIndex) index = (index + 1) % pool.Length;
            lastPoolIndex = index;
            return pool[index];
        }

        private void Open(Order order)
        {
            active.Add(order);
            OrderAdded?.Invoke(order);
        }

        /// <summary>Quante letterine si possono aprire adesso (posti liberi tra gli ordini aperti).</summary>
        public int CollectableCount => Mathf.Min(pending.Count, Mathf.Max(0, maxActive - active.Count));

        /// <summary>Ritira le letterine dalla cassetta e le apre (fino al massimo di ordini aperti). Restituisce quante.</summary>
        public int CollectMail()
        {
            int count = CollectableCount;
            for (int i = 0; i < count; i++)
            {
                Order order = pending[0];
                pending.RemoveAt(0);
                Open(order);
            }
            if (count > 0) PendingChanged?.Invoke(pending.Count);
            return count;
        }

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

        /// <summary>L'ordine aperto più urgente (meno tempo rimasto), o null.</summary>
        public Order MostUrgent()
        {
            Order best = null;
            for (int i = 0; i < active.Count; i++)
                if (best == null || active[i].TimeLeft < best.TimeLeft) best = active[i];
            return best;
        }

        public OrderResult Complete(Order order)
        {
            if (order == null || !active.Remove(order)) return new OrderResult(0, 0, 0, 1f);

            int bonus = Mathf.RoundToInt(order.Definition.SpeedBonusPoints * order.Normalized);
            float multiplier = session != null ? session.StreakMultiplier : 1f;
            int points = Mathf.RoundToInt((order.Definition.RewardPoints + bonus) * multiplier);
            int cookies = order.Definition.RewardCookies;
            if (session != null) session.RegisterDelivery(points, cookies);

            OrderResult result = new OrderResult(points, cookies, bonus, multiplier);
            OrderCompleted?.Invoke(order, result);
            return result;
        }
    }
}
