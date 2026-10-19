using System;
using Elfshift.Core;
using Elfshift.Orders;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace Elfshift.Progression
{
    /// <summary>
    /// La giornata di lavoro: dura qualche minuto. Allo scadere non arrivano più letterine,
    /// si possono ancora consegnare quelle aperte per un breve "straordinario", poi si mostra il riepilogo
    /// con le stelle (in base ai punti). Invio (o Start sul controller) fa ripartire una nuova giornata.
    /// </summary>
    public sealed class DayCycle : MonoBehaviour
    {
        [SerializeField] private GameSession session;
        [SerializeField] private OrderManager orders;
        [Tooltip("Durata della giornata in secondi.")]
        [Min(30f)] [SerializeField] private float dayLength = 480f;
        [Tooltip("Secondi extra, dopo la chiusura, per finire gli ordini già aperti.")]
        [Min(0f)] [SerializeField] private float overtime = 30f;
        [Tooltip("Punti per 1, 2 e 3 stelle.")]
        [SerializeField] private int[] starThresholds = { 300, 700, 1200 };
        [SerializeField] private int dayNumber = 1;
        [Tooltip("Componenti da spegnere nel riepilogo (sguardo, movimento, interazioni).")]
        [SerializeField] private Behaviour[] disableOnEnd = new Behaviour[0];

        private float timeLeft;
        private float overtimeLeft;
        private bool closing;
        private bool ended;

        public float TimeLeft => timeLeft;
        public float DayLength => dayLength;
        public int DayNumber => dayNumber;
        public bool IsClosing => closing && !ended;
        public bool IsOver => ended;
        public int[] StarThresholds => starThresholds;

        /// <summary>Chiusura: non arrivano più letterine.</summary>
        public event Action Closing;
        /// <summary>Fine giornata: stelle guadagnate (0-3).</summary>
        public event Action<int> Ended;

        private void Awake()
        {
            timeLeft = dayLength;
            overtimeLeft = overtime;
            if (session == null || orders == null) Debug.LogError("[Elfshift] DayCycle: riferimenti mancanti.", this);
        }

        private void Update()
        {
            if (ended)
            {
                // Riparte una nuova giornata (stessa scena) con Invio o con Start del controller.
                bool enter = Keyboard.current != null && (Keyboard.current.enterKey.wasPressedThisFrame || Keyboard.current.numpadEnterKey.wasPressedThisFrame);
                bool pad = Gamepad.current != null && Gamepad.current.startButton.wasPressedThisFrame;
                if (enter || pad) Restart();
                return;
            }

            float dt = Time.deltaTime;
            if (!closing)
            {
                timeLeft = Mathf.Max(0f, timeLeft - dt);
                if (timeLeft <= 0f)
                {
                    closing = true;
                    if (orders != null) orders.AutoSpawn = false;
                    Closing?.Invoke();
                }
                return;
            }

            // Straordinario: finisce quando non ci sono più ordini aperti o scade il tempo extra.
            overtimeLeft -= dt;
            bool nothingOpen = orders == null || orders.Active.Count == 0;
            if (overtimeLeft <= 0f || nothingOpen) EndDay();
        }

        public int StarsFor(int points)
        {
            int stars = 0;
            for (int i = 0; i < starThresholds.Length; i++)
                if (points >= starThresholds[i]) stars = i + 1;
            return stars;
        }

        private void EndDay()
        {
            ended = true;
            int stars = StarsFor(session != null ? session.Points : 0);
            Time.timeScale = 0f;
            for (int i = 0; i < disableOnEnd.Length; i++)
                if (disableOnEnd[i] != null) disableOnEnd[i].enabled = false;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            Ended?.Invoke(stars);
        }

        public void Restart()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        private void OnDestroy()
        {
            // Non lasciare il gioco in pausa se la scena viene chiusa durante il riepilogo.
            if (ended) Time.timeScale = 1f;
        }
    }
}
