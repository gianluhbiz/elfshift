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
        public const string DeliverToSleigh = "Consegna alla slitta";

        public const string NotNeededHere = "Questo non serve qui";
        public const string BenchFull = "Il banco è pieno";
        public const string TakeFinishedFirst = "Prima prendi il giocattolo finito";
        public const string NoOrderForThis = "Nessun ordine aperto per questo oggetto";
        public const string OrderExpired = "Ordine scaduto!";
        public const string Intro = "Segui l'obiettivo in alto. Clic sinistro = prendi, E = costruisci, Maiusc = scatta, Spazio = salta";
        public const string NothingToTake = "Niente da prendere qui: avvicinati a uno scaffale e guardalo";
        public const string NothingToBuild = "Niente da costruire: posa 2 legni sul banco e guardalo";

        // Obiettivi del tutorial (mostrati in alto durante il primo ordine)
        public const string GoalTakeWood = "Vai allo scaffale del legno e prendi un legno (clic sinistro)";
        public const string GoalPlaceWood = "Porta il legno al banco di costruzione e posalo (clic sinistro)";
        public const string GoalSecondWood = "Ti serve un altro legno: torna allo scaffale";
        public const string GoalBuild = "Guarda il banco e tieni premuto E (o tasto destro) per costruire";
        public const string GoalTakeToy = "Prendi il trenino dal banco (clic sinistro)";
        public const string GoalDeliver = "Porta il trenino alla slitta, fuori dalla porta a destra";
        public const string GoalWaitOrder = "Aspetta la prossima letterina...";

        public const string GoalTitle = "Obiettivo";
        public const string StampDone = "Consegnato!";
        public const string StampFail = "Scaduto";
        public const string CarriedTitle = "In mano";

        public const string KeyPickup = "CLIC SX";
        public const string KeyInteract = "E";

        public static string GetCloser(string name) { return "Avvicinati: " + name; }
        public static string Take(string itemName) { return "Prendi " + itemName; }
        public static string HoldToBuild(string itemName) { return "Tieni premuto: costruisci " + itemName; }
        public static string ForChild(string childName) { return "Per " + childName; }

        public static string Delivered(string itemName, int points, int cookies)
        {
            return itemName + " consegnato!  +" + points + " punti  +" + cookies + " biscotti";
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
