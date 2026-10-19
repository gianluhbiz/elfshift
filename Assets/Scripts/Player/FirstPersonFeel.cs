using UnityEngine;

namespace Elfshift.Player
{
    /// <summary>
    /// "Sensazione" della prima persona: oscillazione dei passi, campo visivo che si allarga durante lo scatto,
    /// inclinazione laterale quando ci si sposta di lato e piccolo contraccolpo all'atterraggio.
    /// Va messo sulla Telecamera (figlia dell'elfo). Non cambia il gameplay: è solo resa.
    /// </summary>
    public sealed class FirstPersonFeel : MonoBehaviour
    {
        [SerializeField] private PlayerController player;
        [SerializeField] private FirstPersonLook look;
        [SerializeField] private Camera cam;
        [Tooltip("Mani e braccia: si muovono un po' in controtempo rispetto alla telecamera.")]
        [SerializeField] private Transform hands;

        [Header("Campo visivo")]
        [Min(0f)] [SerializeField] private float sprintFovBoost = 10f;
        [Min(0.1f)] [SerializeField] private float fovSpeed = 7f;

        [Header("Passi")]
        [Min(0f)] [SerializeField] private float bobHeight = 0.045f;
        [Min(0f)] [SerializeField] private float bobSide = 0.03f;
        [Min(0f)] [SerializeField] private float rollDegrees = 2.2f;

        private Vector3 baseCameraPosition;
        private Vector3 baseHandsPosition;
        private float baseFov;
        private float phase;
        private float bobAmount;
        private float roll;
        private float landDip;
        private float landVelocity;
        private bool wasGrounded = true;

        private void Awake()
        {
            if (cam == null) cam = GetComponent<Camera>();
            baseCameraPosition = transform.localPosition;
            if (hands != null) baseHandsPosition = hands.localPosition;
            if (cam != null) baseFov = cam.fieldOfView;
        }

        private void LateUpdate()
        {
            if (player == null) return;
            float dt = Time.deltaTime;

            Vector3 velocity = player.PlanarVelocity;
            float speed01 = player.WalkSpeed > 0f ? Mathf.Clamp01(velocity.magnitude / (player.WalkSpeed * 1.7f)) : 0f;
            bool moving = player.IsGrounded && velocity.magnitude > 0.3f;
            bobAmount = Mathf.MoveTowards(bobAmount, moving ? 1f : 0f, dt * 6f);
            float sprintFactor = player.IsSprinting ? 1.35f : 1f;
            phase += dt * Mathf.Lerp(6f, 12f, speed01) * (moving ? 1f : 0f) * sprintFactor;

            float bobY = Mathf.Abs(Mathf.Sin(phase)) * bobHeight * bobAmount * sprintFactor;
            float bobX = Mathf.Sin(phase) * bobSide * bobAmount * sprintFactor;

            // Atterraggio: la telecamera si abbassa di scatto e torna su con una molla.
            if (!wasGrounded && player.IsGrounded) landVelocity = -2.4f;
            wasGrounded = player.IsGrounded;
            landVelocity += (-landDip * 90f - landVelocity * 12f) * dt;
            landDip += landVelocity * dt;

            transform.localPosition = baseCameraPosition + new Vector3(bobX, bobY + landDip * 0.12f, 0f);
            if (hands != null)
                hands.localPosition = baseHandsPosition + new Vector3(-bobX * 0.8f, -bobY * 1.4f + landDip * 0.1f, 0f);

            // Inclinazione laterale: ci si "appoggia" nella direzione del movimento di lato.
            float lateral = Vector3.Dot(velocity, player.transform.right);
            float targetRoll = -Mathf.Clamp(lateral / Mathf.Max(0.1f, player.WalkSpeed), -1.5f, 1.5f) * rollDegrees;
            roll = Mathf.Lerp(roll, targetRoll, 1f - Mathf.Exp(-10f * dt));
            if (look != null) look.Roll = roll;

            if (cam != null)
            {
                float targetFov = baseFov + (player.IsSprinting && moving ? sprintFovBoost : 0f);
                cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, targetFov, 1f - Mathf.Exp(-fovSpeed * dt));
            }
        }
    }
}
