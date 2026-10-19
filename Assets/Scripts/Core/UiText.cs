namespace Elfshift.Core
{
    /// <summary>
    /// Tutti i testi visibili al giocatore in un unico posto (italiano).
    /// Quando aggiungeremo la localizzazione basterà sostituire questa classe,
    /// senza cercare stringhe sparse nel codice.
    /// </summary>
    public static class UiText
    {
        public const string Drop = "Posa a terra";
        public const string PlaceOnBench = "Metti sul banco";
        public const string DeliverToSleigh = "Carica sulla slitta";
        public const string FeedFire = "Metti nel camino";

        public const string NotNeededHere = "Questo non serve qui";
        public const string BenchFull = "Il banco è pieno";
        public const string TakeFinishedFirst = "Prima prendi l'oggetto finito";
        public const string NoOrderForThis = "Nessuna letterina chiede questo: va prima incartato?";
        public const string OrderExpired = "Una letterina è scaduta: la serie riparte da zero";
        public const string Intro = "Segui l'obiettivo in alto. Clic sinistro = prendi/usa, E = lavora, Maiusc = scatta, Spazio = salta";
        public const string NothingToTake = "Niente da prendere qui: avvicinati a uno scaffale o a un banco e guardalo";
        public const string NothingToBuild = "Niente da lavorare: metti sul banco gli oggetti giusti e guardalo";
        public const string FireFull = "Il fuoco è già bello vivace";
        public const string OnlyWoodInFire = "Nel camino va solo la legna";
        public const string FireLow = "Il fuoco si sta spegnendo: senza calore si lavora piano!";
        public const string FireOut = "Brrr! Fa freddo: i banchi lavorano lentamente";
        public const string NewLetter = "È arrivata una letterina nella cassetta della posta!";
        public const string TooManyOrders = "Prima finisci una letterina aperta";
        public const string DayClosing = "Il laboratorio chiude! Consegna le ultime letterine aperte";
        public const string LastMinute = "Manca un minuto alla chiusura!";

        // Obiettivi (riga in alto): sempre il prossimo passo utile
        public const string GoalTitle = "Obiettivo";
        public const string GoalWaitOrder = "Aspetta la prossima letterina...";
        public const string GoalCollectMail = "Ritira le letterine dalla cassetta della posta";
        public const string GoalFeedFire = "Il fuoco si spegne: metti la legna nel camino";
        public const string GoalFetchFirewood = "Il fuoco si spegne: prendi della legna per il camino";
        public static string GoalTake(string item, string place) { return "Prendi " + item + " (" + place + ")"; }
        public static string GoalTakeFrom(string item, string place) { return "Prendi " + item + " dal banco: " + place; }
        public static string GoalBring(string item, string place) { return "Porta " + item + " a: " + place; }
        public static string GoalWork(string place, string product) { return "Tieni premuto E a: " + place + " (" + product + ")"; }
        public static string GoalDeliver(string item) { return "Carica " + item + " sulla slitta, nel cortile"; }
        public static string GoalClearBench(string place) { return "Libera il banco: riprendi gli oggetti da " + place; }
        public static string GoalHandsFull(string item) { return "Hai le mani piene: posa " + item + " dove serve (o a terra)"; }
        public static string GoalWaitWithItem(string item) { return "Hai in mano " + item + ": aspetta la prossima letterina"; }

        public const string StampDone = "Consegnato!";
        public const string StampFail = "Scaduto";
        public const string CarriedTitle = "In mano";
        public const string StreakTitle = "Serie";
        public const string DayTitle = "Giorno";
        public const string ResultsTitle = "Fine della giornata!";
        public const string ResultsHint = "Premi INVIO per iniziare un nuovo giorno";

        public const string KeyPickup = "CLIC SX";
        public const string KeyInteract = "E";

        public static string GetCloser(string name) { return "Avvicinati: " + name; }
        public static string Take(string itemName) { return "Prendi " + itemName; }
        public static string CollectLetters(int count) { return count == 1 ? "Ritira la letterina" : "Ritira " + count + " letterine"; }
        public static string ForChild(string childName) { return "Per " + childName; }

        /// <summary>Suggerimento per il lavoro a tasto premuto, con il verbo giusto per il banco.</summary>
        public static string HoldToCraft(StationType station, string itemName)
        {
            switch (station)
            {
                case StationType.Paint: return "Tieni premuto: dipingi " + itemName;
                case StationType.Wrap: return "Tieni premuto: incarta " + itemName;
                default: return "Tieni premuto: costruisci " + itemName;
            }
        }

        public static string HoldToBuild(string itemName) { return HoldToCraft(StationType.Construction, itemName); }

        public static string Delivered(string itemName, int points, int cookies)
        {
            return itemName + " consegnato!  +" + points + " punti  +" + cookies + " biscotti";
        }

        public static string StreakUp(int streak, float multiplier)
        {
            return "Serie di " + streak + " consegne! Punti x" + multiplier.ToString("0.##");
        }

        public static string ResultsLine(int delivered, int failed, int bestStreak)
        {
            return "Consegne: " + delivered + "    Scadute: " + failed + "    Serie migliore: " + bestStreak;
        }

        public static string FormatTime(int totalSeconds)
        {
            if (totalSeconds < 0) totalSeconds = 0;
            int m = totalSeconds / 60;
            int s = totalSeconds % 60;
            return m + ":" + (s < 10 ? "0" : "") + s;
        }
    }
}
