using Elfshift.Core;
using UnityEngine;

namespace Elfshift.Player
{
    /// <summary>
    /// Movimento dell'elfo: accelerazione e decelerazione morbide, rotazione verso la direzione
    /// di marcia, collisioni con i mobili tramite CharacterController (nessuna compenetrazione).
    /// Il movimento è relativo alla telecamera, quindi funziona anche se la vista viene ruotata.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public sealed class PlayerController : MonoBehaviour
    {
        [SerializeField] private InputReader input;
        [Tooltip("Telecamera di riferimento per la direzione di movimento. Se vuota si usa il mondo.")]
        [SerializeField] private Transform cameraTransform;

        [Header("Movimento")]
        [Min(0.5f)] [SerializeField] private float maxSpeed = 4.6f;
        [Min(1f)] [SerializeField] private float acceleration = 22f;
        [Min(1f)] [SerializeField] private float deceleration = 30f;
        [Min(90f)] [SerializeField] private float turnSpeedDegrees = 900f;
        [SerializeField] private float gravity = -25f;

        private CharacterController controller;
        private ICharacterVisual visual;
        private Vector3 planarVelocity;
        private float verticalVelocity;

        /// <summary>Moltiplicatore della velocità (miglioramenti del negozio, bufera di neve...).</summary>
        public float SpeedMultiplier { get; set; } = 1f;

        /// <summary>Quando è vero l'elfo resta fermo (es. mentre costruisce).</summary>
        public bool MovementLocked { get; set; }

        public float CurrentSpeed => planarVelocity.magnitude;

        private void Awake()
        {
            controller = GetComponent<CharacterController>();
            visual = GetComponentInChildren<ICharacterVisual>();
            if (input == null) Debug.LogError("[Elfshift] PlayerController: manca InputReader.", this);
            if (cameraTransform == null && Camera.main != null) cameraTransform = Camera.main.transform;
        }

        private void Update()
        {
            if (input == null) return;
            float dt = Time.deltaTime;

            Vector2 raw = MovementLocked ? Vector2.zero : input.Move;
            Vector3 forward = Vector3.forward;
            Vector3 right = Vector3.right;
            if (cameraTransform != null)
            {
                forward = Vector3.ProjectOnPlane(cameraTransform.forward, Vector3.up).normalized;
                if (forward.sqrMagnitude < 0.001f) forward = Vector3.forward;
                right = Vector3.Cross(Vector3.up, forward);
            }

            float speedLimit = maxSpeed * Mathf.Max(0f, SpeedMultiplier);
            Vector3 desired = (right * raw.x + forward * raw.y) * speedLimit;
            float rate = desired.sqrMagnitude > 0.001f ? acceleration : deceleration;
            planarVelocity = Vector3.MoveTowards(planarVelocity, desired, rate * dt);

            if (controller.isGrounded && verticalVelocity < 0f) verticalVelocity = -2f;
            verticalVelocity += gravity * dt;

            Vector3 motion = planarVelocity + Vector3.up * verticalVelocity;
            controller.Move(motion * dt);

            if (planarVelocity.sqrMagnitude > 0.04f)
            {
                Quaternion target = Quaternion.LookRotation(planarVelocity.normalized, Vector3.up);
                transform.rotation = Quaternion.RotateTowards(transform.rotation, target, turnSpeedDegrees * dt);
            }

            if (visual != null)
                visual.SetMoveAmount(maxSpeed > 0f ? Mathf.Clamp01(planarVelocity.magnitude / maxSpeed) : 0f);
        }

        /// <summary>Sposta l'elfo (es. a inizio giornata) senza che il CharacterController lo riporti indietro.</summary>
        public void Teleport(Vector3 position, Quaternion rotation)
        {
            controller.enabled = false;
            transform.SetPositionAndRotation(position, rotation);
            controller.enabled = true;
            planarVelocity = Vector3.zero;
            verticalVelocity = 0f;
        }
    }
}
