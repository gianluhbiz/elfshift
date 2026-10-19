using Elfshift.Core;
using Elfshift.Interaction;
using UnityEngine;

namespace Elfshift.Production
{
    /// <summary>
    /// Il camino del laboratorio: va tenuto acceso mettendoci la legna.
    /// Il calore cala piano col tempo; con il fuoco vivace gli elfi lavorano più in fretta,
    /// con il fuoco quasi spento le mani gelano e tutti i banchi diventano lenti.
    /// </summary>
    public sealed class Fireplace : Interactable, IItemReceiver
    {
        [Tooltip("Cosa si può bruciare (di solito il Legno).")]
        [SerializeField] private ItemDefinition fuel;
        [Tooltip("Dove vola il ciocco messo nel fuoco.")]
        [SerializeField] private Transform burnPoint;
        [Tooltip("Banchi di lavoro influenzati dal calore.")]
        [SerializeField] private CraftingStation[] stations = new CraftingStation[0];

        [Header("Calore (0-100)")]
        [Range(0f, 100f)] [SerializeField] private float startWarmth = 70f;
        [Min(1f)] [Tooltip("Calore aggiunto da un ciocco.")] [SerializeField] private float warmthPerLog = 40f;
        [Min(0f)] [Tooltip("Calore perso al secondo.")] [SerializeField] private float decayPerSecond = 0.55f;
        [Range(0f, 100f)] [Tooltip("Sotto questa soglia fa freddo e si lavora piano.")] [SerializeField] private float coldThreshold = 25f;
        [Range(0f, 100f)] [Tooltip("Sopra questa soglia il fuoco è vivace e si lavora più in fretta.")] [SerializeField] private float cozyThreshold = 70f;
        [Range(0.2f, 1f)] [SerializeField] private float coldSpeed = 0.6f;
        [Range(1f, 2f)] [SerializeField] private float cozySpeed = 1.2f;

        [Header("Resa (facoltativa)")]
        [Tooltip("Fiamme: scalate in base al calore (usa un oggetto genitore, non quello che tremola).")]
        [SerializeField] private Transform flames;
        [Tooltip("Luce del fuoco: la sua portata cresce col calore (l'intensità la fa tremolare FlickerLight).")]
        [SerializeField] private Light fireLight;
        [SerializeField] private ParticleSystem burstEffect;

        private float warmth;
        private Vector3 flamesBaseScale = Vector3.one;
        private float lightBase = 1f;
        private float burst;

        /// <summary>Calore attuale da 0 a 1.</summary>
        public float Warmth01 => warmth / 100f;
        public bool IsCold => warmth < coldThreshold;
        public bool IsCozy => warmth >= cozyThreshold;

        /// <summary>Velocità di lavoro dei banchi in base al calore.</summary>
        public float WorkSpeed => IsCold ? coldSpeed : (IsCozy ? cozySpeed : 1f);

        public string PlacePrompt => UiText.FeedFire;

        private void Awake()
        {
            warmth = startWarmth;
            if (flames != null) flamesBaseScale = flames.localScale;
            if (fireLight != null) lightBase = fireLight.range;
            if (fuel == null) Debug.LogError("[Elfshift] Fireplace: manca l'oggetto combustibile (Legno).", this);
        }

        public bool Accepts(ItemDefinition item, out string reason)
        {
            reason = null;
            if (item != null && ReferenceEquals(item, fuel))
            {
                if (warmth > 100f - warmthPerLog * 0.25f) { reason = UiText.FireFull; return false; }
                return true;
            }
            reason = UiText.OnlyWoodInFire;
            return false;
        }

        public void Receive(WorldItem item)
        {
            if (item == null) return;
            Transform target = burnPoint != null ? burnPoint : transform;
            item.FlyIntoAndDestroy(target, Vector3.zero, 0.4f);
            AddWarmth(warmthPerLog);
            burst = 1f;
            if (burstEffect != null) burstEffect.Play();
        }

        /// <summary>Aggiunge calore (anche da script, es. per i test).</summary>
        public void AddWarmth(float amount)
        {
            warmth = Mathf.Clamp(warmth + amount, 0f, 100f);
            ApplySpeed();
        }

        private void Update()
        {
            warmth = Mathf.Max(0f, warmth - decayPerSecond * Time.deltaTime);
            ApplySpeed();

            // Fiamme e luce seguono il calore (con una vampata quando si aggiunge legna).
            burst = Mathf.MoveTowards(burst, 0f, Time.deltaTime * 1.2f);
            float k = Mathf.Lerp(0.25f, 1.1f, Warmth01) + burst * 0.35f;
            if (flames != null) flames.localScale = flamesBaseScale * k;
            if (fireLight != null) fireLight.range = lightBase * Mathf.Lerp(0.45f, 1.1f, Warmth01) * (1f + burst * 0.25f);
        }

        private void ApplySpeed()
        {
            float speed = WorkSpeed;
            for (int i = 0; i < stations.Length; i++)
                if (stations[i] != null) stations[i].SpeedMultiplier = speed;
        }
    }
}
