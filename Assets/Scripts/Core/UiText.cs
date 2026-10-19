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
        public const string Intro = "Prendi 2 legni, posali sul banco e tieni premuto E!";

        public const string KeyPickup = "SPAZIO";
        public const string KeyInteract = "E";

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
