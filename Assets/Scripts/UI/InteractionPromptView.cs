using Elfshift.Core;
using Elfshift.Player;
using UnityEngine;
using UnityEngine.UI;

namespace Elfshift.UI
{
    /// <summary>
    /// Indicazioni contestuali in basso: "[SPAZIO] Prendi Legno", "[E] Tieni premuto: ..."
    /// e la barra di avanzamento delle interazioni a durata.
    /// </summary>
    public sealed class InteractionPromptView : MonoBehaviour
    {
        [SerializeField] private PlayerInteractor interactor;

        [Header("Chip Spazio")]
        [SerializeField] private GameObject pickupChip;
        [SerializeField] private Text pickupKeyText;
        [SerializeField] private Text pickupLabel;

        [Header("Chip E")]
        [SerializeField] private GameObject interactChip;
        [SerializeField] private Text interactKeyText;
        [SerializeField] private Text interactLabel;

        [Header("Suggerimento da lontano")]
        [SerializeField] private Text hintLabel;

        [Header("Mirino")]
        [SerializeField] private Image crosshair;

        [Header("Barra avanzamento")]
        [SerializeField] private GameObject progressRoot;
        [SerializeField] private RectTransform progressFill;

        private void Awake()
        {
            if (pickupKeyText != null) pickupKeyText.text = UiText.KeyPickup;
            if (interactKeyText != null) interactKeyText.text = UiText.KeyInteract;
            SetPrompts(null, null);
            SetProgress(-1f);
            SetHint(null);
        }

        private void OnEnable()
        {
            if (interactor == null)
            {
                Debug.LogError("[Elfshift] InteractionPromptView: manca PlayerInteractor.", this);
                return;
            }
            interactor.PromptChanged += SetPrompts;
            interactor.HoldProgressChanged += SetProgress;
            interactor.HintChanged += SetHint;
        }

        private void OnDisable()
        {
            if (interactor == null) return;
            interactor.PromptChanged -= SetPrompts;
            interactor.HoldProgressChanged -= SetProgress;
            interactor.HintChanged -= SetHint;
        }

        private bool targetInFocus;
        private float crosshairScale = 1f;

        private void Update()
        {
            // Il mirino si allarga e diventa dorato quando c'è qualcosa da fare.
            if (crosshair == null) return;
            float targetScale = targetInFocus ? 1.9f + Mathf.Sin(Time.unscaledTime * 6f) * 0.12f : 1f;
            crosshairScale = Mathf.Lerp(crosshairScale, targetScale, 1f - Mathf.Exp(-14f * Time.unscaledDeltaTime));
            crosshair.transform.localScale = Vector3.one * crosshairScale;
            Color wanted = targetInFocus ? new Color(1f, 0.85f, 0.25f, 1f) : new Color(1f, 1f, 1f, 0.8f);
            crosshair.color = Color.Lerp(crosshair.color, wanted, 1f - Mathf.Exp(-14f * Time.unscaledDeltaTime));
        }

        private void SetHint(string text)
        {
            if (hintLabel == null) return;
            bool visible = !string.IsNullOrEmpty(text);
            hintLabel.gameObject.SetActive(visible);
            if (visible) hintLabel.text = text;
        }

        private void SetPrompts(string pickup, string interact)
        {
            bool showPickup = !string.IsNullOrEmpty(pickup);
            bool showInteract = !string.IsNullOrEmpty(interact);
            targetInFocus = showPickup || showInteract;
            if (pickupChip != null) pickupChip.SetActive(showPickup);
            if (interactChip != null) interactChip.SetActive(showInteract);
            if (showPickup && pickupLabel != null) pickupLabel.text = pickup;
            if (showInteract && interactLabel != null) interactLabel.text = interact;
        }

        private void SetProgress(float value)
        {
            bool visible = value >= 0f;
            if (progressRoot != null) progressRoot.SetActive(visible);
            if (visible && progressFill != null) progressFill.anchorMax = new Vector2(Mathf.Clamp01(value), 1f);
        }
    }
}
