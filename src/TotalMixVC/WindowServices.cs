namespace TotalMixVC;

using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

/// <summary>
/// Provides several useful utilities for manipulating windows.
/// </summary>
[SuppressMessage(
    "StyleCop.CSharp.NamingRules",
    "SA1310:Field names should not contain underscore",
    Justification = "Use the appropriate case for constants to match the Win32 SDK.")]
internal static partial class WindowServices
{
    private const int WS_EX_TRANSPARENT = 0x00000020;

    private const int GWL_EXSTYLE = -20;

    /// <summary>
    /// Sets the extended window style to be transparent so that mouse events can pass through
    /// the window.
    /// </summary>
    /// <param name="hwnd">
    /// The raw window handle which is typically obtained via WindowInteropHelper.
    /// </param>
    public static void SetWindowExTransparent(nint hwnd)
    {
        int extendedStyle = GetWindowLong(hwnd, GWL_EXSTYLE);
        _ = SetWindowLong(hwnd, GWL_EXSTYLE, extendedStyle | WS_EX_TRANSPARENT);
    }

    [LibraryImport("user32.dll")]
    private static partial int GetWindowLong(nint hwnd, int index);

    [LibraryImport("user32.dll")]
    private static partial int SetWindowLong(nint hwnd, int index, int newStyle);
}
