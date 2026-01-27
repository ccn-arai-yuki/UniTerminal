#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Xeon.UniTerminal.Common
{
    /// <summary>
    /// 入力システムの差異を吸収するユーティリティ
    /// </summary>
    public static class InputHandler
    {
        /// <summary>
        /// 上矢印キーが押下されたかを取得します
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
        /// 下矢印キーが押下されたかを取得します
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
        /// Enterキーが押下されたかを取得します
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
        /// Tabキーが押下されたかを取得します
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
        /// Yキーが押下されたかを取得します
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
        /// Nキーが押下されたかを取得します
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

        /// <summary>
        /// Ctrlキーが押されているかを判定する。
        /// </summary>
        /// <returns>左右いずれかのCtrlキーが押されている場合true。</returns>
        public static bool IsHeldCtrl()
        {
            var result = false;
#if ENABLE_INPUT_SYSTEM
            var keyboard = Keyboard.current;
            if (keyboard != null)
                result = keyboard.leftCtrlKey.isPressed || keyboard.rightCtrlKey.isPressed;
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
            result |= UnityEngine.Input.GetKey(UnityEngine.KeyCode.LeftControl) ||
                      UnityEngine.Input.GetKey(UnityEngine.KeyCode.RightControl);
#endif
            return result;
        }

        /// <summary>
        /// Cキーがこのフレームで押されたかを判定する。
        /// </summary>
        /// <returns>Cキーが押された場合true。</returns>
        public static bool IsPressedC()
        {
            var result = false;
#if ENABLE_INPUT_SYSTEM
            var keyboard = Keyboard.current;
            if (keyboard != null)
                result = keyboard.cKey.wasPressedThisFrame;
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
            result |= UnityEngine.Input.GetKeyDown(UnityEngine.KeyCode.C);
#endif
            return result;
        }

        /// <summary>
        /// Ctrl+Cが押されたかを判定する。
        /// </summary>
        /// <returns>Ctrlを押しながらCを押した場合true。</returns>
        public static bool IsPressedCtrlC()
        {
            return IsHeldCtrl() && IsPressedC();
        }
    }
}
