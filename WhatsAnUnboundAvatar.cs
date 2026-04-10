using System;
using System.Collections.Generic;
using System.Linq;
using ExileCore;
using ExileCore.PoEMemory.Components;
using ExileCore.Shared;
using ExileCore.Shared.Enums;
using SharpDX;
using Color = SharpDX.Color;
using RectangleF = SharpDX.RectangleF;

namespace WhatsAnUnboundAvatar;

public enum AvatarState { Charging, Ready, Active, Cooldown }

public record UnboundAvatarUiState(
    AvatarState CurrentState,
    int FuryStacks,
    float TimeRemaining,
    float TotalDuration,
    int UniqueCount,
    int RareUpCount,
    int ActivationRange
);

public class WhatsAnUnboundAvatar : BaseSettingsPlugin<WhatsAnUnboundAvatarSettings>
{
    private const string FuryBuffName = "ailment_bearer_activation_buff";
    private const string ActiveBuffName = "ailment_bearer_buff";
    private const string GracePeriodBuffName = "grace_period";
    private const int MaxFuryStacks = 100;

    private DateTime _lastKeyPressAt;
    private bool _canRender;

    // Cached state for Render() and settings UI
    private AvatarState _state = AvatarState.Charging;
    private int _furyStacks;
    private float _timeRemaining;
    private float _totalDuration;
    private int _uniqueCount;
    private int _rareUpCount;

    private UnboundAvatarSettingsUi? _settingsUi;

    public override bool Initialise()
    {
        Name = "Whats An Unbound Avatar";
        Settings.Triggers ??= new TriggerSettings();
        Settings.Keybind ??= new KeybindSettings();
        Settings.Hud ??= new HudSettings();
        return true;
    }

    public override Job? Tick()
    {
        _canRender = false;

        if (!GameController.InGame) return null;

        var player = GameController.Player;
        if (player == null || !player.IsAlive) return null;

        var area = GameController.Area.CurrentArea;
        if (area == null || area.IsTown || area.IsHideout) return null;

        var buffs = player.GetComponent<Buffs>()?.BuffsList;
        if (buffs == null) return null;

        // Grace period guard
        for (int i = 0; i < buffs.Count; i++)
        {
            if (buffs[i].Name == GracePeriodBuffName)
                return null;
        }

        // Read buffs
        Buff? furyBuff = null;
        Buff? activeBuff = null;
        for (int i = 0; i < buffs.Count; i++)
        {
            var b = buffs[i];
            if (b.Name == FuryBuffName) furyBuff = b;
            else if (b.Name == ActiveBuffName) activeBuff = b;
        }

        // Determine state
        int stacks = furyBuff?.BuffCharges ?? 0;
        _furyStacks = stacks;

        if (activeBuff != null)
        {
            _state = AvatarState.Active;
            _timeRemaining = activeBuff.Timer;
            _totalDuration = activeBuff.MaxTime;
        }
        else if (stacks >= MaxFuryStacks)
        {
            _state = AvatarState.Ready;
            _timeRemaining = 0;
            _totalDuration = 0;
        }
        else if (furyBuff != null)
        {
            _state = AvatarState.Charging;
            _timeRemaining = 0;
            _totalDuration = 0;
        }
        else
        {
            // No fury buff and no active buff = cooldown (or skill not allocated)
            _state = AvatarState.Cooldown;
            _timeRemaining = 0;
            _totalDuration = 0;
        }

        // Enemy scan
        _uniqueCount = 0;
        _rareUpCount = 0;
        int range = Settings.Triggers.ActivationRange.Value;

        if (GameController.EntityListWrapper.ValidEntitiesByType
            .TryGetValue(EntityType.Monster, out var monsters))
        {
            foreach (var m in monsters)
            {
                if (!m.IsAlive || !m.IsHostile) continue;
                if (m.DistancePlayer > range) continue;

                var rarity = m.Rarity;
                if (rarity == MonsterRarity.Unique)
                {
                    _uniqueCount++;
                    _rareUpCount++;
                }
                else if (rarity == MonsterRarity.Rare)
                {
                    _rareUpCount++;
                }
            }
        }

        // Activation logic
        if (_state == AvatarState.Ready)
        {
            bool shouldActivate = false;

            if (Settings.Triggers.ActivateAlways.Value)
                shouldActivate = true;
            else if (Settings.Triggers.ActivateOnUnique.Value && _uniqueCount > 0)
                shouldActivate = true;
            else if (Settings.Triggers.ActivateOnRareCount.Value &&
                     _rareUpCount >= Settings.Triggers.RareCountThreshold.Value)
                shouldActivate = true;

            if (shouldActivate && CanSendInput())
            {
                var now = DateTime.UtcNow;
                if ((now - _lastKeyPressAt).TotalMilliseconds >= Settings.Triggers.InputCooldownMs.Value)
                {
                    var key = Settings.Keybind.SkillKey.Value.Key;
                    if (key != System.Windows.Forms.Keys.None)
                    {
                        Input.KeyDown(key);
                        Input.KeyUp(key);
                        _lastKeyPressAt = now;
                    }
                }
            }
        }

        _canRender = true;
        return null;
    }

