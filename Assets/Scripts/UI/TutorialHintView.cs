using Elfshift.Core;
using Elfshift.Orders;
using Elfshift.Player;
using Elfshift.Production;
using UnityEngine;
using UnityEngine.UI;

namespace Elfshift.UI
{
    /// <summary>
    /// Obiettivo corrente mostrato in alto durante il primo ordine ("prendi il legno", "costruisci", "consegna").
    /// Guarda lo stato del gioco (cosa hai in mano, cosa c'è sul banco) e scrive il prossimo passo.
    /// Sparisce dopo la prima consegna.
    /// </summary>
    public sealed class TutorialHintView : MonoBehaviour
    {
        [SerializeField] private Carrier carrier;
        [SerializeField] private CraftingStation bench;
        [SerializeField] private GameSession session;
        [SerializeField] private OrderManager orders;
        [SerializeField] private GameObject root;
        [SerializeField] private Text label;
        [SerializeField] private UiPop labelPop;

        [Header("Freccia dorata sopra il prossimo bersaglio")]
        [SerializeField] private Transform marker;
        [SerializeField] private Transform shelfTarget;
        [SerializeField] private Transform benchTarget;
        [SerializeField] private Transform sleighTarget;
        [SerializeField] private Vector3 shelfOffset = new Vector3(0f, 3.5f, 0f);
        [SerializeField] private Vector3 benchOffset = new Vector3(0f, 3.1f, 0f);
        [SerializeField] private Vector3 sleighOffset = new Vector3(0f, 2.8f, 0f);

        private string shown;
        private Transform currentTarget;
        private Vector3 currentOffset;
        private float timer;

        private void Update()
        {
            timer -= Time.unscaledDeltaTime;
            if (timer > 0f) return;
            timer = 0.2f;

            if (carrier == null || bench == null || session == null || root == null || label == null) return;
            if (session.OrdersCompleted >= 1)
            {
                if (root.activeSelf) root.SetActive(false);
                if (marker != null && marker.gameObject.activeSelf) marker.gameObject.SetActive(false);
                return;
            }
            Show(ComputeGoal());
            UpdateMarker();
        }

        private void UpdateMarker()
        {
            if (marker == null) return;
            bool visible = currentTarget != null;
            if (marker.gameObject.activeSelf != visible) marker.gameObject.SetActive(visible);
            if (visible) marker.position = currentTarget.position + currentOffset;
        }

        private void SetTarget(Transform target, Vector3 offset)
        {
            currentTarget = target;
            currentOffset = offset;
        }

        private string ComputeGoal()
        {
            SetTarget(null, Vector3.zero);
            if (orders != null && orders.Active.Count == 0 && !carrier.HasItem && bench.InputCount == 0 && !bench.HasOutput)
                return UiText.GoalWaitOrder;

            if (carrier.HasItem && carrier.Held.Definition != null)
            {
                if (carrier.Held.Definition.State == ItemState.RawMaterial)
                {
                    SetTarget(benchTarget, benchOffset);
                    return UiText.GoalPlaceWood;
                }
                SetTarget(sleighTarget, sleighOffset);
                return UiText.GoalDeliver;
            }
            if (bench.HasOutput) { SetTarget(benchTarget, benchOffset); return UiText.GoalTakeToy; }
            if (bench.InputCount >= 2) { SetTarget(benchTarget, benchOffset); return UiText.GoalBuild; }
            SetTarget(shelfTarget, shelfOffset);
            if (bench.InputCount == 1) return UiText.GoalSecondWood;
            return UiText.GoalTakeWood;
        }

        private void Show(string text)
        {
            if (!root.activeSelf) root.SetActive(true);
            if (string.Equals(text, shown)) return;
            shown = text;
            label.text = text;
            if (labelPop != null) labelPop.Play();
        }
    }
}
