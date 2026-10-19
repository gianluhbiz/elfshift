using System;
using Elfshift.Core;
using Elfshift.Interaction;
using Elfshift.Production;
using UnityEngine;

namespace Elfshift.Player
{
    /// <summary>
    /// Cuore dell'interazione: ogni frame cerca, tra gli oggetti INTERAGIBILI VICINI, quello
    /// giusto per l'azione possibile (prendi / posa / consegna / costruisci) e reagisce ai tasti.
    ///
    ///   Spazio ... mani libere: prende dal più vicino   |  mani piene: posa/consegna, altrimenti a terra
    ///   E (tenuto) ... interazione a durata (costruzione)
    ///
    /// Tutto dipende dalla distanza e da ciò che l'elfo tiene in mano: non si può completare
    /// nulla da lontano.
    /// </summary>
    public sealed class PlayerInteractor : MonoBehaviour
    {
        [SerializeField] private InputReader input;
        [SerializeField] private Carrier carrier;
        [SerializeField] private PlayerController movement;
        [Tooltip("Distanza davanti all'elfo a cui viene posato un oggetto a terra.")]
        [SerializeField] private float dropDistance = 0.95f;
        [SerializeField] private float floorHeight = 0f;

        private ICharacterVisual visual;
        private IHoldInteractable activeHold;
        private float holdTimer;
        private bool waitForRelease;
        private string lastPickupPrompt;
        private string lastInteractPrompt;
        private float lastProgress = -1f;

        /// <summary>(testo tasto Spazio, testo tasto E). Stringa vuota/null = nessuna azione.</summary>
        public event Action<string, string> PromptChanged;
        /// <summary>Avanzamento dell'interazione a durata, 0..1; -1 = nascosta.</summary>
        public event Action<float> HoldProgressChanged;
        /// <summary>Messaggio breve per il giocatore (es. "Questo non serve qui").</summary>
        public event Action<string> MessageRaised;

        private void Awake()
        {
            visual = GetComponentInChildren<ICharacterVisual>();
            if (input == null) Debug.LogError("[Elfshift] PlayerInteractor: manca InputReader.", this);
            if (carrier == null) Debug.LogError("[Elfshift] PlayerInteractor: manca Carrier.", this);
        }

        private void OnEnable()
        {
            if (carrier != null) carrier.HeldChanged += OnHeldChanged;
        }

        private void OnDisable()
        {
            if (carrier != null) carrier.HeldChanged -= OnHeldChanged;
            CancelHold();
        }

        private void OnHeldChanged(WorldItem item)
        {
            if (visual != null) visual.SetCarrying(item != null);
        }

        private void Update()
        {
            if (input == null || carrier == null) return;

            // ---- 1) Cerca i candidati vicini (nessuna allocazione) -------------------------
            Vector3 myPos = transform.position;
            bool hasItem = carrier.HasItem;
            ItemDefinition heldDefinition = hasItem ? carrier.Held.Definition : null;

            IItemSource bestSource = null;
            float bestSourceSq = float.MaxValue;
            IItemReceiver acceptor = null;
            float acceptorSq = float.MaxValue;
            IItemReceiver rejector = null;
            float rejectorSq = float.MaxValue;
            string rejectReason = null;
            IHoldInteractable bestHold = null;
            float bestHoldSq = float.MaxValue;

            var all = InteractableRegistry.All;
            for (int i = 0; i < all.Count; i++)
            {
                Interactable candidate = all[i];
                if (candidate == null || !candidate.isActiveAndEnabled) continue;

                Vector3 delta = candidate.Position - myPos;
                delta.y = 0f;
                float sq = delta.sqrMagnitude;
                float reach = candidate.Radius;
                if (sq > reach * reach) continue;

                if (!hasItem)
                {
                    IItemSource source = candidate as IItemSource;
                    if (source != null && source.CanGive && sq < bestSourceSq)
                    {
                        bestSource = source;
                        bestSourceSq = sq;
                    }
                }
                else
                {
                    IItemReceiver receiver = candidate as IItemReceiver;
                    if (receiver != null)
                    {
                        string reason;
                        if (receiver.Accepts(heldDefinition, out reason))
                        {
                            if (sq < acceptorSq) { acceptor = receiver; acceptorSq = sq; }
                        }
                        else if (sq < rejectorSq)
                        {
                            rejector = receiver;
                            rejectorSq = sq;
                            rejectReason = reason;
                        }
                    }
                }

                IHoldInteractable hold = candidate as IHoldInteractable;
                if (hold != null && hold.CanHold && sq < bestHoldSq)
                {
                    bestHold = hold;
                    bestHoldSq = sq;
                }
            }

            // ---- 2) Suggerimenti a schermo --------------------------------------------------
            string pickupPrompt = null;
            if (!hasItem) pickupPrompt = bestSource != null ? bestSource.PickupPrompt : null;
            else pickupPrompt = acceptor != null ? acceptor.PlacePrompt : UiText.Drop;
            string interactPrompt = bestHold != null ? bestHold.HoldPrompt : null;
            PublishPrompts(pickupPrompt, interactPrompt);

            // ---- 3) Tasto Spazio: prendi / posa --------------------------------------------
            if (input.PickupPressed && activeHold == null)
            {
                if (!hasItem)
                {
                    if (bestSource != null)
                    {
                        WorldItem taken = bestSource.Take();
                        if (taken != null)
                        {
                            carrier.Hold(taken);
                            if (visual != null) visual.PlayPickup();
                        }
                    }
                }
                else if (acceptor != null)
                {
                    WorldItem item = carrier.Release();
                    acceptor.Receive(item);
                    if (visual != null) visual.PlayPickup();
                }
                else if (rejector != null)
                {
                    MessageRaised?.Invoke(rejectReason);
                }
                else
                {
                    DropOnFloor();
                }
            }

            // ---- 4) Tasto E tenuto premuto: interazione a durata ---------------------------
            UpdateHold(bestHold);
        }

