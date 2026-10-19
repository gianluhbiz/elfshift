using System;
using Elfshift.Production;
using UnityEngine;

namespace Elfshift.Player
{
    /// <summary>
    /// Gestisce l'oggetto che l'elfo tiene in mano. Per ora una sola mano (capacità 1);
    /// un miglioramento del negozio potrà aumentare la capacità nelle fasi successive.
    /// </summary>
    public sealed class Carrier : MonoBehaviour
    {
        [Tooltip("Punto davanti al petto dove compare l'oggetto trasportato.")]
        [SerializeField] private Transform handSocket;

        private bool announcedHolding;

        public WorldItem Held { get; private set; }
        public bool HasItem => Held != null;

        /// <summary>Scatta quando si prende o si lascia un oggetto (null = mani libere).</summary>
        public event Action<WorldItem> HeldChanged;

        public void Hold(WorldItem item)
        {
            if (item == null) return;
            if (HasItem)
            {
                Debug.LogWarning("[Elfshift] Carrier: le mani sono già occupate.", this);
                return;
            }
            Held = item;
            announcedHolding = true;
            Transform socket = handSocket != null ? handSocket : transform;
            item.AttachTo(socket, Vector3.zero, Vector3.zero, ItemPlacement.Held, 0.2f);
            HeldChanged?.Invoke(Held);
        }

        /// <summary>Toglie l'oggetto dalle mani (chi chiama deve poi posizionarlo).</summary>
        public WorldItem Release()
        {
            WorldItem item = Held;
            Held = null;
            if (announcedHolding)
            {
                announcedHolding = false;
                HeldChanged?.Invoke(null);
            }
            return item;
        }

        private void Update()
        {
            // Se l'oggetto in mano viene distrutto da qualcos'altro, libera le mani.
            if (announcedHolding && Held == null)
            {
                announcedHolding = false;
                HeldChanged?.Invoke(null);
            }
        }
    }
}
