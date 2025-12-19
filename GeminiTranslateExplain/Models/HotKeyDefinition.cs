using System.Windows.Input;

namespace GeminiTranslateExplain.Models
{
    public struct HotKeyDefinition
    {
        public ModifierKeys Modifiers { get; set; }
        public Key Key { get; set; }

        public HotKeyDefinition(ModifierKeys modifiers, Key key)
        {
            Modifiers = modifiers;
            Key = key;
        }

        public static HotKeyDefinition Default => new HotKeyDefinition(ModifierKeys.Control, Key.J);

        public static HotKeyDefinition ScreenshotDefault => new HotKeyDefinition(ModifierKeys.Control | ModifierKeys.Alt, Key.S);
    }
}
