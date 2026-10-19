using UnityEngine;

namespace Elfshift.Player
{
    /// <summary>
    /// Telecamera dall'alto, inclinata, che segue il giocatore in modo morbido.
    /// La rotazione è fissa (nessun movimento brusco); la posizione è limitata a un'area
    /// così il laboratorio resta sempre ben inquadrato.
    /// </summary>
    public sealed class TopDownCamera : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [Range(20f, 80f)] [SerializeField] private float pitchDegrees = 52f;
        [SerializeField] private float yawDegrees = 0f;
        [Min(3f)] [SerializeField] private float distance = 15.5f;
        [Min(0.01f)] [SerializeField] private float smoothTime = 0.28f;
        [Tooltip("Sposta il punto guardato rispetto all'elfo (metri).")]
        [SerializeField] private Vector3 focusOffset = new Vector3(0f, 0.8f, 1.4f);

        [Header("Limiti del punto guardato (x = ovest/est, y = sud/nord)")]
        [SerializeField] private Vector2 minFocus = new Vector2(-2f, -2.5f);
        [SerializeField] private Vector2 maxFocus = new Vector2(5.5f, 1.5f);

        private Vector3 velocity;

        private void OnEnable()
        {
            if (target != null) SnapToTarget();
        }

        private void LateUpdate()
        {
            if (target == null) return;
            Vector3 desired = ComputePosition();
            transform.position = Vector3.SmoothDamp(transform.position, desired, ref velocity, smoothTime);
            transform.rotation = Quaternion.Euler(pitchDegrees, yawDegrees, 0f);
        }

        /// <summary>Posiziona subito la telecamera (senza animazione).</summary>
        public void SnapToTarget()
        {
            transform.position = ComputePosition();
            transform.rotation = Quaternion.Euler(pitchDegrees, yawDegrees, 0f);
            velocity = Vector3.zero;
        }

        private Vector3 ComputePosition()
        {
            Vector3 focus = target.position + focusOffset;
            focus.x = Mathf.Clamp(focus.x, minFocus.x, maxFocus.x);
            focus.z = Mathf.Clamp(focus.z, minFocus.y, maxFocus.y);
            Quaternion rotation = Quaternion.Euler(pitchDegrees, yawDegrees, 0f);
            return focus - rotation * Vector3.forward * distance;
        }
    }
}
