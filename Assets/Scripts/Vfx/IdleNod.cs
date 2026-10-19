using UnityEngine;

namespace Elfshift.Vfx
{
    /// <summary>
    /// Piccolo movimento "vivo" per animali e personaggi fermi (es. la testa delle renne):
    /// ogni tanto abbassa la testa come per brucare, poi la rialza, con un leggero dondolio.
    /// </summary>
    public sealed class IdleNod : MonoBehaviour
    {
        [SerializeField] private float swayDegrees = 3f;
        [SerializeField] private float nodDegrees = 22f;
        [SerializeField] private float speed = 1f;

        private Quaternion baseRotation;
        private float seed;

        private void Awake()
        {
            baseRotation = transform.localRotation;
            seed = Random.value * 20f;
        }

        private void Update()
        {
            float t = Time.time * speed + seed;
            // dondolio continuo
            float sway = Mathf.Sin(t * 1.3f) * swayDegrees;
            float tilt = Mathf.Sin(t * 0.9f) * swayDegrees * 0.6f;
            // "brucata" ogni tanto: un impulso morbido ogni ~7 secondi
            float cycle = Mathf.Repeat(t, 7f);
            float nod = cycle < 1.6f ? Mathf.Sin(cycle / 1.6f * Mathf.PI) * nodDegrees : 0f;
            transform.localRotation = baseRotation * Quaternion.Euler(nod + tilt, sway, 0f);
        }
    }
}
