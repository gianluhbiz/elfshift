using UnityEngine;

namespace Elfshift.Vfx
{
    /// <summary>
    /// Fa lampeggiare dolcemente le lucine di Natale (emissione dei materiali).
    /// Usa MaterialPropertyBlock: non modifica gli asset dei materiali.
    /// </summary>
    public sealed class TwinkleLights : MonoBehaviour
    {
        [SerializeField] private Renderer[] bulbs = new Renderer[0];
        [SerializeField] private Color[] bulbColors = new Color[0];
        [Min(0f)] [SerializeField] private float baseIntensity = 1.6f;
        [Range(0f, 1f)] [SerializeField] private float variation = 0.55f;
        [Min(0.1f)] [SerializeField] private float speed = 2.2f;
        [Min(0.02f)] [SerializeField] private float updateInterval = 0.06f;

        private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");
        private MaterialPropertyBlock block;
        private float timer;

        private void Awake()
        {
            block = new MaterialPropertyBlock();
        }

        private void Update()
        {
            timer -= Time.deltaTime;
            if (timer > 0f) return;
            timer = updateInterval;

            float t = Time.time * speed;
            for (int i = 0; i < bulbs.Length; i++)
            {
                if (bulbs[i] == null) continue;
                float wave = 0.5f + 0.5f * Mathf.Sin(t + i * 1.9f);
                float intensity = baseIntensity * (1f - variation + variation * wave);
                Color baseColor = i < bulbColors.Length ? bulbColors[i] : Color.white;
                bulbs[i].GetPropertyBlock(block);
                block.SetColor(EmissionColorId, baseColor * intensity);
                bulbs[i].SetPropertyBlock(block);
            }
        }

#if UNITY_EDITOR
        public void EditorSetup(Renderer[] renderers, Color[] colors)
        {
            bulbs = renderers;
            bulbColors = colors;
        }
#endif
    }
}
