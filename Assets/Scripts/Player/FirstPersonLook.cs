using Elfshift.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Elfshift.Player
{
    /// <summary>
    /// Sguardo in prima persona: il mouse (o lo stick destro) ruota l'elfo a destra/sinistra e la
    /// telecamera su/giù. Il cursore viene bloccato al centro della finestra di gioco.
    /// Esc libera il cursore (per uscire dalla finestra di gioco); un clic lo blocca di nuovo.
    /// Va messo sull'oggetto Telecamera, figlio dell'elfo.
    /// </summary>
    public sealed class FirstPersonLook : MonoBehaviour
    {
        [SerializeField] private InputReader input;
        [Tooltip("L'oggetto che ruota a destra/sinistra (l'elfo).")]
        [SerializeField] private Transform body;
        [Range(-89f, 0f)] [SerializeField] private float minPitch = -80f;
        [Range(0f, 89f)] [SerializeField] private float maxPitch = 80f;

        private float pitch;

        /// <summary>Inclinazione laterale della visuale in gradi (la imposta FirstPersonFeel).</summary>
        public float Roll { get; set; }
        private bool cursorLocked;

        /// <summary>Se falso lo sguardo resta fermo (menu, pausa, sequenze).</summary>
        public bool LookEnabled { get; set; } = true;

        private void OnEnable()
        {
            if (input == null || body == null)
            {
                Debug.LogError("[Elfshift] FirstPersonLook: riferimenti mancanti (input o corpo).", this);
                return;
            }
            SetCursorLocked(true);
        }

        private void OnDisable()
        {
            SetCursorLocked(false);
        }

        private void Update()
        {
            if (input == null || body == null) return;

            if (cursorLocked && input.PausePressed) SetCursorLocked(false);
            else if (!cursorLocked && Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame) SetCursorLocked(true);

            if (!cursorLocked || !LookEnabled)
            {
                transform.localRotation = Quaternion.Euler(pitch, 0f, Roll);
                return;
            }

            Vector2 look = input.LookDelta;
            body.Rotate(0f, look.x, 0f, Space.World);
            pitch = Mathf.Clamp(pitch - look.y, minPitch, maxPitch);
            transform.localRotation = Quaternion.Euler(pitch, 0f, Roll);
        }

        private void SetCursorLocked(bool locked)
        {
            cursorLocked = locked;
            Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !locked;
        }
    }
}
