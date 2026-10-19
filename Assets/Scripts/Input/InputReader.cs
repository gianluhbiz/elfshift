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
    ///   Sguardo ....... mouse / stick destro
    ///   Raccogli/posa . tasto sinistro del mouse / grilletto destro
    ///   Salto ......... Spazio / pulsante A (Sud)
    ///   Interagisci ... E / tasto destro del mouse / pulsante X (Ovest) - da tenere premuto
    ///   Pausa ......... Esc / Start
    /// </summary>
    public sealed class InputReader : MonoBehaviour, IInputProvider
    {
        [Header("Sguardo (prima persona)")]
        [Min(0.01f)] [SerializeField] private float mouseDegreesPerPixel = 0.09f;
        [Min(10f)] [SerializeField] private float stickDegreesPerSecond = 150f;

        private InputAction lookMouseAction;
        private InputAction lookStickAction;
        private InputAction moveAction;
        private InputAction pickupAction;
        private InputAction jumpAction;
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

        public Vector2 LookDelta
        {
            get
            {
                Vector2 mouse = lookMouseAction.ReadValue<Vector2>() * mouseDegreesPerPixel;
                Vector2 stick = lookStickAction.ReadValue<Vector2>() * (stickDegreesPerSecond * Time.deltaTime);
                return mouse + stick;
            }
        }

        public bool PickupPressed => pickupAction.WasPressedThisFrame();
        public bool JumpPressed => jumpAction.WasPressedThisFrame();
        public bool InteractHeld => interactAction.IsPressed();
        public bool PausePressed => pauseAction.WasPressedThisFrame();

        private void Awake()
        {
            moveAction = new InputAction("Move", InputActionType.Value, expectedControlType: "Vector2");
            // Composito predefinito = digitale normalizzato: in diagonale non si va più veloci.
            moveAction.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/w")
                .With("Down", "<Keyboard>/s")
                .With("Left", "<Keyboard>/a")
                .With("Right", "<Keyboard>/d");
            moveAction.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/upArrow")
                .With("Down", "<Keyboard>/downArrow")
                .With("Left", "<Keyboard>/leftArrow")
                .With("Right", "<Keyboard>/rightArrow");
            moveAction.AddBinding("<Gamepad>/leftStick");

            lookMouseAction = new InputAction("LookMouse", InputActionType.Value, "<Mouse>/delta", null, null, "Vector2");
            lookStickAction = new InputAction("LookStick", InputActionType.Value, "<Gamepad>/rightStick", null, null, "Vector2");

            pickupAction = new InputAction("Pickup", InputActionType.Button);
            pickupAction.AddBinding("<Mouse>/leftButton");
            pickupAction.AddBinding("<Gamepad>/rightTrigger");

            jumpAction = new InputAction("Jump", InputActionType.Button);
            jumpAction.AddBinding("<Keyboard>/space");
            jumpAction.AddBinding("<Gamepad>/buttonSouth");

            interactAction = new InputAction("Interact", InputActionType.Button);
            interactAction.AddBinding("<Keyboard>/e");
            interactAction.AddBinding("<Mouse>/rightButton");
            interactAction.AddBinding("<Gamepad>/buttonWest");

            pauseAction = new InputAction("Pause", InputActionType.Button);
            pauseAction.AddBinding("<Keyboard>/escape");
            pauseAction.AddBinding("<Gamepad>/start");
        }

        private void OnEnable()
        {
            moveAction.Enable();
            lookMouseAction.Enable();
            lookStickAction.Enable();
            pickupAction.Enable();
            jumpAction.Enable();
            interactAction.Enable();
            pauseAction.Enable();
        }

        private void OnDisable()
        {
            moveAction.Disable();
            lookMouseAction.Disable();
            lookStickAction.Disable();
            pickupAction.Disable();
            jumpAction.Disable();
            interactAction.Disable();
            pauseAction.Disable();
        }

        private void OnDestroy()
        {
            moveAction?.Dispose();
            lookMouseAction?.Dispose();
            lookStickAction?.Dispose();
            pickupAction?.Dispose();
            jumpAction?.Dispose();
            interactAction?.Dispose();
            pauseAction?.Dispose();
        }
    }
}
