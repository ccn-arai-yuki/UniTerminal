#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Xeon.UniTerminal.Common
{
    /// <summary>
    /// 入力システムの差異を吸収するユーティリティ。
    /// </summary>
    public static class InputHandler
    {
        /// <summary>
        /// 上矢印キーが押下されたかを取得します。
        /// </summary>
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

        /// <summary>
        /// 下矢印キーが押下されたかを取得します。
        /// </summary>
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

        /// <summary>
        /// Enterキーが押下されたかを取得します。
        /// </summary>
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

        /// <summary>
        /// Tabキーが押下されたかを取得します。
        /// </summary>
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

        /// <summary>
        /// Yキーが押下されたかを取得します。
        /// </summary>
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

        /// <summary>
        /// Nキーが押下されたかを取得します。
        /// </summary>
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
