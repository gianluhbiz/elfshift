using Elfshift.Core;
using Elfshift.Interaction;
using UnityEngine;

namespace Elfshift.Orders
{
    /// <summary>
    /// La cassetta delle letterine. Quando arriva una letterina la bandierina si alza e spuntano le buste;
    /// un clic (a mani vuote) le ritira e apre gli ordini: solo da quel momento parte il loro tempo.
    /// </summary>
    public sealed class Mailbox : Interactable, IUsable
    {
        [SerializeField] private OrderManager orders;
        [Tooltip("La bandierina laterale: ruota attorno al suo asse Z locale quando ci sono letterine.")]
        [SerializeField] private Transform flag;
        [Tooltip("Rotazione (gradi, asse Z locale) della bandierina alzata e abbassata.")]
        [SerializeField] private float flagRaisedAngle = 0f;
        [SerializeField] private float flagLoweredAngle = -90f;
        [Tooltip("Le buste che spuntano dalla fessura (si accendono quando c'è posta).")]
        [SerializeField] private GameObject lettersVisual;
        [SerializeField] private ParticleSystem collectEffect;

        private float flagAngle = -90f;
        private Quaternion flagBase = Quaternion.identity;
        private float wiggle;

        public bool CanUse => orders != null && orders.PendingCount > 0;

        public string UsePrompt
        {
            get
            {
                if (orders == null) return string.Empty;
                return orders.CollectableCount > 0 ? UiText.CollectLetters(orders.CollectableCount) : UiText.TooManyOrders;
            }
        }

        private void Awake()
        {
            if (orders == null) Debug.LogError("[Elfshift] Mailbox: manca il riferimento a OrderManager.", this);
            if (flag != null) flagBase = flag.localRotation;
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            if (orders != null) orders.MailArrived += OnMailArrived;
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            if (orders != null) orders.MailArrived -= OnMailArrived;
        }

        private void OnMailArrived(Order order)
        {
            wiggle = 1f;
        }

        public void Use()
        {
            if (orders == null) return;
            int collected = orders.CollectMail();
            if (collected > 0 && collectEffect != null) collectEffect.Play();
            wiggle = 0.6f;
        }

        private void Update()
        {
            bool hasMail = orders != null && orders.PendingCount > 0;
            if (lettersVisual != null && lettersVisual.activeSelf != hasMail) lettersVisual.SetActive(hasMail);

            // La bandierina sale/scende morbida e, quando arriva posta, sobbalza un po'.
            float target = hasMail ? flagRaisedAngle : flagLoweredAngle;
            flagAngle = Mathf.Lerp(flagAngle, target, 1f - Mathf.Exp(-8f * Time.deltaTime));
            wiggle = Mathf.MoveTowards(wiggle, 0f, Time.deltaTime * 1.5f);
            float shake = Mathf.Sin(Time.time * 30f) * 10f * wiggle;
            if (flag != null) flag.localRotation = flagBase * Quaternion.Euler(0f, 0f, flagAngle + shake);
        }
    }
}
