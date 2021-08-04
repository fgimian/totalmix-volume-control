using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Interop;

namespace TotalMixVC.GUI
{
    /// <summary>
    /// Represents a key modifier for a shortcut.
    /// </summary>
    [Flags]
    public enum KeyModifier
    {
        /// <summary>No key modifier.</summary>
        None = 0x0000,

        /// <summary>The Alt key modifier.</summary>
        Alt = 0x0001,

        /// <summary>The Ctrl key modifier.</summary>
        Ctrl = 0x0002,

        /// <summary>The Shift key modifier.</summary>
        Shift = 0x0004,

        /// <summary>The Win / Windows key modifier.</summary>
        Win = 0x0008
    }

    /// <summary>
    /// Represents a hotkey that may be bound.
    /// </summary>
    /// <param name="KeyModifier">The key modifier for the hotkey.</param>
    /// <param name="Key">The key that must be pressed with the modifier for the hotkey.</param>
    public record Hotkey(KeyModifier KeyModifier, Key Key);

    /// <summary>
    /// Manages various global hotkeys along with their associated actions.
    /// </summary>
    public class GlobalHotKeyManager : IDisposable
    {
        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool RegisterHotKey(
            IntPtr hWnd, int id, uint fsModifiers, uint vlc);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        private const int WmHotkey = 0x0312;

        private readonly Dictionary<Hotkey, Action> _actions;

        private bool _disposed = false;

        /// <summary>
        /// Initializes a new instance of the <see cref="GlobalHotKeyManager"/> class.
        /// </summary>
        public GlobalHotKeyManager()
        {
            _actions = new Dictionary<Hotkey, Action>();

            // Please note that the message loop pumper calls ThreadFilterMessage and then
            // ThreadPreprocessMessage every time it receives a key stroke.  Thus, either of these
            // will suffice for our purposes.
            ComponentDispatcher.ThreadPreprocessMessage += OnThreadPreprocessMessage;
        }

        /// <summary>
        /// Finalizes an instance of the <see cref="GlobalHotKeyManager"/> class.
        /// </summary>
        ~GlobalHotKeyManager()
        {
            Dispose(false);
        }

        /// <summary>
        /// Disposes the current hotkey manager.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Disposes the current hotkey manager.
        /// </summary>
        /// <param name="disposing">Whether managed resources should be disposed.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }

            if (disposing)
            {
                foreach (KeyValuePair<Hotkey, Action> kvp in _actions)
                {
                    UnregisterHotKey(IntPtr.Zero, kvp.Key.GetHashCode());
                }
            }

            _disposed = true;
        }

        /// <summary>
        /// Registers a hotkey with an associated action that should be triggered when the hotkey
        /// is detected globally.
        /// </summary>
        /// <param name="hotkey">The hotkey to bind globally.</param>
        /// <param name="action">The action to run when the hotkey is detected.</param>
        public void Register(Hotkey hotkey, Action action)
        {
            _actions.Add(hotkey, action);

            var keyModifier = hotkey.KeyModifier;
            var key = KeyInterop.VirtualKeyFromKey(hotkey.Key);

            if (!RegisterHotKey(IntPtr.Zero, hotkey.GetHashCode(), (uint)keyModifier, (uint)key))
            {
                throw new Win32Exception(Marshal.GetLastWin32Error());
            }
        }

        /// <summary>
        /// The event handler that executes when a keyboard is message is received.  This handler
        /// will run the appropriate action based on the hotkey detected.
        /// </summary>
        /// <param name="msg">Message information for the key stroke.</param>
        /// <param name="handled">Whether or not the key stroke has been handled.</param>
        public void OnThreadPreprocessMessage(ref MSG msg, ref bool handled)
        {
            if (msg.message != WmHotkey)
            {
                return;
            }

            var key = KeyInterop.KeyFromVirtualKey(((int)msg.lParam >> 16) & 0xFFFF);
            var keyModifier = (KeyModifier)((int)msg.lParam & 0xFFFF);
            var hotkey = new Hotkey(keyModifier, key);

            Task.Run(_actions[hotkey]);
        }
    }
}
