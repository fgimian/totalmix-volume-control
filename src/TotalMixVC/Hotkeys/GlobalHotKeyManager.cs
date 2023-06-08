using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows.Input;
using System.Windows.Interop;

namespace TotalMixVC.Hotkeys;

/// <summary>
/// Manages various global hotkeys along with their associated actions.
/// </summary>
public class GlobalHotKeyManager : IDisposable
{
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
    /// Registers a hotkey with an associated action that should be triggered when the hotkey
    /// is detected globally.
    /// </summary>
    /// <param name="hotkey">The hotkey to bind globally.</param>
    /// <param name="action">The action to run when the hotkey is detected.</param>
    /// <exception cref="Win32Exception">
    /// An issue occurred during the registration of the hotkey.
    /// </exception>
    public void Register(Hotkey hotkey, Action action)
    {
        _actions.Add(hotkey, action);

        KeyModifier keyModifier = hotkey.KeyModifier;
        int key = KeyInterop.VirtualKeyFromKey(hotkey.Key);

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

        Key key = KeyInterop.KeyFromVirtualKey(((int)msg.lParam >> 16) & 0xFFFF);
        KeyModifier keyModifier = (KeyModifier)((int)msg.lParam & 0xFFFF);
        Hotkey hotkey = new() { KeyModifier = keyModifier, Key = key };

        _actions[hotkey]();
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

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool RegisterHotKey(
        IntPtr hWnd, int id, uint fsModifiers, uint vlc);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);
}
