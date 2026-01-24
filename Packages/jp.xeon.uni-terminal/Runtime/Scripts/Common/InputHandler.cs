#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Xeon.UniTerminal.Common
{
    public static class InputHandler
    {
        public static bool IsPressedUpArrow()
        {
            var result = false;
#if ENABLE_INPUT_SYSTEM
            result = Keyboard.current != null && Keyboard.current.upArrowKey.wasPressedThisFrame;
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
            result |= Input.GetKeyDown(KeyCode.UpArrow);
#endif
            return result;
        }

        public static bool IsPressedDownArrow()
        {
            var result = false;
#if ENABLE_INPUT_SYSTEM
            result = Keyboard.current != null && Keyboard.current.downArrowKey.wasPressedThisFrame;
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
            result |= Input.GetKeyDown(KeyCode.DownArrow);
#endif
            return result;
        }

        public static bool IsPressedReturn()
        {
            var result = false;
#if ENABLE_INPUT_SYSTEM
            result = Keyboard.current != null && Keyboard.current.enterKey.wasPressedThisFrame;
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
            result |= Input.GetKeyDown(KeyCode.Return);
#endif
            return result;
        }

        public static bool IsPressedTab()
        {
            var result = false;
#if ENABLE_INPUT_SYSTEM
            result = Keyboard.current != null && Keyboard.current.tabKey.wasPressedThisFrame;
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
            result |= Input.GetKeyDown(KeyCode.Tab);
#endif
            return result;
        }

        public static bool IsPressedY()
        {
            var result = false;
#if ENABLE_INPUT_SYSTEM
            result = Keyboard.current != null && Keyboard.current.yKey.wasPressedThisFrame;
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
            result |= Input.GetKeyDown(KeyCode.Y);
#endif
            return result;
        }

        public static bool IsPressedN()
        {
            var result = false;
#if ENABLE_INPUT_SYSTEM
            result = Keyboard.current != null && Keyboard.current.nKey.wasPressedThisFrame;
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
            result |= Input.GetKeyDown(KeyCode.N);
#endif
            return result;
        }
    }
}