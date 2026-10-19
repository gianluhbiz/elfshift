using UnityEngine;

namespace Elfshift.Core
{
    /// <summary>
    /// Contratto di input usato dal gioco. Il gioco non conosce tastiera o gamepad:
    /// chiede solo "dove vuole andare il giocatore?" e "ha premuto raccogli?".
    /// </summary>
    public interface IInputProvider
    {
        /// <summary>Direzione di movimento, lunghezza massima 1 (x = destra/sinistra, y = avanti/indietro).</summary>
        Vector2 Move { get; }
        /// <summary>Rotazione dello sguardo in questo frame, in GRADI (x = destra/sinistra, y = su/giù). Mouse e stick destro.</summary>
        Vector2 LookDelta { get; }
        /// <summary>Spazio / pulsante A: raccogli o posa (vero solo nel frame della pressione).</summary>
        bool PickupPressed { get; }
        /// <summary>E / pulsante X: interazione, vero finché il tasto è tenuto premuto.</summary>
        bool InteractHeld { get; }
        /// <summary>Esc / Start: pausa (vero solo nel frame della pressione).</summary>
        bool PausePressed { get; }
    }
}
