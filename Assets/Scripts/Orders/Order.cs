using UnityEngine;

namespace Elfshift.Orders
{
    /// <summary>Un ordine aperto (una letterina in corso), con il suo timer.</summary>
    public sealed class Order
    {
        public int Id { get; }
        public OrderDefinition Definition { get; }
        public string ChildName { get; }
        public float TimeLimit { get; }
        public float TimeLeft { get; internal set; }

        /// <summary>1 = tempo pieno, 0 = scaduto.</summary>
        public float Normalized => TimeLimit <= 0f ? 0f : Mathf.Clamp01(TimeLeft / TimeLimit);

        public Order(int id, OrderDefinition definition, string childName)
        {
            Id = id;
            Definition = definition;
            ChildName = childName;
            TimeLimit = definition.TimeLimitSeconds;
            TimeLeft = TimeLimit;
        }
    }

    /// <summary>Risultato di una consegna riuscita.</summary>
    public readonly struct OrderResult
    {
        public readonly int Points;
        public readonly int Cookies;
        public readonly int SpeedBonus;

        public OrderResult(int points, int cookies, int speedBonus)
        {
            Points = points;
            Cookies = cookies;
            SpeedBonus = speedBonus;
        }
    }
}
