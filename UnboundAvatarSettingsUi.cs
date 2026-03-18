using System;
using System.Collections.Generic;
using System.Numerics;
using ExileCore.Shared.Nodes;
using ImGuiNET;

namespace WhatsAnUnboundAvatar;

public sealed class UnboundAvatarSettingsUi
{
    private int _activeTab;
    private readonly Dictionary<string, float> _anims = new();

    private static readonly string[] Tabs = { "Status", "Triggers", "HUD" };

    // ── Palette ─────────────────────────────────────────────────────
    // Electric blue accent — elemental power theme
    private static uint Accent => Col(0.27f, 0.53f, 1f);
    private static uint AccentDim => Col(0.18f, 0.35f, 0.65f);
    private static uint Label => Col(0.88f, 0.88f, 0.88f);
    private static uint Desc => Col(0.38f, 0.40f, 0.44f);
    private static uint TabOff => Col(0.48f, 0.48f, 0.52f);
    private static uint CardBg => Col(0.05f, 0.05f, 0.07f, 1f);
    private static uint StatusGreen => Col(0.2f, 0.85f, 0.3f);
    private static uint StatusDim => Col(0.3f, 0.3f, 0.35f);

    // State colors
    private static uint StateCharging => Col(0.3f, 0.8f, 0.3f);
    private static uint StateReady => Col(0.2f, 1f, 0.4f);
    private static uint StateActive => Col(1f, 0.6f, 0.1f);
    private static uint StateCooldown => Col(0.3f, 0.5f, 0.8f);

    private const float Row = 34f;

    // ── Entry point ─────────────────────────────────────────────────

    public void Draw(WhatsAnUnboundAvatarSettings s, UnboundAvatarUiState state)
    {
        var contentMin = ImGui.GetCursorScreenPos();
        float contentW = ImGui.GetContentRegionAvail().X;
        var dl = ImGui.GetWindowDrawList();

        // Tab bar
        float tabH = 26f;
        float tabW = contentW / Tabs.Length;

        for (int i = 0; i < Tabs.Length; i++)
        {
            var tMin = new Vector2(contentMin.X + i * tabW, contentMin.Y);
            var tMax = new Vector2(contentMin.X + (i + 1) * tabW, contentMin.Y + tabH);
            bool active = i == _activeTab;

            if (active)
                dl.AddRectFilled(tMin, tMax, WithAlpha(Accent, 0.12f));

            ImGui.SetCursorScreenPos(tMin);
            ImGui.InvisibleButton($"##ua_tab_{i}", tMax - tMin);
            bool hov = ImGui.IsItemHovered();
            if (ImGui.IsItemClicked()) _activeTab = i;

            if (hov && !active)
                dl.AddRectFilled(tMin, tMax, WithAlpha(Accent, 0.06f));
            if (active)
                dl.AddLine(tMin with { Y = tMax.Y - 1 } + new Vector2(3, 0),
                    tMax - new Vector2(3, 1), Accent, 2f);

            uint tc = active ? Accent : (hov ? AccentDim : TabOff);
            CenterText(dl, Tabs[i], (tMin + tMax) * 0.5f, tc);
        }

        float sepY = contentMin.Y + tabH + 1;
        dl.AddLine(new Vector2(contentMin.X, sepY),
            new Vector2(contentMin.X + contentW, sepY), WithAlpha(Accent, 0.10f), 1f);

        // Status bar below tabs
        float statusY = sepY + 4;
        DrawStatusBar(dl, contentMin.X, contentW, statusY, state);
        float statusH = 22f;

        // Content area
        ImGui.SetCursorScreenPos(new Vector2(contentMin.X, sepY + statusH + 6));
        var avail = new Vector2(contentW, ImGui.GetContentRegionAvail().Y);
        ImGui.BeginChild("##ua_content", avail, ImGuiChildFlags.None, ImGuiWindowFlags.None);

        var cdl = ImGui.GetWindowDrawList();
        var cMin = ImGui.GetWindowPos();
        var cSz = ImGui.GetWindowSize();

        cdl.AddRectFilled(cMin, cMin + cSz, CardBg, 3f);
        float pulse = (float)(0.4 + 0.3 * Math.Sin(ImGui.GetTime() * 1.8));
        cdl.AddRect(cMin, cMin + cSz, WithAlpha(Accent, pulse * 0.18f), 3f, ImDrawFlags.None, 1f);

        float scrollY = ImGui.GetScrollY();
        float y = cMin.Y + 10 - scrollY;
        float x = cMin.X + 12;
        float cx = cMin.X + cSz.X * 0.50f;
        float sw = cSz.X * 0.40f;

        switch (_activeTab)
        {
            case 0: TabStatus(cdl, s, x, cx, ref y, sw, cSz.X, state); break;
            case 1: TabTriggers(cdl, s.Triggers, x, cx, ref y, sw); break;
            case 2: TabHud(cdl, s.Hud, x, cx, ref y, sw); break;
        }

        ImGui.SetCursorScreenPos(new Vector2(x, y));
        ImGui.Dummy(new Vector2(1, 4));
        ImGui.EndChild();
    }

