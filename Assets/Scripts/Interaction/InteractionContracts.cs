using Elfshift.Core;
using Elfshift.Production;

namespace Elfshift.Interaction
{
    /// <summary>Qualcosa da cui si può prendere un oggetto (scaffale, banco, oggetto a terra).</summary>
    public interface IItemSource
    {
        bool CanGive { get; }
        /// <summary>Testo mostrato al giocatore (es. "Prendi Legno"). Deve essere una stringa già pronta.</summary>
        string PickupPrompt { get; }
        /// <summary>Consegna l'oggetto al giocatore. Chi chiama lo mette in mano.</summary>
        WorldItem Take();
    }

    /// <summary>Qualcosa dove si può posare/consegnare un oggetto (banco, slitta).</summary>
    public interface IItemReceiver
    {
        /// <summary>Dice se l'oggetto viene accettato; se no, "reason" spiega perché.</summary>
        bool Accepts(ItemDefinition item, out string reason);
        string PlacePrompt { get; }
        void Receive(WorldItem item);
    }

    /// <summary>Interazione da tenere premuta per un certo tempo (es. costruire un giocattolo).</summary>
    public interface IHoldInteractable
    {
        bool CanHold { get; }
        /// <summary>Secondi da tenere premuto.</summary>
        float HoldDuration { get; }
        string HoldPrompt { get; }
        /// <summary>Chiamata a ogni frame mentre si tiene premuto. 0 = inizio, 1 = fine.</summary>
        void HoldProgress(float normalized);
        /// <summary>Il giocatore ha lasciato il tasto o si è allontanato.</summary>
        void HoldCancelled();
        void HoldCompleted();
    }

    /// <summary>
    /// Qualcosa che si "usa" con un clic a mani vuote, senza prendere oggetti (es. la cassetta delle letterine).
    /// </summary>
    public interface IUsable
    {
        bool CanUse { get; }
        /// <summary>Testo mostrato al giocatore (es. "Ritira 2 letterine").</summary>
        string UsePrompt { get; }
        void Use();
    }
}
