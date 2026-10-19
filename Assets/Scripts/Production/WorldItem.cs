using System.Collections;
using Elfshift.Core;
using Elfshift.Interaction;
using UnityEngine;

namespace Elfshift.Production
{
    /// <summary>Dove si trova fisicamente un oggetto in questo momento.</summary>
    public enum ItemPlacement
    {
        /// <summary>Appoggiato a terra: si può raccogliere.</summary>
        Floor = 0,
        /// <summary>In mano all'elfo.</summary>
        Held = 1,
        /// <summary>Sopra un banco (ingresso o uscita).</summary>
        Slot = 2,
        /// <summary>Sta per sparire (consegnato).</summary>
        Consumed = 3
    }

    /// <summary>
    /// Un oggetto fisico del mondo (legno, trenino...). Il suo aspetto viene sempre dal prefab
    /// indicato nel <see cref="ItemDefinition"/>: cambiando il prefab cambia il modello ovunque.
    /// Non ha fisica: si sposta con piccole animazioni, quindi non può cadere fuori dalla mappa.
    /// </summary>
    public sealed class WorldItem : Interactable, IItemSource
    {
        private const float FloorPickupRadius = 1.25f;

        private Coroutine moveRoutine;
        private Coroutine popRoutine;

        public ItemDefinition Definition { get; private set; }
        public ItemPlacement Placement { get; private set; } = ItemPlacement.Floor;

        public bool CanGive => Placement == ItemPlacement.Floor && Definition != null;
        public string PickupPrompt { get; private set; } = string.Empty;

        /// <summary>Crea un oggetto nel mondo con il suo modello 3D.</summary>
        public static WorldItem Create(ItemDefinition definition, Vector3 position, Quaternion rotation, Transform parent = null)
        {
            if (definition == null)
            {
                Debug.LogError("[Elfshift] WorldItem.Create chiamato senza ItemDefinition.");
                return null;
            }

            GameObject go = new GameObject(definition.DisplayName);
            go.transform.SetParent(parent, false);
            go.transform.SetPositionAndRotation(position, rotation);

            WorldItem item = go.AddComponent<WorldItem>();
            item.Initialize(definition);
            return item;
        }

        private void Initialize(ItemDefinition definition)
        {
            Definition = definition;
            Placement = ItemPlacement.Floor;
            PickupPrompt = UiText.Take(definition.DisplayName);
            SetInteraction(FloorPickupRadius, transform);
            BuildVisual(definition);
        }

        private void BuildVisual(ItemDefinition definition)
        {
            GameObject visual;
            if (definition.VisualPrefab != null)
            {
                visual = Instantiate(definition.VisualPrefab, transform);
            }
            else
            {
                // Segnaposto di emergenza: succede solo se manca il prefab nell'asset.
                Debug.LogWarning("[Elfshift] L'oggetto '" + definition.name + "' non ha un Visual Prefab.", definition);
                visual = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                visual.transform.SetParent(transform, false);
                visual.transform.localScale = Vector3.one * 0.3f;
                Renderer r = visual.GetComponent<Renderer>();
                if (r != null) r.material.color = definition.AccentColor;
            }

            visual.name = "Visual";
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localRotation = Quaternion.identity;

            // Gli oggetti non devono bloccare il giocatore.
            Collider[] colliders = visual.GetComponentsInChildren<Collider>();
            for (int i = 0; i < colliders.Length; i++) colliders[i].enabled = false;
        }

        // ---------------------------------------------------------------- IItemSource
        public WorldItem Take()
        {
            return CanGive ? this : null;
        }

