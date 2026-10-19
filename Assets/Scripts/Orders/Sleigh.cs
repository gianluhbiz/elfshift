using System.Collections;
using Elfshift.Core;
using Elfshift.Interaction;
using Elfshift.Production;
using UnityEngine;

namespace Elfshift.Orders
{
    /// <summary>
    /// La slitta: zona di consegna. Accetta un pacco solo se il giocatore è vicino
    /// E se esiste un ordine aperto che lo richiede.
    /// </summary>
    public sealed class Sleigh : Interactable, IItemReceiver
    {
        [SerializeField] private OrderManager orders;
        [Tooltip("Punto (dentro la slitta) verso cui vola l'oggetto consegnato.")]
        [SerializeField] private Transform dropPoint;
        [Tooltip("Parte della slitta che dondola alla consegna.")]
        [SerializeField] private Transform bounceTarget;
        [SerializeField] private ParticleSystem deliveryEffect;

        private Coroutine bounceRoutine;
        private Vector3 bounceBaseScale = Vector3.one;

        public string PlacePrompt => UiText.DeliverToSleigh;

        private void Awake()
        {
            if (orders == null) Debug.LogError("[Elfshift] Sleigh: manca il riferimento a OrderManager.", this);
            if (bounceTarget != null) bounceBaseScale = bounceTarget.localScale;
        }

        public bool Accepts(ItemDefinition item, out string reason)
        {
            reason = null;
            if (orders == null || orders.FindOpenOrderFor(item) == null)
            {
                reason = UiText.NoOrderForThis;
                return false;
            }
            return true;
        }

        public void Receive(WorldItem item)
        {
            if (item == null || orders == null) return;
            Order order = orders.FindOpenOrderFor(item.Definition);
            if (order == null) return;

            Transform target = dropPoint != null ? dropPoint : transform;
            item.FlyIntoAndDestroy(target, Vector3.zero, 0.45f);
            orders.Complete(order);

            if (deliveryEffect != null) deliveryEffect.Play();
            if (bounceTarget != null && isActiveAndEnabled)
            {
                if (bounceRoutine != null) StopCoroutine(bounceRoutine);
                bounceRoutine = StartCoroutine(BounceRoutine());
            }
        }

        private IEnumerator BounceRoutine()
        {
            yield return new WaitForSeconds(0.35f);
            float t = 0f;
            while (t < 1f)
            {
                t += Time.deltaTime / 0.45f;
                float k = Mathf.Clamp01(t);
                float s = 1f + Mathf.Sin(k * Mathf.PI * 2f) * (1f - k) * 0.06f;
                bounceTarget.localScale = new Vector3(bounceBaseScale.x, bounceBaseScale.y * s, bounceBaseScale.z);
                yield return null;
            }
            bounceTarget.localScale = bounceBaseScale;
            bounceRoutine = null;
        }
    }
}
