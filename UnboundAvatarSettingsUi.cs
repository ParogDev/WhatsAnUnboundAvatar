using System;
using System.Numerics;
using ImGuiNET;

namespace WhatsAnUnboundAvatar;

// Settings panel: the generated SettingsMenu (tabs Status / Triggers / HUD from the [Submenu] properties and their
// [Menu] labels) plus the live parts it can't know about: a status strip above the tabs and, on the Status tab, the
// mechanic explanation, a live fury bar and the enemy scan the triggers read.
public sealed class UnboundAvatarSettingsUi
{
    private readonly SettingsMenu _menu;
    private UnboundAvatarUiState? _state;

    // Electric blue accent: elemental power theme.
    private static readonly Vector4 Accent = new(0.27f, 0.53f, 1f, 1f);
    private static readonly Vector4 LabelCol = new(0.88f, 0.88f, 0.88f, 1f);
    private static readonly Vector4 Muted = new(0.52f, 0.55f, 0.60f, 1f);
    private static readonly Vector4 Good = new(0.2f, 0.85f, 0.3f, 1f);
    private static readonly Vector4 StateCharging = new(0.3f, 0.8f, 0.3f, 1f);
    private static readonly Vector4 StateReady = new(0.2f, 1f, 0.4f, 1f);
    private static readonly Vector4 StateActive = new(1f, 0.6f, 0.1f, 1f);
    private static readonly Vector4 StateCooldown = new(0.3f, 0.5f, 0.8f, 1f);

    public UnboundAvatarSettingsUi()
    {
        // "Status" and "Triggers" must match the generated tab names ([MenuTab("Status")] on Keybind, [Menu("Triggers")]
        // on Triggers): a name that matches no generated tab becomes a tab of its own, holding only this body.
        _menu = new SettingsMenu { Accent = Accent }
            .Banner(DrawStatusStrip)
            .Tab("Status", DrawStatusTab, first: true)
            .Tab("Triggers", DrawTriggersIntro, first: true);
    }

    public void Draw(WhatsAnUnboundAvatarSettings settings, UnboundAvatarUiState state)
    {
        _state = state;
        _menu.Draw(settings);
    }

    private static Vector4 StateColor(AvatarState s) => s switch
    {
        AvatarState.Charging => StateCharging,
        AvatarState.Ready => StateReady,
        AvatarState.Active => StateActive,
        AvatarState.Cooldown => StateCooldown,
        _ => Muted
    };

    // ---- status strip above the tabs ----

    private void DrawStatusStrip()
    {
        var st = _state;
        if (st == null) { SettingsMenu.Dim("No live state yet"); return; }
        _menu.Badge(st.CurrentState.ToString(), StateColor(st.CurrentState), "What the plugin reads from your buffs right now");
        ImGui.SameLine();
        _menu.Badge($"Fury {st.FuryStacks}/100", st.FuryStacks >= 100 ? StateReady : st.FuryStacks > 0 ? LabelCol : Muted,
            "Unbound Fury stacks (ailment_bearer_activation_buff charges)");
        ImGui.SameLine();
        bool any = st.UniqueCount > 0 || st.RareUpCount > 0;
        _menu.Badge($"{st.UniqueCount} unique / {st.RareUpCount} rare+", any ? Good : Muted,
            $"Enemies within Activation Range ({st.ActivationRange} grid)");
        ImGui.Spacing();
    }

    // ---- Status tab ----

    private void DrawStatusTab()
    {
        Intro("Unbound Avatar",
            "Accumulates Fury stacks (0-100) from elemental ailments. At 100, activate for 10 s of 80% more "
            + "elemental damage; hits always inflict all elemental ailments while active.");

        var st = _state;
        _menu.SectionHeader("Live Status");
        if (st == null) { SettingsMenu.Dim("No live state yet"); return; }
        DrawFuryBar(st);
        string detail = st.CurrentState switch
        {
            AvatarState.Charging => $"Charging (stacks: {st.FuryStacks}/100)",
            AvatarState.Ready => "Ready",
            AvatarState.Active => $"Active ({st.TimeRemaining:F1}s)",
            AvatarState.Cooldown => "Cooldown",
            _ => "Unknown"
        };
        Dot(StateColor(st.CurrentState), detail, LabelCol);

        _menu.SectionHeader("Enemy Scan", $"Enemies within Activation Range ({st.ActivationRange} grid)");
        if (st.UniqueCount > 0) Dot(Good, $"{st.UniqueCount} Unique nearb{(st.UniqueCount == 1 ? "y" : "ies")}", LabelCol);
        if (st.RareUpCount > 0) Dot(Good, $"{st.RareUpCount} Rare+ nearby", LabelCol);
        if (st.UniqueCount == 0 && st.RareUpCount == 0) Dot(Muted, "No qualifying enemies", Muted);
        ImGui.Spacing();
    }

    private void DrawTriggersIntro() =>
        Intro("Trigger Conditions",
            "When fury reaches 100 the skill key is pressed if any enabled condition matches (they are OR'd).");

    private static void DrawFuryBar(UnboundAvatarUiState st)
    {
        float frac;
        Vector4 fill;
        string label;
        switch (st.CurrentState)
        {
            case AvatarState.Charging:
                frac = st.FuryStacks / 100f;
                fill = new Vector4(frac, 1f - frac * 0.22f, 0f, 1f);
                label = $"Fury: {st.FuryStacks}/100";
                break;
            case AvatarState.Ready:
                frac = 1f; fill = StateReady; label = "READY";
                break;
            case AvatarState.Active:
                frac = st.TotalDuration > 0 ? st.TimeRemaining / st.TotalDuration : 0f;
                fill = StateActive; label = $"UNBOUND: {st.TimeRemaining:F1}s";
                break;
            default:
                frac = 0f; fill = StateCooldown; label = "CD";
                break;
        }

        var dl = ImGui.GetWindowDrawList();
        var min = ImGui.GetCursorScreenPos();
        float w = Math.Max(60f, ImGui.GetContentRegionAvail().X - 8f);
        const float h = 18f;
        var max = min + new Vector2(w, h);
        dl.AddRectFilled(min, max, ImGui.GetColorU32(new Vector4(0.1f, 0.1f, 0.12f, 1f)), 3f);
        if (frac > 0f) dl.AddRectFilled(min, new Vector2(min.X + w * Math.Clamp(frac, 0f, 1f), max.Y), ImGui.GetColorU32(fill), 3f);
        dl.AddRect(min, max, ImGui.GetColorU32(Accent with { W = 0.3f }), 3f);
        var ts = ImGui.CalcTextSize(label);
        dl.AddText((min + max) * 0.5f - ts * 0.5f, ImGui.GetColorU32(LabelCol), label);
        ImGui.Dummy(new Vector2(w, h + 4f));
    }

    // ---- small helpers ----

    private static void Intro(string title, string body)
    {
        SettingsMenu.Text(Accent, title);
        ImGui.PushTextWrapPos(0f);
        SettingsMenu.Text(Muted, body);
        ImGui.PopTextWrapPos();
        ImGui.Spacing();
    }

    private static void Dot(Vector4 dot, string text, Vector4 textCol)
    {
        var p = ImGui.GetCursorScreenPos();
        float lh = ImGui.GetTextLineHeight();
        ImGui.GetWindowDrawList().AddCircleFilled(new Vector2(p.X + 6f, p.Y + lh * 0.5f), 4f, ImGui.GetColorU32(dot));
        ImGui.SetCursorScreenPos(new Vector2(p.X + 16f, p.Y));
        SettingsMenu.Text(textCol, text);
    }
}
