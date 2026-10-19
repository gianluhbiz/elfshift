using UnityEngine;

namespace Elfshift.Player
{
    /// <summary>
    /// Animazione "da codice" del modello provvisorio dell'elfo: camminata, respiro,
    /// trasporto (braccia davanti), lavoro (martellate) e piccolo sobbalzo alla raccolta.
    /// Non servono clip di animazione: muove direttamente braccia, gambe, corpo e testa.
    /// </summary>
    public sealed class ElfProceduralAnimator : MonoBehaviour, ICharacterVisual
    {
        [Header("Parti del corpo (assegnate automaticamente dal costruttore di scena)")]
        [SerializeField] private Transform body;
        [SerializeField] private Transform head;
        [SerializeField] private Transform armLeft;
        [SerializeField] private Transform armRight;
        [SerializeField] private Transform legLeft;
        [SerializeField] private Transform legRight;
        [SerializeField] private Transform hatTip;

        [Header("Parametri")]
        [SerializeField] private float walkFrequency = 10f;
        [SerializeField] private float legSwingDegrees = 38f;
        [SerializeField] private float armSwingDegrees = 32f;

        private float moveTarget;
        private float moveSmooth;
        private float carrySmooth;
        private float workSmooth;
        private bool carrying;
        private bool working;
        private float walkPhase;
        private float pickupPulse;
        private Vector3 bodyBasePos;
        private Quaternion hatBaseRot = Quaternion.identity;

        public void SetMoveAmount(float normalized) { moveTarget = Mathf.Clamp01(normalized); }
        public void SetCarrying(bool value) { carrying = value; }
        public void SetWorking(bool value) { working = value; }
        public void PlayPickup() { pickupPulse = 1f; }

        private void Awake()
        {
            if (body != null) bodyBasePos = body.localPosition;
            if (hatTip != null) hatBaseRot = hatTip.localRotation;
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            moveSmooth = Mathf.MoveTowards(moveSmooth, moveTarget, dt * 6f);
            carrySmooth = Mathf.MoveTowards(carrySmooth, carrying ? 1f : 0f, dt * 7f);
            workSmooth = Mathf.MoveTowards(workSmooth, working ? 1f : 0f, dt * 8f);
            pickupPulse = Mathf.MoveTowards(pickupPulse, 0f, dt * 4f);
            walkPhase += dt * walkFrequency * Mathf.Lerp(0.35f, 1f, moveSmooth) * (moveSmooth > 0.02f ? 1f : 0f);

            float swing = Mathf.Sin(walkPhase) * moveSmooth;
            float t = Time.time;

            // Gambe: oscillano in controfase.
            if (legLeft != null) legLeft.localRotation = Quaternion.Euler(-swing * legSwingDegrees, 0f, 0f);
            if (legRight != null) legRight.localRotation = Quaternion.Euler(swing * legSwingDegrees, 0f, 0f);

            // Braccia: oscillano camminando; portando un oggetto vanno avanti; lavorando martellano.
            float hammer = Mathf.Sin(t * 16f) * 28f;
            float armLeftX = -swing * armSwingDegrees * (1f - carrySmooth);
            float armRightX = swing * armSwingDegrees * (1f - carrySmooth);
            armLeftX = Mathf.Lerp(armLeftX, -78f, carrySmooth);
            armRightX = Mathf.Lerp(armRightX, -78f, carrySmooth);
            armLeftX = Mathf.Lerp(armLeftX, -70f + hammer, workSmooth);
            armRightX = Mathf.Lerp(armRightX, -70f - hammer, workSmooth);
            float armInward = carrySmooth * 14f;
            if (armLeft != null) armLeft.localRotation = Quaternion.Euler(armLeftX, 0f, armInward);
            if (armRight != null) armRight.localRotation = Quaternion.Euler(armRightX, 0f, -armInward);

            // Corpo: rimbalzo del passo, respiro da fermo, inclinazione in avanti, "pop" alla raccolta.
            if (body != null)
            {
                float bounce = Mathf.Abs(Mathf.Sin(walkPhase)) * 0.06f * moveSmooth;
                float breathe = Mathf.Sin(t * 2.2f) * 0.012f * (1f - moveSmooth);
                body.localPosition = bodyBasePos + Vector3.up * (bounce + breathe + pickupPulse * 0.04f);
                float lean = moveSmooth * 7f + workSmooth * 10f;
                body.localRotation = Quaternion.Euler(lean, 0f, Mathf.Sin(walkPhase) * 3f * moveSmooth);
                float squash = 1f + pickupPulse * 0.06f;
                body.localScale = new Vector3(1f / Mathf.Sqrt(squash), squash, 1f / Mathf.Sqrt(squash));
            }

            // Testa: leggero dondolio.
            if (head != null)
                head.localRotation = Quaternion.Euler(Mathf.Sin(t * 1.7f) * 1.5f - moveSmooth * 4f, Mathf.Sin(t * 0.9f) * 4f * (1f - moveSmooth), Mathf.Sin(walkPhase) * 2.5f * moveSmooth);

            // Punta del cappello: ondeggia come se fosse morbida.
            if (hatTip != null)
                hatTip.localRotation = hatBaseRot * Quaternion.Euler(Mathf.Sin(t * 5f - walkPhase) * (4f + 10f * moveSmooth), 0f, Mathf.Sin(t * 3.1f) * 6f * moveSmooth);
        }
    }
}
