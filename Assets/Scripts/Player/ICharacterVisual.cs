namespace Elfshift.Player
{
    /// <summary>
    /// Ciò che il gioco chiede al "corpo" dell'elfo. Il modello provvisorio usa
    /// <see cref="ElfProceduralAnimator"/>; quando avrai un personaggio con Animator basterà
    /// scrivere un altro componente che implementa questa interfaccia (es. imposta parametri
    /// dell'Animator) e metterlo sul modello: il gameplay non cambia.
    /// </summary>
    public interface ICharacterVisual
    {
        /// <summary>0 = fermo, 1 = velocità massima.</summary>
        void SetMoveAmount(float normalized);
        void SetCarrying(bool carrying);
        void SetWorking(bool working);
        /// <summary>Piccola animazione quando si raccoglie o si posa qualcosa.</summary>
        void PlayPickup();
    }
}