    // ── Status Bar ──────────────────────────────────────────────────

    private static void DrawStatusBar(ImDrawListPtr dl, float startX, float width, float y, UnboundAvatarUiState state)
    {
        float x = startX + 12;

        // State indicator
        uint stateCol = state.CurrentState switch
        {
            AvatarState.Charging => StateCharging,
            AvatarState.Ready => StateReady,
            AvatarState.Active => StateActive,
            AvatarState.Cooldown => StateCooldown,
            _ => StatusDim
        };
        string stateText = state.CurrentState.ToString();
        dl.AddCircleFilled(new Vector2(x + 4, y + 8), 4f, stateCol);
        dl.AddText(new Vector2(x + 12, y + 1), Label, stateText);
        x += ImGui.CalcTextSize(stateText).X + 26;

        // Fury stacks
        string furyText = $"{state.FuryStacks}/100";
        uint furyCol = state.FuryStacks >= 100 ? StateReady : (state.FuryStacks > 0 ? Label : StatusDim);
        dl.AddCircleFilled(new Vector2(x + 4, y + 8), 4f, furyCol);
        dl.AddText(new Vector2(x + 12, y + 1), furyCol, furyText);
        x += ImGui.CalcTextSize(furyText).X + 26;

        // Enemy count
        int totalEnemies = state.UniqueCount + state.RareUpCount - state.UniqueCount; // rare-only + unique
        string enemyText = $"{state.UniqueCount}U {state.RareUpCount}R+";
        uint enemyCol = state.UniqueCount > 0 || state.RareUpCount > 0 ? StatusGreen : StatusDim;
        dl.AddCircleFilled(new Vector2(x + 4, y + 8), 4f, enemyCol);
        dl.AddText(new Vector2(x + 12, y + 1), enemyCol, enemyText);
    }

    // ── Info Block ──────────────────────────────────────────────────

    private static void DrawInfoBlock(ImDrawListPtr dl, float x, ref float y, string title, string body)
    {
        float boxX = x + 2;
        float textX = boxX + 10;
        float startY = y;

        dl.AddText(new Vector2(textX, y + 2), Accent, title);
        y += 18f;

        foreach (var line in body.Split('\n'))
        {
            dl.AddText(new Vector2(textX, y), Desc, line);
            y += 15f;
        }
        y += 4f;

        dl.AddRectFilled(new Vector2(boxX, startY), new Vector2(boxX + 3, y), WithAlpha(Accent, 0.4f), 1f);
        y += 4f;
    }

    // ── Tabs ────────────────────────────────────────────────────────

