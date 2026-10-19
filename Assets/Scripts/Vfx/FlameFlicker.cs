using UnityEngine;

namespace Elfshift.Vfx
{
    /// <summary>Fa ondeggiare le fiamme del camino: altezza e larghezza cambiano in modo irregolare.</summary>
    public sealed class FlameFlicker : MonoBehaviour
    {
        [SerializeField] private Transform[] flames = new Transform[0];
        [Min(0f)] [SerializeField] private float amount = 0.28f;
        [Min(0.1f)] [SerializeField] private float speed = 6f;

        private Vector3[] baseScales;
        private float seed;

        private void Awake()
        {
            seed = Random.value * 50f;
            baseScales = new Vector3[flames.Length];
            for (int i = 0; i < flames.Length; i++)
                if (flames[i] != null) baseScales[i] = flames[i].localScale;
        }

        private void Update()
        {
            for (int i = 0; i < flames.Length; i++)
            {
                if (flames[i] == null) continue;
                float n = Mathf.PerlinNoise(seed + i * 3.7f, Time.time * speed) - 0.5f;
                float m = Mathf.PerlinNoise(seed + 20f + i * 1.9f, Time.time * speed * 0.8f) - 0.5f;
                Vector3 b = baseScales[i];
                flames[i].localScale = new Vector3(b.x * (1f - m * amount), b.y * (1f + n * 2f * amount), b.z * (1f - m * amount));
            }
        }

#if UNITY_EDITOR
        public void EditorSetup(Transform[] flameTransforms)
        {
            flames = flameTransforms;
        }
#endif
    }
}
