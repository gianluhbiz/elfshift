using System;
using UnityEngine;

namespace Elfshift.Core
{
    /// <summary>
    /// Stato della partita in corso: punti, biscotti, ordini completati/falliti.
    /// Viene collegato nella scena (nessun singleton). Nelle fasi successive
    /// questo stato verrà letto/scritto dal sistema di salvataggio.
    /// </summary>
    public sealed class GameSession : MonoBehaviour
    {
        public int Points { get; private set; }
        public int Cookies { get; private set; }
        public int OrdersCompleted { get; private set; }
        public int OrdersFailed { get; private set; }

        /// <summary>Scatta ogni volta che uno dei valori cambia.</summary>
        public event Action Changed;

        public void RegisterDelivery(int points, int cookies)
        {
            Points += Mathf.Max(0, points);
            Cookies += Mathf.Max(0, cookies);
            OrdersCompleted++;
            Changed?.Invoke();
        }

        public void RegisterFailure(int penaltyPoints)
        {
            Points = Mathf.Max(0, Points - Mathf.Max(0, penaltyPoints));
            OrdersFailed++;
            Changed?.Invoke();
        }

        public void ResetSession()
        {
            Points = 0;
            Cookies = 0;
            OrdersCompleted = 0;
            OrdersFailed = 0;
            Changed?.Invoke();
        }
    }
}
