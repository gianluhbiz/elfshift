using Elfshift.Player;
using Elfshift.Production;
using UnityEngine;
using UnityEngine.UI;

namespace Elfshift.UI
{
    /// <summary>Riquadro in basso a sinistra: cosa tiene in mano l'elfo.</summary>
    public sealed class CarriedItemView : MonoBehaviour
    {
        [SerializeField] private Carrier carrier;
        [SerializeField] private ItemIconService icons;
        [SerializeField] private GameObject root;
        [SerializeField] private RawImage icon;
        [SerializeField] private Text label;

        private void OnEnable()
        {
            if (carrier == null)
            {
                Debug.LogError("[Elfshift] CarriedItemView: manca Carrier.", this);
                return;
            }
            carrier.HeldChanged += OnHeldChanged;
            OnHeldChanged(carrier.Held);
        }

        private void OnDisable()
        {
            if (carrier != null) carrier.HeldChanged -= OnHeldChanged;
        }

        private void OnHeldChanged(WorldItem item)
        {
            bool has = item != null && item.Definition != null;
            if (root != null) root.SetActive(has);
            if (!has) return;
            if (label != null) label.text = item.Definition.DisplayName;
            if (icon != null)
            {
                Texture texture = icons != null ? icons.GetIcon(item.Definition) : null;
                icon.texture = texture;
                icon.enabled = texture != null;
            }
        }
    }
}
