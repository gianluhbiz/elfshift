using UnityEngine;
using UnityEngine.InputSystem;

namespace Elfshift.Core
{
    /// <summary>
    /// Punto UNICO in cui sono definiti i controlli. Per cambiare un tasto si modifica
    /// solo questo file. Usa il "nuovo" Input System di Unity; le azioni sono create da codice,
    /// quindi non serve nessun asset da configurare.
    ///
    /// Controlli:
    ///   Movimento ..... WASD / frecce / stick sinistro
    ///   Raccogli/posa . Spazio / pulsante A (Sud)
    ///   Interagisci ... E / pulsante X (Ovest) - da tenere premuto
    ///   Pausa ......... Esc / Start
    /// </summary>
    public sealed class InputReader : MonoBehaviour, IInputProvider
    {
        private InputAction moveAction;
        private InputAction pickupAction;
        private InputAction interactAction;
        private InputAction pauseAction;

        public Vector2 Move
        {
            get
            {
                Vector2 v = moveAction.ReadValue<Vector2>();
                return Vector2.ClampMagnitude(v, 1f);
            }
        }

        public bool PickupPressed => pickupAction.WasPressedThisFrame();
        public bool InteractHeld => interactAction.IsPressed();
        public bool PausePressed => pauseAction.WasPressedThisFrame();

        private void Awake()
        {
            moveAction = new InputAction("Move", InputActionType.Value, expectedControlType: "Vector2");
            // "mode=2" = digitale normalizzato: la diagonale non è più veloce.
            moveAction.AddCompositeBinding("2DVector(mode=2)")
                .With("Up", "<Keyboard>/w")
                .With("Down", "<Keyboard>/s")
                .With("Left", "<Keyboard>/a")
                .With("Right", "<Keyboard>/d");
            moveAction.AddCompositeBinding("2DVector(mode=2)")
                .With("Up", "<Keyboard>/upArrow")
                .With("Down", "<Keyboard>/downArrow")
                .With("Left", "<Keyboard>/leftArrow")
                .With("Right", "<Keyboard>/rightArrow");
            moveAction.AddBinding("<Gamepad>/leftStick");

            pickupAction = new InputAction("Pickup", InputActionType.Button);
            pickupAction.AddBinding("<Keyboard>/space");
            pickupAction.AddBinding("<Gamepad>/buttonSouth");

            interactAction = new InputAction("Interact", InputActionType.Button);
            interactAction.AddBinding("<Keyboard>/e");
            interactAction.AddBinding("<Gamepad>/buttonWest");

            pauseAction = new InputAction("Pause", InputActionType.Button);
            pauseAction.AddBinding("<Keyboard>/escape");
            pauseAction.AddBinding("<Gamepad>/start");
        }

        private void OnEnable()
        {
            moveAction.Enable();
            pickupAction.Enable();
            interactAction.Enable();
            pauseAction.Enable();
        }

        private void OnDisable()
        {
            moveAction.Disable();
            pickupAction.Disable();
            interactAction.Disable();
            pauseAction.Disable();
        }

        private void OnDestroy()
        {
            moveAction?.Dispose();
            pickupAction?.Dispose();
            interactAction?.Dispose();
            pauseAction?.Dispose();
        }
    }
}