    private void TabStatus(ImDrawListPtr dl, WhatsAnUnboundAvatarSettings s,
        float x, float cx, ref float y, float sw, float cardW, UnboundAvatarUiState state)
    {
        DrawInfoBlock(dl, x, ref y,
            "Unbound Avatar",
            "Accumulates Fury stacks (0-100) from elemental ailments.\n" +
            "At 100, activate for 10s of 80% more elemental damage.\n" +
            "Hits always inflict all elemental ailments while active.");

        // Live fury bar preview
        SectionHeader(dl, x, ref y, "Live Status");

        float barX = x + 6;
        float barW = cardW - 44f;
        float barH = 18f;

        // Fury bar background
        var barMin = new Vector2(barX, y);
        var barMax = new Vector2(barX + barW, y + barH);
        dl.AddRectFilled(barMin, barMax, Col(0.1f, 0.1f, 0.12f), 3f);

        // Fill based on state
        float frac;
        uint fillCol;
        string barLabel;

        switch (state.CurrentState)
        {
            case AvatarState.Charging:
                frac = state.FuryStacks / 100f;
                float t = frac;
                fillCol = Col(t, 1f - t * 0.22f, 0f);
                barLabel = $"Fury: {state.FuryStacks}/100";
                break;
            case AvatarState.Ready:
                frac = 1f;
                fillCol = StateReady;
                barLabel = "READY";
                break;
            case AvatarState.Active:
                frac = state.TotalDuration > 0 ? state.TimeRemaining / state.TotalDuration : 0f;
                fillCol = StateActive;
                barLabel = $"UNBOUND: {state.TimeRemaining:F1}s";
                break;
            default:
                frac = 0f;
                fillCol = StateCooldown;
                barLabel = "CD";
                break;
        }

        if (frac > 0f)
        {
            var fillMax = new Vector2(barX + barW * Math.Clamp(frac, 0f, 1f), y + barH);
            dl.AddRectFilled(barMin, fillMax, fillCol, 3f);
        }

        dl.AddRect(barMin, barMax, WithAlpha(Accent, 0.3f), 3f, ImDrawFlags.None, 1f);
        CenterText(dl, barLabel, (barMin + barMax) * 0.5f, Label);
        y += barH + 8f;

        // State dot
        uint dotCol = state.CurrentState switch
        {
            AvatarState.Charging => StateCharging,
            AvatarState.Ready => StateReady,
            AvatarState.Active => StateActive,
            AvatarState.Cooldown => StateCooldown,
            _ => StatusDim
        };

        string stateDetail = state.CurrentState switch
        {
            AvatarState.Charging => $"Charging (stacks: {state.FuryStacks}/100)",
            AvatarState.Ready => "Ready",
            AvatarState.Active => $"Active ({state.TimeRemaining:F1}s)",
            AvatarState.Cooldown => "Cooldown",
            _ => "Unknown"
        };

        dl.AddCircleFilled(new Vector2(x + 12, y + 7), 5f, dotCol);
        dl.AddText(new Vector2(x + 22, y), Label, stateDetail);
        y += 20f;

        // Enemy scan
        SectionHeader(dl, x, ref y, "Enemy Scan");

        if (state.UniqueCount > 0)
        {
            dl.AddCircleFilled(new Vector2(x + 12, y + 7), 4f, StatusGreen);
            dl.AddText(new Vector2(x + 22, y), Label,
                $"{state.UniqueCount} Unique nearb{(state.UniqueCount == 1 ? "y" : "ies")}");
            y += 18f;
        }

        if (state.RareUpCount > 0)
        {
            dl.AddCircleFilled(new Vector2(x + 12, y + 7), 4f, StatusGreen);
            dl.AddText(new Vector2(x + 22, y), Label,
                $"{state.RareUpCount} Rare+ nearby");
            y += 18f;
        }

        if (state.UniqueCount == 0 && state.RareUpCount == 0)
        {
            dl.AddCircleFilled(new Vector2(x + 12, y + 7), 4f, StatusDim);
            dl.AddText(new Vector2(x + 22, y), StatusDim, "No qualifying enemies");
            y += 18f;
        }

        // Keybind display
        SectionHeader(dl, x, ref y, "Keybind");
        dl.AddText(new Vector2(x + 6, y + 1), Label, "Skill Key");
        ImGui.SetCursorScreenPos(new Vector2(cx, y));
        s.Keybind.SkillKey.DrawPickerButton("ua_keybind");
        HelpMarker("Set this to the key bound to Unbound Avatar in-game.");
        y += Row;
    }

