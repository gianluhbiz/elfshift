using Elfshift.Core;
using UnityEngine;

namespace Elfshift.Player
{
    /// <summary>
    /// Movimento dell'elfo in prima persona: W/S avanti/indietro, A/D passo laterale, Maiusc scatto, Spazio salto, relativi alla direzione
    /// in cui guardi. Movimento "piantato a terra": la direzione segue SUBITO lo sguardo (niente effetto ghiaccio
    /// quando giri il mouse), la partenza è rapidissima e lo stop quasi istantaneo. Collisioni con i mobili tramite
    /// CharacterController (nessuna compenetrazione). La rotazione è gestita da <see cref="FirstPersonLook"/>.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public sealed class PlayerController : MonoBehaviour
    {
        [SerializeField] private InputReader input;

        [Header("Movimento")]
        [Min(0.5f)] [SerializeField] private float maxSpeed = 4.6f;
        [Min(1f)] [Tooltip("Di quante volte aumenta la velocità tenendo premuto Maiusc.")] [SerializeField] private float sprintMultiplier = 1.7f;
        [Min(1f)] [Tooltip("Metri al secondo guadagnati ogni secondo in partenza (alto = scattante).")] [SerializeField] private float acceleration = 60f;
        [Min(1f)] [Tooltip("Metri al secondo persi ogni secondo quando lasci i tasti (alto = si ferma subito).")] [SerializeField] private float deceleration = 90f;
        [Range(0f, 1f)] [Tooltip("Quanto si può correggere la direzione in aria (0 = niente, 1 = come a terra).")] [SerializeField] private float airControl = 0.6f;
        [SerializeField] private float gravity = -30f;
        [Min(0.2f)] [Tooltip("Altezza del salto in metri.")] [SerializeField] private float jumpHeight = 1.0f;

        private CharacterController controller;
        private ICharacterVisual visual;
        private Vector3 planarVelocity;
        // Velocità espressa rispetto all'elfo (x = di lato, y = avanti): così quando giri lo sguardo
        // il movimento gira con te invece di continuare nella vecchia direzione.
        private Vector2 localVelocity;
        private float verticalVelocity;

        /// <summary>Moltiplicatore della velocità (miglioramenti del negozio, bufera di neve...).</summary>
        public float SpeedMultiplier { get; set; } = 1f;

        /// <summary>Quando è vero l'elfo resta fermo (es. mentre costruisce).</summary>
        public bool MovementLocked { get; set; }

        public float CurrentSpeed => planarVelocity.magnitude;

        /// <summary>Velocità orizzontale attuale (metri al secondo, nel mondo).</summary>
        public Vector3 PlanarVelocity => planarVelocity;

        /// <summary>Vero mentre si sta scattando (Maiusc premuto e ci si sta muovendo in avanti).</summary>
        public bool IsSprinting { get; private set; }

        /// <summary>Vero se i piedi toccano terra.</summary>
        public bool IsGrounded { get; private set; }

        /// <summary>Velocità massima di camminata, usata per normalizzare le animazioni.</summary>
        public float WalkSpeed => maxSpeed;

        private void Awake()
        {
            controller = GetComponent<CharacterController>();
            visual = GetComponentInChildren<ICharacterVisual>();
            if (input == null) Debug.LogError("[Elfshift] PlayerController: manca InputReader.", this);
        }

        private void Update()
        {
            if (input == null) return;
            float dt = Time.deltaTime;

            Vector2 raw = MovementLocked ? Vector2.zero : input.Move;
            Vector3 forward = Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized;
            if (forward.sqrMagnitude < 0.001f) forward = Vector3.forward;
            Vector3 right = Vector3.Cross(Vector3.up, forward);

            // Si scatta solo andando avanti (non di lato o indietro) e non mentre si costruisce.
            IsSprinting = input.SprintHeld && !MovementLocked && raw.y > 0.3f;
            float speedLimit = maxSpeed * Mathf.Max(0f, SpeedMultiplier) * (IsSprinting ? sprintMultiplier : 1f);
            Vector2 desired = Vector2.ClampMagnitude(raw, 1f) * speedLimit;

            bool grounded = controller.isGrounded;
            IsGrounded = grounded;

            // A terra: se c'è input si accelera verso la velocità voluta, altrimenti si frena forte.
            // Se si inverte la direzione si usa la frenata (più forte) così il cambio è immediato.
            bool hasInput = desired.sqrMagnitude > 0.0001f;
            bool reversing = hasInput && Vector2.Dot(desired, localVelocity) < 0f;
            float rate = hasInput && !reversing ? acceleration : deceleration;
            if (!grounded) rate *= airControl;
            localVelocity = Vector2.MoveTowards(localVelocity, desired, rate * dt);
            planarVelocity = right * localVelocity.x + forward * localVelocity.y;
            if (grounded && verticalVelocity < 0f) verticalVelocity = -2f;
            if (grounded && input.JumpPressed) verticalVelocity = Mathf.Sqrt(-2f * gravity * jumpHeight);
            verticalVelocity += gravity * dt;

            Vector3 motion = planarVelocity + Vector3.up * verticalVelocity;
            controller.Move(motion * dt);

            if (visual != null)
                visual.SetMoveAmount(maxSpeed > 0f ? Mathf.Clamp01(planarVelocity.magnitude / (maxSpeed * sprintMultiplier)) : 0f);
        }

        /// <summary>Sposta l'elfo (es. a inizio giornata) senza che il CharacterController lo riporti indietro.</summary>
        public void Teleport(Vector3 position, Quaternion rotation)
        {
            controller.enabled = false;
            transform.SetPositionAndRotation(position, rotation);
            controller.enabled = true;
            planarVelocity = Vector3.zero;
            localVelocity = Vector2.zero;
            verticalVelocity = 0f;
        }
    }
}
