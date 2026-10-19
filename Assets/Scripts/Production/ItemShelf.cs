using System.Collections;
using Elfshift.Core;
using Elfshift.Interaction;
using UnityEngine;

namespace Elfshift.Production
{
    /// <summary>
    /// Scaffale con scorta infinita di una materia prima. Il giocatore, vicino allo scaffale,
    /// preme Spazio con le mani libere e riceve un nuovo oggetto.
    /// </summary>
    public sealed class ItemShelf : Interactable, IItemSource
    {
        [SerializeField] private ItemDefinition item;
        [Tooltip("Da dove sembra uscire l'oggetto (es. la cassa di legna). Se vuoto: lo scaffale.")]
        [SerializeField] private Transform spawnOrigin;
        [Tooltip("Parte dello scaffale che fa un piccolo 'sobbalzo' quando si prende un oggetto.")]
        [SerializeField] private Transform bounceTarget;

        private string pickupPrompt = string.Empty;
        private Vector3 bounceBaseScale = Vector3.one;
        private Coroutine bounceRoutine;

        public bool CanGive => item != null;
        public string PickupPrompt => pickupPrompt;

        private void Awake()
        {
            if (item == null)
            {
                Debug.LogError("[Elfshift] Lo scaffale '" + name + "' non ha un oggetto assegnato.", this);
                return;
            }
            pickupPrompt = UiText.Take(item.DisplayName);
            if (bounceTarget != null) bounceBaseScale = bounceTarget.localScale;
        }

        public WorldItem Take()
        {
            if (item == null) return null;
            Transform origin = spawnOrigin != null ? spawnOrigin : transform;
            WorldItem created = WorldItem.Create(item, origin.position, origin.rotation);
            if (created != null && isActiveAndEnabled && bounceTarget != null)
            {
                if (bounceRoutine != null) StopCoroutine(bounceRoutine);
                bounceRoutine = StartCoroutine(BounceRoutine());
            }
            return created;
        }

        private IEnumerator BounceRoutine()
        {
            float t = 0f;
            while (t < 1f)
            {
                t += Time.deltaTime / 0.25f;
                float k = Mathf.Clamp01(t);
                float s = 1f + Mathf.Sin(k * Mathf.PI) * 0.05f;
                bounceTarget.localScale = new Vector3(bounceBaseScale.x * s, bounceBaseScale.y * (2f - s), bounceBaseScale.z * s);
                yield return null;
            }
            bounceTarget.localScale = bounceBaseScale;
            bounceRoutine = null;
        }
    }
}