    private void TabTriggers(ImDrawListPtr dl, TriggerSettings s,
        float x, float cx, ref float y, float sw)
    {
        DrawInfoBlock(dl, x, ref y,
            "Trigger Conditions",
            "Configure when the skill auto-activates at 100 fury.\n" +
            "Multiple conditions are OR'd — any match will trigger.");

        SectionHeader(dl, x, ref y, "Enemy Triggers");
        Toggle("ua_uniq", s.ActivateOnUnique, dl, x, cx, ref y,
            "Activate on Unique", "Auto-activate when a Unique enemy is within range");
        Toggle("ua_rare", s.ActivateOnRareCount, dl, x, cx, ref y,
            "Activate on Rare Count", "Auto-activate when N+ Rare/Unique enemies are within range");
        IntSlider("ua_rct", s.RareCountThreshold, dl, x, cx, ref y, sw,
            "Rare Count Threshold", "Minimum rare+ count to trigger activation");
        Toggle("ua_always", s.ActivateAlways, dl, x, cx, ref y,
            "Always Activate", "Activate whenever ready, regardless of nearby enemies");

        SectionHeader(dl, x, ref y, "Scan & Input");
        IntSlider("ua_range", s.ActivationRange, dl, x, cx, ref y, sw,
            "Activation Range", "Grid-unit radius to scan for qualifying enemies");
        IntSlider("ua_cd", s.InputCooldownMs, dl, x, cx, ref y, sw,
            "Input Cooldown", "Minimum milliseconds between key presses");
    }

    private void TabHud(ImDrawListPtr dl, HudSettings s,
        float x, float cx, ref float y, float sw)
    {
        SectionHeader(dl, x, ref y, "Visibility");
        Toggle("ua_show", s.ShowHud, dl, x, cx, ref y,
            "Show HUD", "Display the fury/status bar overlay in-game");

        SectionHeader(dl, x, ref y, "Position");
        IntSlider("ua_hx", s.HudX, dl, x, cx, ref y, sw,
            "Position X", "Horizontal position of the status bar");
        IntSlider("ua_hy", s.HudY, dl, x, cx, ref y, sw,
            "Position Y", "Vertical position of the status bar");

        SectionHeader(dl, x, ref y, "Size");
        IntSlider("ua_bw", s.BarWidth, dl, x, cx, ref y, sw,
            "Bar Width", "Width of the status bar in pixels");
        IntSlider("ua_bh", s.BarHeight, dl, x, cx, ref y, sw,
            "Bar Height", "Height of the status bar in pixels");
    }

    // ── Widget primitives ───────────────────────────────────────────

    private static void HelpMarker(string desc)
    {
        ImGui.SameLine();
        ImGui.TextDisabled("(?)");
        if (ImGui.IsItemHovered())
        {
            ImGui.BeginTooltip();
            ImGui.PushTextWrapPos(ImGui.GetFontSize() * 35.0f);
            ImGui.TextUnformatted(desc);
            ImGui.PopTextWrapPos();
            ImGui.EndTooltip();
        }
    }

    private void SectionHeader(ImDrawListPtr dl, float x, ref float y, string title)
    {
        y += 4f;
        dl.AddText(new Vector2(x, y), Accent, title);
        y += 18f;
        dl.AddLine(new Vector2(x, y - 4), new Vector2(x + ImGui.CalcTextSize(title).X + 40, y - 4),
            WithAlpha(Accent, 0.25f), 1f);
    }

    private void Toggle(string key, ToggleNode node, ImDrawListPtr dl,
        float x, float cx, ref float y, string label, string desc)
    {
        dl.AddText(new Vector2(x + 6, y + 1), Label, label);
        dl.AddText(new Vector2(x + 6, y + 16), Desc, desc);
        ImGui.SetCursorScreenPos(new Vector2(cx, y + 5));
        var v = node.Value;
        _anims.TryGetValue(key, out float a);
        if (PillToggle($"##{key}", ref v, ref a))
            node.Value = v;
        _anims[key] = a;
        y += Row;
    }

    private void IntSlider(string key, RangeNode<int> node, ImDrawListPtr dl,
        float x, float cx, ref float y, float sw, string label, string desc)
    {
        dl.AddText(new Vector2(x + 6, y + 1), Label, label);
        dl.AddText(new Vector2(x + 6, y + 16), Desc, desc);
        ImGui.SetCursorScreenPos(new Vector2(cx, y + 7));
        var v = node.Value;
        if (SliderInt($"##{key}", ref v, node.Min, node.Max, sw))
            node.Value = v;
        dl.AddText(new Vector2(cx + sw + 6, y + 7), Accent, v.ToString());
        y += Row;
    }

