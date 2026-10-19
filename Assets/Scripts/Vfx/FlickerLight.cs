using UnityEngine;

namespace Elfshift.Vfx
{
    /// <summary>Fa tremolare dolcemente una luce, come una candela o una lanterna.</summary>
    [RequireComponent(typeof(Light))]
    public sealed class FlickerLight : MonoBehaviour
    {
        [Min(0f)] [SerializeField] private float amount = 0.12f;
        [Min(0.1f)] [SerializeField] private float speed = 3f;

        private Light target;
        private float baseIntensity;
        private float seed;

        private void Awake()
        {
            target = GetComponent<Light>();
            baseIntensity = target.intensity;
            seed = Random.value * 100f;
        }

        private void Update()
        {
            float n = Mathf.PerlinNoise(seed, Time.time * speed) - 0.5f;
            target.intensity = baseIntensity * (1f + n * 2f * amount);
        }
    }
}
