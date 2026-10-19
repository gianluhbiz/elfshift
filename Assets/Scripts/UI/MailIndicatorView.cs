using Elfshift.Orders;
using UnityEngine;
using UnityEngine.UI;

namespace Elfshift.UI
{
    /// <summary>Pillola con la busta e il numero di letterine che aspettano nella cassetta della posta.</summary>
    public sealed class MailIndicatorView : MonoBehaviour
    {
        [SerializeField] private OrderManager orders;
        [SerializeField] private GameObject root;
        [SerializeField] private Text countText;
        [SerializeField] private UiPop pop;

        private int shown = -1;

        private void Update()
        {
            if (orders == null) return;
            int count = orders.PendingCount;
            if (count == shown) return;
            bool grew = count > shown && shown >= 0;
            shown = count;
            bool visible = count > 0;
            if (root != null && root.activeSelf != visible) root.SetActive(visible);
            if (countText != null) countText.text = count.ToString();
            if (grew && pop != null) pop.Punch(0.35f);
        }
    }
}
