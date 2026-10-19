namespace Elfshift.Core
{
    /// <summary>
    /// Stato di un oggetto lungo la catena di produzione.
    /// Ogni <see cref="ItemDefinition"/> appartiene a uno di questi stati.
    /// </summary>
    public enum ItemState
    {
        /// <summary>Materia prima (legno, stoffa, metallo).</summary>
        RawMaterial = 0,
        /// <summary>Giocattolo costruito ma non ancora verniciato.</summary>
        Toy = 1,
        /// <summary>Giocattolo verniciato (Fase 2).</summary>
        PaintedToy = 2,
        /// <summary>Giocattolo impacchettato (Fase 2).</summary>
        WrappedGift = 3,
        /// <summary>Consegna completata (stato finale, l'oggetto non esiste più nel mondo).</summary>
        Delivered = 4
    }

    /// <summary>Tipo di postazione di lavoro. Le ricette sono legate a un tipo.</summary>
    public enum StationType
    {
        Construction = 0,
        Paint = 1,
        Wrap = 2
    }
}
