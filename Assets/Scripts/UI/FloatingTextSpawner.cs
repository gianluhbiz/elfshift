using UnityEngine;
using UnityEngine.UI;

namespace Elfshift.UI
{
    /// <summary>Crea scritte che volano via ("+100", "+5") partendo da un punto dell'interfaccia.</summary>
    public sealed class FloatingTextSpawner : MonoBehaviour
    {
        [Tooltip("Scritta modello (disattivata): viene copiata ogni volta.")]
        [SerializeField] private Text template;

        public void Spawn(string text, Transform from, Color color, Vector2 pixelOffset)
        {
            if (template == null || from == null) return;
            Text copy = Instantiate(template, template.transform.parent);
            copy.gameObject.SetActive(true);
            copy.transform.position = from.position + (Vector3)(pixelOffset * (Screen.height / 720f));
            FloatingText floating = copy.gameObject.AddComponent<FloatingText>();
            floating.Begin(text, color);
        }
    }
}
