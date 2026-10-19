using System;
using UnityEngine;

namespace Elfshift.Core
{
    /// <summary>
    /// Punteggio della partita: punti, biscotti, ordini riusciti e falliti e la "serie" di consegne
    /// consecutive senza ordini scaduti (più è lunga, più punti vale ogni consegna).
    /// </summary>
    public sealed class GameSession : MonoBehaviour
    {
        [Tooltip("Bonus per ogni consegna di fila (0,25 = +25% a consegna).")]
        [Range(0f, 1f)] [SerializeField] private float streakStep = 0.25f;
        [Tooltip("Oltre questa serie il bonus non cresce più.")]
        [Min(1)] [SerializeField] private int streakCap = 4;

        public int Points { get; private set; }
        public int Cookies { get; private set; }
        public int OrdersCompleted { get; private set; }
        public int OrdersFailed { get; private set; }

        /// <summary>Consegne di fila senza ordini scaduti.</summary>
        public int Streak { get; private set; }
        public int BestStreak { get; private set; }

        /// <summary>Moltiplicatore dei punti per la PROSSIMA consegna (1 = nessun bonus).</summary>
        public float StreakMultiplier => 1f + streakStep * Mathf.Min(Streak, streakCap);

        public event Action Changed;
        /// <summary>La serie è cambiata (nuovo valore). 0 = serie interrotta.</summary>
        public event Action<int> StreakChanged;

        public void RegisterDelivery(int points, int cookies)
        {
            Points += Mathf.Max(0, points);
            Cookies += Mathf.Max(0, cookies);
            OrdersCompleted++;
            Streak++;
            if (Streak > BestStreak) BestStreak = Streak;
            Changed?.Invoke();
            StreakChanged?.Invoke(Streak);
        }

        public void RegisterFailure(int penaltyPoints)
        {
            Points = Mathf.Max(0, Points - Mathf.Max(0, penaltyPoints));
            OrdersFailed++;
            bool hadStreak = Streak > 0;
            Streak = 0;
            Changed?.Invoke();
            if (hadStreak) StreakChanged?.Invoke(0);
        }

        /// <summary>Spende biscotti (per i miglioramenti delle fasi successive). Falso se non bastano.</summary>
        public bool TrySpendCookies(int amount)
        {
            if (amount < 0 || Cookies < amount) return false;
            Cookies -= amount;
            Changed?.Invoke();
            return true;
        }

        public void ResetSession()
        {
            Points = 0;
            Cookies = 0;
            OrdersCompleted = 0;
            OrdersFailed = 0;
            Streak = 0;
            BestStreak = 0;
            Changed?.Invoke();
            StreakChanged?.Invoke(0);
        }
    }
}
