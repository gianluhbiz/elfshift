using UnityEngine;

namespace Elfshift.Vfx
{
    /// <summary>Movimento ondeggiante e rotazione lenta (stelle, decorazioni sospese, frecce).</summary>
    public sealed class Bobbing : MonoBehaviour
    {
        [SerializeField] private float height = 0.08f;
        [SerializeField] private float speed = 1.6f;
        [SerializeField] private float spinDegreesPerSecond = 0f;

        private Vector3 basePosition;
        private float phase;

        private void Awake()
        {
            basePosition = transform.localPosition;
            phase = Random.value * Mathf.PI * 2f;
        }

        private void Update()
        {
            transform.localPosition = basePosition + Vector3.up * (Mathf.Sin(Time.time * speed + phase) * height);
            if (spinDegreesPerSecond != 0f) transform.Rotate(0f, spinDegreesPerSecond * Time.deltaTime, 0f, Space.Self);
        }
    }
}
