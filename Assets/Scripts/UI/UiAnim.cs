using UnityEngine;

namespace Elfshift.UI
{
    /// <summary>Funzioni di "easing" per le animazioni dell'interfaccia (rimbalzi, elastici, rallentamenti).</summary>
    public static class UiAnim
    {
        public static float OutCubic(float t)
        {
            t = Mathf.Clamp01(t);
            float u = 1f - t;
            return 1f - u * u * u;
        }

        /// <summary>Supera il valore finale e poi si assesta (effetto "pop").</summary>
        public static float BackOut(float t, float overshoot = 1.70158f)
        {
            t = Mathf.Clamp01(t) - 1f;
            return t * t * ((overshoot + 1f) * t + overshoot) + 1f;
        }

        /// <summary>Oscilla come una molla prima di fermarsi (effetto gelatina).</summary>
        public static float ElasticOut(float t)
        {
            t = Mathf.Clamp01(t);
            if (t <= 0f) return 0f;
            if (t >= 1f) return 1f;
            const float c4 = 2f * Mathf.PI / 3f;
            return Mathf.Pow(2f, -10f * t) * Mathf.Sin((t * 10f - 0.75f) * c4) + 1f;
        }
    }
}
