using System;
using System.Runtime.InteropServices;

namespace TotalMixVC;

/// <summary>
/// Provides several useful utilities for manipulating windows.
/// </summary>
internal static class WindowServices
{
    private const int WsExTransparent = 0x00000020;

    private const int GwlExstyle = -20;

    /// <summary>
    /// Sets the extended window style to be transparent so that mouse events can pass through
    /// the window.
    /// </summary>
    /// <param name="hwnd">
    /// The raw window handle which is typically obtained via WindowInteropHelper.
    /// </param>
    public static void SetWindowExTransparent(IntPtr hwnd)
    {
        int extendedStyle = GetWindowLong(hwnd, GwlExstyle);
        SetWindowLong(hwnd, GwlExstyle, extendedStyle | WsExTransparent);
    }

    [DllImport("user32.dll")]
    private static extern int GetWindowLong(IntPtr hwnd, int index);

    [DllImport("user32.dll")]
    private static extern int SetWindowLong(IntPtr hwnd, int index, int newStyle);
}