    private bool CanSendInput()
    {
        try
        {
            if (!GameController.Window.IsForeground())
                return false;

            var chatPanel = GameController.IngameState.IngameUi.ChatPanel;
            if (chatPanel?.ChatTitlePanel?.IsVisible == true)
                return false;

            return true;
        }
        catch
        {
            return false;
        }
    }

    public override void Render()
    {
        if (!_canRender) return;
        if (!Settings.Hud.ShowHud.Value) return;

        try
        {
            if (GameController.IngameState.IngameUi.FullscreenPanels.Any(x => x.IsVisible)) return;
            if (GameController.IngameState.IngameUi.LargePanels.Any(x => x.IsVisible)) return;
        }
        catch { return; }

        float x = Settings.Hud.HudX.Value;
        float y = Settings.Hud.HudY.Value;
        float w = Settings.Hud.BarWidth.Value;
        float h = Settings.Hud.BarHeight.Value;

        var barRect = new RectangleF(x, y, w, h);
        DrawStatusBar(barRect);
    }

    private void DrawStatusBar(RectangleF barRect)
    {
        float frac;
        Color fillColor;
        string label;

        switch (_state)
        {
            case AvatarState.Charging:
                frac = (float)_furyStacks / MaxFuryStacks;
                // Green-to-yellow gradient based on stacks
                float t = frac;
                fillColor = new Color(
                    (int)(t * 255),           // R: 0→255
                    (int)(255 - t * 55),      // G: 255→200
                    0, 255);
                label = $"Fury: {_furyStacks}/{MaxFuryStacks}";
                break;

            case AvatarState.Ready:
                frac = 1f;
                fillColor = Color.LimeGreen;
                string enemyInfo = _uniqueCount > 0 ? $" [{_uniqueCount}U]" :
                                   _rareUpCount > 0 ? $" [{_rareUpCount}R]" : "";
                label = $"READY{enemyInfo}";
                break;

            case AvatarState.Active:
                frac = _totalDuration > 0 ? _timeRemaining / _totalDuration : 0f;
                fillColor = Color.Orange;
                label = $"UNBOUND: {_timeRemaining:F1}s";
                break;

            case AvatarState.Cooldown:
            default:
                frac = 0f;
                fillColor = new Color(60, 120, 200);
                label = "CD";
                break;
        }

        // Background
        Graphics.DrawBox(barRect, new Color(30, 30, 30, 200));

        // Fill
        if (frac > 0)
        {
            var fillRect = new RectangleF(barRect.X, barRect.Y,
                barRect.Width * Math.Clamp(frac, 0f, 1f), barRect.Height);
            Graphics.DrawBox(fillRect, fillColor);
        }

        // Border
        Graphics.DrawFrame(barRect, Color.Gray, 1);

        // Text
        var textPos = new System.Numerics.Vector2(barRect.X + 4, barRect.Y + 2);
        Graphics.DrawText(label, textPos, Color.White);
    }

    public override void DrawSettings()
    {
        _settingsUi ??= new UnboundAvatarSettingsUi();
        _settingsUi.Draw(Settings, GetUiState());
    }

    internal UnboundAvatarUiState GetUiState() => new(
        _state,
        _furyStacks,
        _timeRemaining,
        _totalDuration,
        _uniqueCount,
        _rareUpCount,
        Settings.Triggers.ActivationRange.Value
    );
}
