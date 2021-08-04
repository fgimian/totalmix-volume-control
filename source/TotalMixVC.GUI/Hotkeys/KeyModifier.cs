using System;

namespace TotalMixVC.GUI.Hotkeys
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
}
