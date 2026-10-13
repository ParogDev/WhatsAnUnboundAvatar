using System.Windows.Forms;
using ExileCore.Shared.Attributes;
using ExileCore.Shared.Interfaces;
using ExileCore.Shared.Nodes;

namespace WhatsAnUnboundAvatar;

// Drawn by SettingsMenu (SettingsMenu.cs): every [Submenu] property is a tab, in declaration order; Keybind joins the
// live Status tab through [MenuTab]. [Menu] gives the label and the description, [MenuUnit] the slider suffix.
public class WhatsAnUnboundAvatarSettings : ISettings
{
    [Menu("Enable", "Turn the whole plugin on or off")]
    public ToggleNode Enable { get; set; } = new(true);

    [MenuTab("Status")]
    [Menu("Keybind", "Key the plugin presses to activate the skill")]
    public KeybindSettings Keybind { get; set; } = new();

    [Menu("Triggers", "When Unbound Avatar is activated automatically")]
    public TriggerSettings Triggers { get; set; } = new();

    [Menu("HUD", "On-screen fury/status bar")]
    public HudSettings Hud { get; set; } = new();
}

[Submenu]
public class TriggerSettings
{
    [Menu("Activate on Unique", "Activate when a unique enemy is within Activation Range")]
    public ToggleNode ActivateOnUnique { get; set; } = new(true);

    [Menu("Activate on Rare Count", "Activate when at least Rare Count Threshold rare or unique enemies are within range")]
    public ToggleNode ActivateOnRareCount { get; set; } = new(true);

    [Menu("Rare Count Threshold", "How many rare+ enemies in range trigger activation (1-20)")]
    public RangeNode<int> RareCountThreshold { get; set; } = new(3, 1, 20);

    [Menu("Always Activate", "Activate whenever ready, regardless of nearby enemies")]
    public ToggleNode ActivateAlways { get; set; } = new(false);

    [Menu("Activation Range", "Radius scanned for qualifying enemies, in grid units (10-200)")]
    [MenuUnit("grid")]
    public RangeNode<int> ActivationRange { get; set; } = new(80, 10, 200);

    [Menu("Input Cooldown", "Minimum time between skill key presses, in ms (200-2000)")]
    [MenuUnit("ms")]
    public RangeNode<int> InputCooldownMs { get; set; } = new(500, 200, 2000);
}

[Submenu]
public class KeybindSettings
{
    [Menu("Skill Key", "Key bound to Unbound Avatar in game; the plugin presses it when a trigger fires")]
    public HotkeyNodeV2 SkillKey { get; set; } = new(Keys.XButton1);
}

[Submenu]
public class HudSettings
{
    [Menu("Show HUD", "Show the fury/status bar on screen")]
    public ToggleNode ShowHud { get; set; } = new(true);

    [Menu("Position X", "Left edge of the bar, in screen px")]
    [MenuUnit("px")]
    public RangeNode<int> HudX { get; set; } = new(500, 0, 3840);

    [Menu("Position Y", "Top edge of the bar, in screen px")]
    [MenuUnit("px")]
    public RangeNode<int> HudY { get; set; } = new(36, 0, 2160);

    [Menu("Bar Width", "Width of the status bar, in px (50-600)")]
    [MenuUnit("px")]
    public RangeNode<int> BarWidth { get; set; } = new(220, 50, 600);

    [Menu("Bar Height", "Height of the status bar, in px (10-60)")]
    [MenuUnit("px")]
    public RangeNode<int> BarHeight { get; set; } = new(22, 10, 60);
}
