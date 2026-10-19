using Elfshift.Core;
using Elfshift.Progression;
using UnityEngine;
using UnityEngine.UI;

namespace Elfshift.UI
{
    /// <summary>Orologio della giornata in alto a destra: "Giorno 1" e il tempo che resta prima della chiusura.</summary>
    public sealed class DayClockView : MonoBehaviour
    {
        [SerializeField] private DayCycle day;
        [SerializeField] private Text dayLabel;
        [SerializeField] private Text timeLabel;
        [Tooltip("Disco che si svuota come un orologio (Image di tipo Filled, radiale).")]
        [SerializeField] private Image dial;
        [SerializeField] private Color normalColor = new Color(0.29f, 0.23f, 0.2f);
        [SerializeField] private Color urgentColor = new Color(0.85f, 0.34f, 0.31f);

        private int shownSeconds = -1;

        private void Start()
        {
            if (day != null && dayLabel != null) dayLabel.text = UiText.DayTitle + " " + day.DayNumber;
        }

        private void Update()
        {
            if (day == null) return;
            int seconds = Mathf.CeilToInt(day.TimeLeft);
            if (seconds != shownSeconds && timeLabel != null)
            {
                shownSeconds = seconds;
                timeLabel.text = day.IsClosing || day.IsOver ? "Chiuso" : UiText.FormatTime(seconds);
                timeLabel.color = seconds <= 60 ? urgentColor : normalColor;
            }
            if (dial != null) dial.fillAmount = day.DayLength > 0f ? day.TimeLeft / day.DayLength : 0f;
        }
    }
}
