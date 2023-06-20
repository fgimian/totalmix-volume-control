namespace TotalMixVC.Hotkeys;

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Windows.Input;
using System.Windows.Interop;

/// <summary>
/// Manages various global hotkeys along with their associated actions.
/// </summary>
public partial class GlobalHotKeyManager : IDisposable
{
    [SuppressMessage(
        "StyleCop.CSharp.NamingRules",
        "SA1310:Field names should not contain underscore",
        Justification = "Use the appropriate case for constants to match the Win32 SDK.")]
    private const int WM_HOTKEY = 0x0312;

    private readonly Dictionary<Hotkey, Action> _actions;

    private bool _disposed;

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

        if (!RegisterHotKey(nint.Zero, hotkey.GetHashCode(), (uint)keyModifier, (uint)key))
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
        if (msg.message != WM_HOTKEY)
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
                UnregisterHotKey(nint.Zero, kvp.Key.GetHashCode());
            }
        }

        _disposed = true;
    }

    [LibraryImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool RegisterHotKey(
    nint hWnd, int id, uint fsModifiers, uint vlc);

    [LibraryImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool UnregisterHotKey(nint hWnd, int id);
}
