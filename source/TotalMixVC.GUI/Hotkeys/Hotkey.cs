using System.Windows.Input;

namespace TotalMixVC.GUI.Hotkeys
{
    /// <summary>
    /// Represents a hotkey that may be bound.
    /// </summary>
    public record Hotkey
    {
        /// <summary>
        /// Gets the key modifier for the hotkey.
        /// </summary>
        public KeyModifier KeyModifier { get; init; }

        /// <summary>
        /// Gets the key that must be pressed with the modifier for the hotkey.
        /// </summary>
        public Key Key { get; init; }
    }
}
