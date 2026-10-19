using UnityEngine;

namespace Elfshift.UI
{
    /// <summary>Movimento "vivo" continuo: leggero dondolio e respiro (scala) di un elemento dell'interfaccia.</summary>
    public sealed class UiWobble : MonoBehaviour
    {
        [Min(0f)] [SerializeField] private float rotationDegrees = 1.5f;
        [Min(0f)] [SerializeField] private float scaleAmount = 0.03f;
        [Min(0f)] [SerializeField] private float floatPixels = 0f;
        [Min(0.1f)] [SerializeField] private float speed = 2.2f;

        private float phase;
        private Vector2 basePosition;
        private RectTransform rect;

        private void Awake()
        {
            phase = Random.value * 6.28f;
            rect = transform as RectTransform;
            if (rect != null) basePosition = rect.anchoredPosition;
        }

        private void Update()
        {
            float t = Time.unscaledTime * speed + phase;
            transform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(t) * rotationDegrees);
            if (scaleAmount > 0f)
            {
                float s = 1f + Mathf.Sin(t * 1.3f) * scaleAmount;
                transform.localScale = new Vector3(s, 2f - s, 1f) * 1f;
            }
            if (floatPixels > 0f && rect != null)
                rect.anchoredPosition = basePosition + new Vector2(0f, Mathf.Sin(t) * floatPixels);
        }
    }
}
