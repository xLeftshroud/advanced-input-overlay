namespace AdvancedInputOverlay.Models;

/// <summary>
/// Bidirectional map between Windows virtual-key codes and the string names used in
/// the layout JSON. Mouse buttons are handled separately by <see cref="Services.InputHook"/>.
/// </summary>
public static class KeyMap
{
    private static readonly Dictionary<int, string> VkToName;
    private static readonly Dictionary<string, int> NameToVk;

    static KeyMap()
    {
        var map = new Dictionary<int, string>();

        // Letters A..Z
        for (int i = 0; i < 26; i++)
            map[0x41 + i] = ((char)('A' + i)).ToString();

        // Digits 0..9
        for (int i = 0; i < 10; i++)
            map[0x30 + i] = ((char)('0' + i)).ToString();

        // Function F1..F24
        for (int i = 0; i < 24; i++)
            map[0x70 + i] = $"F{i + 1}";

        // Modifiers (L/R-specific VKs delivered by low-level hook)
        map[0xA0] = "LShift";
        map[0xA1] = "RShift";
        map[0xA2] = "LCtrl";
        map[0xA3] = "RCtrl";
        map[0xA4] = "LAlt";
        map[0xA5] = "RAlt";
        map[0x5B] = "LWin";
        map[0x5C] = "RWin";

        // Navigation
        map[0x25] = "Left";
        map[0x26] = "Up";
        map[0x27] = "Right";
        map[0x28] = "Down";

        // Editing / control
        map[0x08] = "Backspace";
        map[0x09] = "Tab";
        map[0x0D] = "Enter";
        map[0x13] = "Pause";
        map[0x1B] = "Escape";
        map[0x20] = "Space";
        map[0x21] = "PageUp";
        map[0x22] = "PageDown";
        map[0x23] = "End";
        map[0x24] = "Home";
        map[0x2C] = "PrintScreen";
        map[0x2D] = "Insert";
        map[0x2E] = "Delete";
        map[0x5D] = "Apps";   // Context menu key

        // Symbols (US layout — VK codes are layout-independent but glyph isn't)
        map[0xBA] = "Semicolon";
        map[0xBB] = "Equal";
        map[0xBC] = "Comma";
        map[0xBD] = "Minus";
        map[0xBE] = "Period";
        map[0xBF] = "Slash";
        map[0xC0] = "BackQuote";
        map[0xDB] = "LBracket";
        map[0xDC] = "Backslash";
        map[0xDD] = "RBracket";
        map[0xDE] = "Quote";

        // Numpad
        for (int i = 0; i < 10; i++)
            map[0x60 + i] = $"Num{i}";
        map[0x6A] = "NumMul";
        map[0x6B] = "NumAdd";
        map[0x6D] = "NumSub";
        map[0x6E] = "NumDot";
        map[0x6F] = "NumDiv";

        // Locks
        map[0x14] = "CapsLock";
        map[0x90] = "NumLock";
        map[0x91] = "ScrollLock";

        VkToName = map;
        NameToVk = new Dictionary<string, int>(map.Count, StringComparer.OrdinalIgnoreCase);
        foreach (var kv in map)
            NameToVk[kv.Value] = kv.Key;
    }

    public static bool TryGetName(int vk, out string name) => VkToName.TryGetValue(vk, out name!);

    public static bool TryGetVk(string name, out int vk) => NameToVk.TryGetValue(name, out vk);
}
