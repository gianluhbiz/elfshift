using Elfshift.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Elfshift.UI
{
    /// <summary>Pillola "Serie x1.5": compare dalla seconda consegna di fila e salta quando la serie cresce.</summary>
    public sealed class StreakView : MonoBehaviour
    {
        [SerializeField] private GameSession session;
        [SerializeField] private GameObject root;
        [SerializeField] private Text label;
        [SerializeField] private UiPop pop;

        private void OnEnable()
        {
            if (session != null) session.StreakChanged += Refresh;
        }

        private void OnDisable()
        {
            if (session != null) session.StreakChanged -= Refresh;
        }

        private void Start()
        {
            if (session != null) Refresh(session.Streak);
        }

        private void Refresh(int streak)
        {
            bool visible = streak >= 1 && session.StreakMultiplier > 1.001f;
            if (root != null && root.activeSelf != visible) root.SetActive(visible);
            if (!visible) return;
            if (label != null) label.text = UiText.StreakTitle + " x" + session.StreakMultiplier.ToString("0.##");
            if (pop != null) pop.Punch(0.3f);
        }
    }
}
