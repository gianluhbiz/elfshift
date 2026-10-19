using UnityEngine;

namespace Elfshift.UI
{
    /// <summary>
    /// Fa "saltare fuori" un elemento dell'interfaccia quando compare (scala elastica) e permette
    /// piccoli "colpetti" (Punch) per dare risalto, per esempio quando un numero aumenta.
    /// </summary>
    public sealed class UiPop : MonoBehaviour
    {
        [Min(0.05f)] [SerializeField] private float duration = 0.5f;
        [Min(0f)] [SerializeField] private float delay = 0f;
        [Range(0f, 1f)] [SerializeField] private float startScale = 0.25f;
        [Tooltip("Se vero parte da solo quando l'oggetto si attiva.")]
        [SerializeField] private bool playOnEnable = true;

        private float time = 999f;
        private float punch;

        private void OnEnable()
        {
            if (playOnEnable) Play();
            else transform.localScale = Vector3.one;
        }

        /// <summary>Riparte dall'inizio.</summary>
        public void Play()
        {
            time = -delay;
            transform.localScale = Vector3.one * startScale;
        }

        /// <summary>Piccolo rimbalzo di scala (0.2 = +20%).</summary>
        public void Punch(float amount = 0.25f)
        {
            punch = Mathf.Max(punch, amount);
        }

        private void Update()
        {
            float dt = Time.unscaledDeltaTime;
            float baseScale = 1f;
            if (time < 1000f)
            {
                time += dt;
                if (time < 0f) baseScale = startScale;
                else
                {
                    float k = time / duration;
                    baseScale = k >= 1f ? 1f : Mathf.LerpUnclamped(startScale, 1f, UiAnim.ElasticOut(k));
                    if (k >= 1f) time = 9999f;
                }
            }
            if (punch > 0.001f) punch = Mathf.MoveTowards(punch, 0f, dt * 1.6f);
            else punch = 0f;
            float pulse = 1f + Mathf.Sin(Mathf.Clamp01(punch * 3f) * Mathf.PI * 0.5f) * punch;
            transform.localScale = Vector3.one * Mathf.Max(0.01f, baseScale * pulse);
        }
    }
}