        private void UpdateHold(IHoldInteractable candidate)
        {
            if (!input.InteractHeld) waitForRelease = false;

            bool wantsHold = input.InteractHeld && !waitForRelease && candidate != null;
            if (!wantsHold)
            {
                CancelHold();
                return;
            }

            if (!ReferenceEquals(activeHold, candidate))
            {
                CancelHold();
                activeHold = candidate;
                holdTimer = 0f;
                if (movement != null) movement.MovementLocked = true;
                if (visual != null) visual.SetWorking(true);
            }

            holdTimer += Time.deltaTime;
            float duration = Mathf.Max(0.05f, activeHold.HoldDuration);
            float progress = Mathf.Clamp01(holdTimer / duration);
            activeHold.HoldProgress(progress);
            PublishProgress(progress);

            if (holdTimer >= duration)
            {
                IHoldInteractable finished = activeHold;
                activeHold = null;
                holdTimer = 0f;
                waitForRelease = true;
                if (movement != null) movement.MovementLocked = false;
                if (visual != null) visual.SetWorking(false);
                PublishProgress(-1f);
                finished.HoldCompleted();
            }
        }

        private void CancelHold()
        {
            if (activeHold == null) return;
            activeHold.HoldCancelled();
            activeHold = null;
            holdTimer = 0f;
            if (movement != null) movement.MovementLocked = false;
            if (visual != null) visual.SetWorking(false);
            PublishProgress(-1f);
        }

        private void DropOnFloor()
        {
            Vector3 target = transform.position + transform.forward * dropDistance;
            // Se davanti c'è un mobile o un muro, posa l'oggetto ai piedi dell'elfo.
            Vector3 probe = new Vector3(target.x, floorHeight + 0.4f, target.z);
            if (Physics.CheckSphere(probe, 0.2f, ~0, QueryTriggerInteraction.Ignore))
                target = transform.position + transform.forward * 0.05f;
            target.y = floorHeight;

            WorldItem item = carrier.Release();
            if (item == null) return;
            float yaw = transform.eulerAngles.y + UnityEngine.Random.Range(-25f, 25f);
            item.PlaceOnFloor(target, yaw);
            if (visual != null) visual.PlayPickup();
        }

        private void PublishPrompts(string pickup, string interact)
        {
            if (string.Equals(pickup, lastPickupPrompt) && string.Equals(interact, lastInteractPrompt)) return;
            lastPickupPrompt = pickup;
            lastInteractPrompt = interact;
            PromptChanged?.Invoke(pickup, interact);
        }

        private void PublishProgress(float value)
        {
            if (Mathf.Approximately(value, lastProgress)) return;
            lastProgress = value;
            HoldProgressChanged?.Invoke(value);
        }
    }
}
