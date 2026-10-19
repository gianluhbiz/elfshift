using System.Collections.Generic;
using UnityEngine;

namespace Elfshift.Interaction
{
    /// <summary>
    /// Elenco di tutti gli oggetti interagibili attivi. Il giocatore lo scorre per trovare
    /// il più vicino: niente ricerche nella scena e niente allocazioni durante il gioco.
    /// </summary>
    public static class InteractableRegistry
    {
        private static readonly List<Interactable> items = new List<Interactable>(64);

        public static IReadOnlyList<Interactable> All => items;

        public static void Register(Interactable item)
        {
            if (item != null && !items.Contains(item)) items.Add(item);
        }

        public static void Unregister(Interactable item)
        {
            items.Remove(item);
        }

        // Con "Enter Play Mode Options" (senza ricaricare il dominio) i campi statici
        // restano: li azzeriamo a ogni avvio per non tenere riferimenti a oggetti distrutti.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetOnLoad()
        {
            items.Clear();
        }
    }
}
