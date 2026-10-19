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
        /// <summary>Tasto sinistro del mouse / grilletto destro: prendi, posa o consegna (vero solo nel frame della pressione).</summary>
        bool PickupPressed { get; }
        /// <summary>Maiusc sinistro / pressione dello stick sinistro: scatto (vero finché è tenuto premuto).</summary>
        bool SprintHeld { get; }
        /// <summary>Spazio / pulsante A: salto (vero solo nel frame della pressione).</summary>
        bool JumpPressed { get; }
        /// <summary>E / tasto destro del mouse / pulsante X: interazione, vero finché è tenuto premuto.</summary>
        bool InteractHeld { get; }
        /// <summary>Esc / Start: pausa (vero solo nel frame della pressione).</summary>
        bool PausePressed { get; }
    }
}