    // ── Self-contained drawing helpers ──────────────────────────────

    private static bool PillToggle(string id, ref bool value, ref float animState)
    {
        var dl = ImGui.GetWindowDrawList();
        var cur = ImGui.GetCursorScreenPos();
        const float w = 40f, h = 20f, r = h * 0.5f;

        ImGui.InvisibleButton(id, new Vector2(w, h));
        bool changed = ImGui.IsItemClicked();
        if (changed) value = !value;

        float dt = ImGui.GetIO().DeltaTime;
        float target = value ? 1f : 0f;
        animState = Math.Clamp(animState + (target - animState) * Math.Min(dt * 10f, 1f), 0f, 1f);

        uint trackOff = ImGui.GetColorU32(new Vector4(0.2f, 0.2f, 0.2f, 1f));
        uint trackColor = LerpCol(trackOff, Accent, animState);
        dl.AddRectFilled(cur, cur + new Vector2(w, h), trackColor, r);

        float knobX = cur.X + r + animState * (w - h);
        uint knobCol = ImGui.GetColorU32(new Vector4(
            0.5f + 0.5f * animState, 0.5f + 0.5f * animState,
            0.5f + 0.5f * animState, 1f));
        dl.AddCircleFilled(new Vector2(knobX, cur.Y + r), r - 2f, knobCol);

        return changed;
    }

    private static bool SliderInt(string id, ref int value, int min, int max, float width)
    {
        var dl = ImGui.GetWindowDrawList();
        var cur = ImGui.GetCursorScreenPos();
        const float h = 16f, th = 4f, tr = 7f;

        ImGui.InvisibleButton(id, new Vector2(width, h));
        bool changed = false;
        if (ImGui.IsItemActive())
        {
            float frac = Math.Clamp((ImGui.GetMousePos().X - cur.X) / width, 0f, 1f);
            int nv = min + (int)(frac * (max - min));
            if (nv != value) { value = nv; changed = true; }
        }

        float trackY = cur.Y + (h - th) * 0.5f;
        dl.AddRectFilled(cur with { Y = trackY }, new Vector2(cur.X + width, trackY + th),
            ImGui.GetColorU32(new Vector4(0.15f, 0.15f, 0.15f, 1f)), 2f);

        float f = (max > min) ? (float)(value - min) / (max - min) : 0f;
        float fw = f * width;
        dl.AddRectFilled(cur with { Y = trackY }, new Vector2(cur.X + fw, trackY + th), Accent, 2f);

        float tx = cur.X + fw, ty = cur.Y + h * 0.5f;
        dl.AddCircleFilled(new Vector2(tx, ty), tr, Accent);
        dl.AddCircleFilled(new Vector2(tx, ty), tr - 2f,
            ImGui.GetColorU32(new Vector4(0.95f, 0.95f, 0.95f, 1f)));

        return changed;
    }

    // ── Color utilities ─────────────────────────────────────────────

    private static uint Col(float r, float g, float b, float a = 1f)
        => ImGui.GetColorU32(new Vector4(r, g, b, a));

    private static uint WithAlpha(uint color, float alpha)
    {
        var v = ImGui.ColorConvertU32ToFloat4(color);
        v.W = alpha;
        return ImGui.GetColorU32(v);
    }

    private static uint LerpCol(uint a, uint b, float t)
    {
        var va = ImGui.ColorConvertU32ToFloat4(a);
        var vb = ImGui.ColorConvertU32ToFloat4(b);
        return ImGui.GetColorU32(new Vector4(
            va.X + (vb.X - va.X) * t, va.Y + (vb.Y - va.Y) * t,
            va.Z + (vb.Z - va.Z) * t, va.W + (vb.W - va.W) * t));
    }

    private static void CenterText(ImDrawListPtr dl, string text, Vector2 center, uint color)
    {
        var sz = ImGui.CalcTextSize(text);
        dl.AddText(center - sz * 0.5f, color, text);
    }
}
