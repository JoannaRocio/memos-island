using UnityEngine;
using UnityEngine.InputSystem;

namespace MemosIsland.Core
{
    /// <summary>
    /// Controles del juego, estilo GBA:
    /// cruz = flechas/WASD/stick · A = Z/Enter/Espacio · B = X/Shift (correr) · Start = Esc/Tab.
    /// </summary>
    public static class GameInput
    {
        static InputAction _move, _confirm, _cancel, _menu;

        static void Init()
        {
            if (_move != null) return;

            _move = new InputAction("Move", InputActionType.Value, expectedControlType: "Vector2");
            _move.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/upArrow").With("Down", "<Keyboard>/downArrow")
                .With("Left", "<Keyboard>/leftArrow").With("Right", "<Keyboard>/rightArrow");
            _move.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/w").With("Down", "<Keyboard>/s")
                .With("Left", "<Keyboard>/a").With("Right", "<Keyboard>/d");
            _move.AddBinding("<Gamepad>/dpad");
            _move.AddBinding("<Gamepad>/leftStick");

            _confirm = new InputAction("Confirm", InputActionType.Button);
            _confirm.AddBinding("<Keyboard>/z");
            _confirm.AddBinding("<Keyboard>/enter");
            _confirm.AddBinding("<Keyboard>/space");
            _confirm.AddBinding("<Gamepad>/buttonSouth");

            _cancel = new InputAction("Cancel", InputActionType.Button);
            _cancel.AddBinding("<Keyboard>/x");
            _cancel.AddBinding("<Keyboard>/leftShift");
            _cancel.AddBinding("<Keyboard>/rightShift");
            _cancel.AddBinding("<Gamepad>/buttonEast");

            _menu = new InputAction("Menu", InputActionType.Button);
            _menu.AddBinding("<Keyboard>/escape");
            _menu.AddBinding("<Keyboard>/tab");
            _menu.AddBinding("<Gamepad>/start");

            _move.Enable();
            _confirm.Enable();
            _cancel.Enable();
            _menu.Enable();
        }

        public static Vector2 Move { get { Init(); return DebugMove != Vector2.zero ? DebugMove : _move.ReadValue<Vector2>(); } }

        /// <summary>Solo para pruebas automáticas: dirección "mantenida".</summary>
        public static Vector2 DebugMove;
        public static bool ConfirmPressed { get { Init(); return _confirm.WasPressedThisFrame() || ConsumeDebugPress(); } }

        /// <summary>Solo para pruebas automáticas: cada unidad simula un toque de A en un cuadro distinto.</summary>
        public static int DebugPresses;
        static int _debugFrame = -1;

        static bool ConsumeDebugPress()
        {
            if (DebugPresses <= 0 || _debugFrame == UnityEngine.Time.frameCount) return false;
            if (_debugFrame == UnityEngine.Time.frameCount - 1) return false; // un cuadro de por medio, como un toque real
            _debugFrame = UnityEngine.Time.frameCount;
            DebugPresses--;
            return true;
        }
        public static bool CancelPressed { get { Init(); return _cancel.WasPressedThisFrame(); } }
        public static bool CancelHeld { get { Init(); return _cancel.IsPressed(); } }
        public static bool MenuPressed { get { Init(); return _menu.WasPressedThisFrame(); } }
    }
}
