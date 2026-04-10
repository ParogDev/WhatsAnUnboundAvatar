using System.Windows.Forms;
using ExileCore.Shared.Attributes;
using ExileCore.Shared.Interfaces;
using ExileCore.Shared.Nodes;

namespace WhatsAnUnboundAvatar;

public class WhatsAnUnboundAvatarSettings : ISettings
{
    public ToggleNode Enable { get; set; } = new(true);

    public TriggerSettings Triggers { get; set; } = new();
    public KeybindSettings Keybind { get; set; } = new();
    public HudSettings Hud { get; set; } = new();
}

[Submenu]
public class TriggerSettings
{
    public ToggleNode ActivateOnUnique { get; set; } = new(true);
    public ToggleNode ActivateOnRareCount { get; set; } = new(true);
    public RangeNode<int> RareCountThreshold { get; set; } = new(3, 1, 20);
    public ToggleNode ActivateAlways { get; set; } = new(false);
    public RangeNode<int> ActivationRange { get; set; } = new(80, 10, 200);
    public RangeNode<int> InputCooldownMs { get; set; } = new(500, 200, 2000);
}

[Submenu]
public class KeybindSettings
{
    public HotkeyNodeV2 SkillKey { get; set; } = new(Keys.XButton1);
}

[Submenu]
public class HudSettings
{
    public ToggleNode ShowHud { get; set; } = new(true);
    public RangeNode<int> HudX { get; set; } = new(500, 0, 3840);
    public RangeNode<int> HudY { get; set; } = new(36, 0, 2160);
    public RangeNode<int> BarWidth { get; set; } = new(220, 50, 600);
    public RangeNode<int> BarHeight { get; set; } = new(22, 10, 60);
}