        // ---------------------------------------------------------------- Movimento
        /// <summary>
        /// Sposta l'oggetto sotto un nuovo "genitore" (mano, slot del banco, terra se null)
        /// con una breve animazione ad arco.
        /// </summary>
        public void AttachTo(Transform parent, Vector3 localPosition, Vector3 localEuler, ItemPlacement placement, float duration = 0.18f)
        {
            Placement = placement;
            StopMove();
            transform.SetParent(parent, true);
            transform.localScale = Vector3.one;
            Quaternion targetRotation = Quaternion.Euler(localEuler);

            if (duration <= 0f || !isActiveAndEnabled)
            {
                transform.localPosition = localPosition;
                transform.localRotation = targetRotation;
                return;
            }
            moveRoutine = StartCoroutine(MoveRoutine(localPosition, targetRotation, duration));
        }

        /// <summary>Posa l'oggetto a terra nella posizione indicata.</summary>
        public void PlaceOnFloor(Vector3 worldPosition, float yawDegrees)
        {
            AttachTo(null, worldPosition, new Vector3(0f, yawDegrees, 0f), ItemPlacement.Floor, 0.22f);
        }

        /// <summary>Piccola animazione "pop" (rimbalzo di scala), usata quando un oggetto appare.</summary>
        public void Pop()
        {
            if (!isActiveAndEnabled) return;
            if (popRoutine != null) StopCoroutine(popRoutine);
            popRoutine = StartCoroutine(PopRoutine());
        }

        /// <summary>L'oggetto vola verso un punto, rimpicciolisce e sparisce (consegna).</summary>
        public void FlyIntoAndDestroy(Transform target, Vector3 localTarget, float duration)
        {
            Placement = ItemPlacement.Consumed;
            StopMove();
            if (!isActiveAndEnabled || target == null)
            {
                Destroy(gameObject);
                return;
            }
            transform.SetParent(target, true);
            moveRoutine = StartCoroutine(FlyRoutine(localTarget, duration));
        }

        private void StopMove()
        {
            if (moveRoutine != null)
            {
                StopCoroutine(moveRoutine);
                moveRoutine = null;
            }
        }

        private IEnumerator MoveRoutine(Vector3 targetPos, Quaternion targetRot, float duration)
        {
            Vector3 startPos = transform.localPosition;
            Quaternion startRot = transform.localRotation;
            float arc = Mathf.Clamp(Vector3.Distance(startPos, targetPos) * 0.25f, 0.05f, 0.35f);
            float t = 0f;
            while (t < 1f)
            {
                t += Time.deltaTime / duration;
                float k = Mathf.Clamp01(t);
                float eased = 1f - (1f - k) * (1f - k) * (1f - k);
                transform.localPosition = Vector3.Lerp(startPos, targetPos, eased) + Vector3.up * (Mathf.Sin(k * Mathf.PI) * arc);
                transform.localRotation = Quaternion.Slerp(startRot, targetRot, eased);
                yield return null;
            }
            transform.localPosition = targetPos;
            transform.localRotation = targetRot;
            moveRoutine = null;
        }

        private IEnumerator FlyRoutine(Vector3 targetPos, float duration)
        {
            Vector3 startPos = transform.localPosition;
            float t = 0f;
            while (t < 1f)
            {
                t += Time.deltaTime / duration;
                float k = Mathf.Clamp01(t);
                transform.localPosition = Vector3.Lerp(startPos, targetPos, k) + Vector3.up * (Mathf.Sin(k * Mathf.PI) * 0.6f);
                transform.localScale = Vector3.one * Mathf.Lerp(1f, 0.15f, k * k);
                transform.Rotate(0f, 540f * Time.deltaTime, 0f, Space.Self);
                yield return null;
            }
            Destroy(gameObject);
        }

        private IEnumerator PopRoutine()
        {
            float t = 0f;
            const float duration = 0.35f;
            while (t < 1f)
            {
                t += Time.deltaTime / duration;
                float k = Mathf.Clamp01(t);
                // Sovrascala leggermente e poi si assesta a 1.
                float s = 1f + Mathf.Sin(k * Mathf.PI) * 0.25f - (1f - k) * (1f - k) * 0.8f;
                transform.localScale = Vector3.one * Mathf.Max(0.05f, s);
                yield return null;
            }
            transform.localScale = Vector3.one;
            popRoutine = null;
        }
    }
}
