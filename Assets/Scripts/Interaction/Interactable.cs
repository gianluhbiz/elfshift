using UnityEngine;

namespace Elfshift.Interaction
{
    /// <summary>
    /// Base di tutto ciò con cui il giocatore può interagire (scaffali, banchi, slitta, oggetti a terra).
    /// Si registra da solo nel <see cref="InteractableRegistry"/>: il giocatore controlla la DISTANZA
    /// dal punto di interazione, quindi nulla funziona "da qualsiasi punto della mappa".
    /// </summary>
    public abstract class Interactable : MonoBehaviour
    {
        [Tooltip("Punto da cui si misura la distanza. Se vuoto si usa la posizione dell'oggetto.")]
        [SerializeField] private Transform interactionPoint;
        [Min(0.2f)]
        [Tooltip("Distanza massima (in metri, sul piano) a cui il giocatore può interagire.")]
        [SerializeField] private float radius = 1.7f;
        [Tooltip("Nome mostrato come suggerimento quando lo guardi da lontano (es. \"Scaffale del legno\"). Vuoto = nessun suggerimento.")]
        [SerializeField] private string displayName = "";
        [Min(0f)]
        [Tooltip("Altezza (dal piano di appoggio) del punto che il giocatore deve guardare per interagire.")]
        [SerializeField] private float focusHeight = 0.8f;

        public Vector3 Position => interactionPoint != null ? interactionPoint.position : transform.position;
        public float Radius => radius;
        public string DisplayName => displayName;

        /// <summary>Punto da guardare (in prima persona si interagisce con ciò che si ha davanti).</summary>
        public Vector3 FocusPoint => transform.position + Vector3.up * focusHeight;

        protected void SetFocusHeight(float height)
        {
            focusHeight = Mathf.Max(0f, height);
        }

        protected void SetInteraction(float newRadius, Transform point)
        {
            radius = Mathf.Max(0.2f, newRadius);
            interactionPoint = point;
        }

        protected virtual void OnEnable()
        {
            InteractableRegistry.Register(this);
        }

        protected virtual void OnDisable()
        {
            InteractableRegistry.Unregister(this);
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.85f, 0.2f, 0.8f);
            Gizmos.DrawWireSphere(Position, radius);
        }
#endif
    }
}
