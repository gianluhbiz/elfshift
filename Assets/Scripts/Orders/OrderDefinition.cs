using Elfshift.Core;
using UnityEngine;

namespace Elfshift.Orders
{
    /// <summary>
    /// Una "letterina" possibile: quale oggetto chiede, in quanto tempo e quanto vale.
    /// Gli ordini veri vengono creati a partire da questi asset.
    /// </summary>
    [CreateAssetMenu(menuName = "Elfshift/Ordine", fileName = "Order_Nuovo")]
    public sealed class OrderDefinition : ScriptableObject
    {
        [Tooltip("Oggetto da consegnare alla slitta (deve essere nello stato richiesto, es. Trenino).")]
        [SerializeField] private ItemDefinition requestedItem;
        [Tooltip("Oggetto disegnato sulla letterina (es. il trenino dipinto, anche se va consegnato il pacco). Se vuoto: l'oggetto richiesto.")]
        [SerializeField] private ItemDefinition letterItem;
        [Min(10f)]
        [SerializeField] private float timeLimitSeconds = 120f;
        [Min(0)] [SerializeField] private int rewardPoints = 100;
        [Min(0)] [SerializeField] private int rewardCookies = 5;
        [Min(0)]
        [Tooltip("Punti extra proporzionali al tempo rimasto alla consegna.")]
        [SerializeField] private int speedBonusPoints = 50;
        [Min(0)]
        [Tooltip("Punti persi se l'ordine scade.")]
        [SerializeField] private int failPenaltyPoints = 20;

        public ItemDefinition RequestedItem => requestedItem;
        public ItemDefinition LetterItem => letterItem != null ? letterItem : requestedItem;
        public float TimeLimitSeconds => timeLimitSeconds;
        public int RewardPoints => rewardPoints;
        public int RewardCookies => rewardCookies;
        public int SpeedBonusPoints => speedBonusPoints;
        public int FailPenaltyPoints => failPenaltyPoints;

#if UNITY_EDITOR
        public void EditorSetup(ItemDefinition item, float seconds, int points, int cookies, int bonus, int penalty, ItemDefinition shownOnLetter = null)
        {
            requestedItem = item;
            letterItem = shownOnLetter;
            timeLimitSeconds = seconds;
            rewardPoints = points;
            rewardCookies = cookies;
            speedBonusPoints = bonus;
            failPenaltyPoints = penalty;
        }
#endif
    }
}
