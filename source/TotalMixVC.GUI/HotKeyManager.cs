using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Interop;

namespace TotalMixVC.GUI
{
    [Flags]
    public enum KeyModifier
    {
        None = 0x0000,
        Alt = 0x0001,
        Ctrl = 0x0002,
        Shift = 0x0004,
        Win = 0x0008
    }

    public record Hotkey(KeyModifier KeyModifier, Key Key);

    public class HotKeyManager : IDisposable
    {
        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool RegisterHotKey(
            IntPtr hWnd, int id, uint fsModifiers, uint vlc);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        private const int WmHotkey = 0x0312;

        private readonly Dictionary<Hotkey, Action> _actions;

        private bool _disposed = false;

        public HotKeyManager()
        {
            _actions = new Dictionary<Hotkey, Action>();

            // TODO: What is the difference between ThreadFilterMessage and ThreadPreprocessMessage?
            ComponentDispatcher.ThreadPreprocessMessage += OnThreadPreprocessMessage;
        }

        ~HotKeyManager()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

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

        public void OnThreadPreprocessMessage(ref MSG msg, ref bool handled)
        {
            if (msg.message != WmHotkey)
            {
                return;
            }

            var key = KeyInterop.KeyFromVirtualKey(((int)msg.lParam >> 16) & 0xFFFF);
            var keyModifier = (KeyModifier)((int)msg.lParam & 0xFFFF);
            var hotKey = new Hotkey(keyModifier, key);

            Task.Run(_actions[hotKey]);
        }

        public void Register(Hotkey hotKey, Action action)
        {
            _actions.Add(hotKey, action);

            var keyModifier = hotKey.KeyModifier;
            var key = KeyInterop.VirtualKeyFromKey(hotKey.Key);

            if (!RegisterHotKey(IntPtr.Zero, hotKey.GetHashCode(), (uint)keyModifier, (uint)key))
            {
                throw new Win32Exception(Marshal.GetLastWin32Error());
            }
        }
    }
}
