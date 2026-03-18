# CLAUDE.md — WhatsAnUnboundAvatar

## Plugin Context

Auto-activates the Unbound Avatar (Avatar of the Wilds keystone) skill in PoE2 when fury stacks reach 100 and configurable enemy conditions are met. Displays a HUD bar showing fury progress, active buff state, and cooldown. The player accumulates Unbound Fury stacks (0-100) by inflicting elemental ailments; at 100 stacks the skill grants the "Unbound" buff for 10 seconds (80% more elemental damage, hits always inflict all elemental ailments).

**Main class**: `WhatsAnUnboundAvatar` inherits `BaseSettingsPlugin<WhatsAnUnboundAvatarSettings>`
**Settings class**: `WhatsAnUnboundAvatarSettings` implements `ISettings`
**Key classes**: `TriggerSettings`, `KeybindSettings`, `HudSettings`, `UnboundAvatarSettingsUi`, `UnboundAvatarUiState` (record), `AvatarState` (enum: Charging, Ready, Active, Cooldown)

### Architecture

- **Tick()**: Reads player buffs to determine state (Charging/Ready/Active/Cooldown), scans nearby monsters for Unique/Rare counts, evaluates trigger conditions, sends key input when conditions are met. Caches all state for Render and settings UI.
- **Render()**: Draws a single HUD status bar showing fury progress (green→yellow gradient), ready state (lime), active countdown (orange drain), or cooldown (blue).
- **DrawSettings()**: Delegates to `UnboundAvatarSettingsUi` which provides a 3-tab neon-styled ImGui panel (Status, Triggers, HUD) with electric blue accent.

### Buff Names

| Buff | Internal Name | Key Properties |
|------|--------------|----------------|
| Fury stacks | `ailment_bearer_activation_buff` | `.Charges` = stack count (0-100) |
| Active (Unbound) | `ailment_bearer_buff` | `.Timer` = seconds remaining, `.MaxTime` = total duration (10s) |

### Lifecycle Methods Used

| Method | What it does |
|--------|-------------|
| `Initialise()` | Null-coalesces settings sub-objects |
| `Tick()` | All logic: buff reading, enemy scan, activation decision, input sending |
| `Render()` | Draws HUD status bar |
| `DrawSettings()` | Delegates to custom `UnboundAvatarSettingsUi` |

### Current Settings

| Setting | Type | Default | Location |
|---------|------|---------|----------|
| Enable | ToggleNode | true | Root |
| ActivateOnUnique | ToggleNode | true | Triggers |
| ActivateOnRareCount | ToggleNode | true | Triggers |
| RareCountThreshold | RangeNode\<int\> | 3 (1-20) | Triggers |
| ActivateAlways | ToggleNode | false | Triggers |
| ActivationRange | RangeNode\<int\> | 80 (10-200) | Triggers |
| InputCooldownMs | RangeNode\<int\> | 500 (200-2000) | Triggers |
| SkillKey | HotkeyNodeV2 | XButton1 | Keybind |
| ShowHud | ToggleNode | true | Hud |
| HudX | RangeNode\<int\> | 500 (0-3840) | Hud |
| HudY | RangeNode\<int\> | 36 (0-2160) | Hud |
| BarWidth | RangeNode\<int\> | 220 (50-600) | Hud |
| BarHeight | RangeNode\<int\> | 22 (10-60) | Hud |

## Project Setup

- This is an ExileApi plugin (game HUD overlay framework for Path of Exile)
- Do not edit anything outside this directory
- Target framework: net10.0-windows, OutputType: Library

### Namespace → DLL Mapping
| DLL | Key Namespaces |
|---|---|
| `ExileCore.dll` | `ExileCore` (BaseSettingsPlugin, GameController, Graphics, Input), `ExileCore.Shared` (Nodes, Enums, Interfaces, Attributes, Helpers), `ExileCore.PoEMemory` (Components, MemoryObjects) |
| `GameOffsets.dll` | `GameOffsets` (offsets structs), `GameOffsets.Native` (Vector2i, NativeStringU) |

## Build & Run

- NO manual build command — Loader.exe auto-compiles from Plugins/Source/
- HUD installation path: resolved from .csproj HintPath (parent dir of ExileCore.dll)
- For IDE support set env var: `exapiPackage` = `<HUD installation path>`

## API Reference

- **Default**: HUD installation (from .csproj HintPath) — compiled DLLs with intellisense
- **Enhanced**: If `.claude/override-path` exists, read it for a path to expanded
  API reference with full type definitions and source. Use that path for deep lookups
  when the compiled DLLs don't provide enough detail about a type, method, or pattern.

## Plugin Anatomy

Every plugin is a C# class library. The main class inherits `BaseSettingsPlugin<TSettings>` and the settings class implements `ISettings`.

Minimal `.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0-windows</TargetFramework>
    <OutputType>Library</OutputType>
    <UseWindowsForms>true</UseWindowsForms>
    <PlatformTarget>x64</PlatformTarget>
    <LangVersion>latest</LangVersion>
    <DebugType>embedded</DebugType>
  </PropertyGroup>
  <ItemGroup>
    <Reference Include="ExileCore">
      <HintPath>$(exapiPackage)\ExileCore.dll</HintPath>
      <Private>False</Private>
    </Reference>
    <Reference Include="GameOffsets">
      <HintPath>$(exapiPackage)\GameOffsets.dll</HintPath>
      <Private>False</Private>
    </Reference>
  </ItemGroup>
  <ItemGroup>
    <PackageReference Include="ImGui.NET" Version="1.90.0.1" />
    <PackageReference Include="Newtonsoft.Json" Version="13.0.3" />
    <PackageReference Include="SharpDX.Mathematics" Version="4.2.0" />
  </ItemGroup>
</Project>
```

### Plugin Lifecycle

| Method | When called | Notes |
|---|---|---|
| `Initialise()` | Once on load | Register hotkeys, wire up `OnPressed`/`OnValueChanged`, return `true` on success |
| `OnLoad()` | After Initialise | Load textures: `Graphics.InitImage("file.png")` |
| `AreaChange(AreaInstance area)` | Zone change | Clear cached entity lists here |
| `Tick()` | Every frame | Return `null` (no async job needed) or a `Job` for background work |
| `Render()` | Every frame | Draw overlays; check `Settings.Enable` and `GameController.InGame` |
| `EntityAdded(Entity entity)` | Entity enters range | Filter and cache relevant entities here |
| `EntityRemoved(Entity entity)` | Entity leaves range | Remove from caches |
| `DrawSettings()` | Settings panel open | Call `base.DrawSettings()` unless fully custom |

## GameController API

`GameController` is the main access point available in all plugin methods:

```csharp
// State checks
GameController.InGame
GameController.Game.IsInGameState

// Player
GameController.Player                          // local player Entity
GameController.Player.GetComponent<Positioned>().GridPosNum

// Area
GameController.Area.CurrentArea.IsPeaceful
GameController.Area.CurrentArea.Area.RawName
GameController.Area.CurrentArea.IsTown
GameController.Area.CurrentArea.IsHideout

// Entities — prefer ValidEntitiesByType over Entities for filtered access
GameController.EntityListWrapper.ValidEntitiesByType[EntityType.Monster]
GameController.Entities   // all entities, more expensive

// Ingame UI / state
GameController.Game.IngameState.IngameUi.Map.LargeMap.IsVisible
GameController.Game.IngameState.IngameUi.FullscreenPanels
GameController.Game.IngameState.IngameUi.OpenRightPanel
GameController.Game.IngameState.Camera        // Camera, WorldToScreen
GameController.Game.IngameState.Data          // terrain, server data, area dimensions

// Game files (static data)
GameController.Files.BaseItemTypes.Translate(entity.Path)
GameController.Files.GemEffects.GetById(id)

// Window
GameController.Window.GetWindowRectangleTimeCache   // cached, use this not GetWindowRectangle()

// Inter-plugin
GameController.PluginBridge.SaveMethod("MyPlugin.Method", delegate)
GameController.PluginBridge.GetMethod<TDelegate>("OtherPlugin.Method")

// Timing
GameController.DeltaTime  // double, seconds since last frame
```

## Entity API

```csharp
entity.Path         // "Metadata/Monsters/..."
entity.Metadata     // same as Path for most entities
entity.Id           // uint, unique per session
entity.IsValid      // always check before extensive use
entity.IsAlive
entity.GridPosNum   // Vector2, 2D grid position
entity.PosNum       // Vector3, 3D world position
entity.Distance(otherEntity)
entity.DistancePlayer  // float, distance to local player
entity.Type         // EntityType enum
entity.GetComponent<Positioned>()     // null if not present
entity.GetComponent<Render>()
entity.GetComponent<Stats>()
entity.GetComponent<ObjectMagicProperties>()?.Mods
```

## Component Quick Reference

### Life
```csharp
var life = entity.GetComponent<Life>();
life.CurHP / life.MaxHP      // health
life.CurMana / life.MaxMana  // mana
life.CurES / life.MaxES      // energy shield
life.HPPercentage             // 0-100
```

### Buffs
```csharp
var buffs = entity.GetComponent<Buffs>();
buffs.BuffsList               // List<Buff>
buff.Name                     // internal string ID (e.g., "frozen", "shocked")
buff.DisplayName              // human-readable
buff.Timer                    // seconds remaining
buff.MaxTime                  // original duration
buff.Charges                  // stack count
```

### Positioned & Render
```csharp
var pos = entity.GetComponent<Positioned>();
pos.GridPosNum                // Vector2 grid coords

var render = entity.GetComponent<Render>();
render.Name                   // display name
render.Bounds                 // bounding box
```

### ObjectMagicProperties
```csharp
var omp = entity.GetComponent<ObjectMagicProperties>();
omp.Rarity                    // MonsterRarity enum
omp.Mods                      // List<string> mod names
```

### Stats
```csharp
var stats = entity.GetComponent<Stats>();
stats.StatDictionary          // Dictionary<GameStat, int>
```

### Other Common Components
- `Monster` — monster-specific data
- `Player` — player-specific data
- `Chest` — chest state (IsOpened, IsStrongbox)
- `Portal` — portal destination
- `WorldItem` — ground item info
- `Targetable` — whether entity can be targeted (isTargetable)
- `Base` — item base type info (Name, ItemBaseName)
- `Mods` — item mods (ItemMods, ImplicitMods, ItemRarity)
- `Sockets` — socket links, colors, count

## Settings Node Types

```csharp
public class MySettings : ISettings
{
    public ToggleNode Enable { get; set; } = new ToggleNode(false);
    public RangeNode<int> SomeRange { get; set; } = new RangeNode<int>(10, 1, 100);
    public RangeNode<float> SomeFloat { get; set; } = new RangeNode<float>(1.5f, 0f, 10f);
    public ColorNode SomeColor { get; set; } = new ColorNode(Color.White);
    public HotkeyNodeV2 SomeKey { get; set; } = Keys.F5;
    // Methods:
    //   .IsPressed()    → bool, true while key is held down
    //   .PressedOnce()  → bool, true only on first frame of press (edge-triggered)
    //   .Value          → Keys enum value
    // Wire in Initialise():
    //   Input.RegisterKey(Settings.SomeKey.Value);
    //   Settings.SomeKey.OnValueChanged += () => Input.RegisterKey(Settings.SomeKey.Value);
    public ButtonNode SomeButton { get; set; }   // wire OnPressed in constructor
    public TextNode SomeText { get; set; } = new TextNode("default");
    public EmptyNode SomeHeader { get; set; }     // visual separator in settings

    [Menu("Display Name")]
    public ToggleNode WithLabel { get; set; } = new ToggleNode(true);

    [Menu("Category Header", parentIndex: 100)]
    public EmptyNode CategoryNode { get; set; }

    [Menu("Sub Option", parentIndex: 101, index: 100)]
    public ToggleNode SubOption { get; set; } = new ToggleNode(false);

    [IgnoreMenu]
    public SomeType NotShownInSettings { get; set; }
}

[Submenu]
public class NestedSection
{
    public ToggleNode SubOption { get; set; } = new ToggleNode(false);
}
```

Register hotkeys in `Initialise()`:
```csharp
Input.RegisterKey(Settings.SomeKey.Value);
Settings.SomeKey.OnValueChanged += () => Input.RegisterKey(Settings.SomeKey.Value);
```

## Graphics API

```csharp
// Rectangles — use System.Numerics.Vector2 for positions
Graphics.DrawBox(topLeft, bottomRight, color, rounding);
Graphics.DrawFrame(topLeft, bottomRight, color, borderWidth, segments, rounding);

// Text
Graphics.DrawText("text", position, color);
Graphics.DrawTextWithBackground("text", position, textColor, alignment, bgColor);
var size = Graphics.MeasureText("text");
using (Graphics.SetTextScale(1.5f)) { /* draw at scale */ }

// World / map lines
Graphics.DrawLineInWorld(gridPos1, gridPos2, width, color);
Graphics.DrawLineOnLargeMap(gridPos1, gridPos2, width, color);

// Circles
Graphics.DrawFilledCircleInWorld(worldPos, radius, color);

// Textures (load in OnLoad, draw in Render)
Graphics.InitImage("myimage.png");            // from plugin directory
Graphics.InitImage("key", fullPath);          // with explicit path
Graphics.DrawImage("key", rectF, color);      // RectangleF from SharpDX
var texId = Graphics.GetTextureId("key");     // IntPtr for ImGui
Graphics.HasImage("key");                     // check if loaded

// Camera projection
var screenPos = GameController.Game.IngameState.Camera.WorldToScreen(entity.PosNum);
```

## Performance Rules

ExileApi plugins run every frame (60+ fps). The game process is memory-mapped, so every property access that isn't cached may read from game memory.

- **NEVER** call `GetComponent<T>()` in `Render()` — do it in `Tick()`, store in fields
- **NEVER** iterate `GameController.Entities` — use `EntityListWrapper.ValidEntitiesByType`
- **NEVER** call `entity.Path` per-frame — cache in `EntityAdded()` via `SetHudComponent`
- Use `EntityAdded`/`EntityRemoved` to maintain filtered entity lists
- Use `TimeCache<T>` for expensive operations that don't need per-frame freshness
- Clear state in `AreaChange()`, not per-tick
- Use cached accessors (`GetWindowRectangleTimeCache`, `GetClientRectCache`)
- Check `entity.IsValid` and `entity.IsAlive` before processing
- Check `screenPos == Vector2.Zero` after `WorldToScreen()`
- Load textures in `OnLoad()`, never in `Render()`
- Separate data reads (`Tick`) from drawing (`Render`) — never GetComponent in Render
- Use `_canRender` flags to skip `Render()` entirely when nothing to draw
- Guard early in `Tick()`: InGame, IsAlive, area checks (town/hideout) before work

## Mandatory Safety Guards

Every plugin MUST implement these guards. They are not optional.

### Spawn Immunity (Grace Period)

No action should be taken during spawn immunity that would cause it to end. Skip all input sending, processing, and rendering during grace period:

```csharp
// In Tick(), after getting buffs list:
for (int i = 0; i < buffs.Count; i++)
{
    if (buffs[i].Name == "grace_period")
        return;
}
```

The buff name is `"grace_period"`. This covers the intangibility window after entering a zone. Any plugin that sends inputs (key presses, mouse clicks), triggers abilities, or renders overlays MUST check this. Sending input during grace period breaks spawn immunity and can kill the player.

### Input Safety (CanSendInput Pattern)

Any plugin that sends synthetic key presses MUST guard against sending input when the game cannot properly receive it. Use this pattern:

```csharp
private bool CanSendInput()
{
    try
    {
        if (!GameController.Window.IsForeground())
            return false;

        var ingameUi = GameController.IngameState.IngameUi;

        // Chat open — keystrokes would type into chat
        if (ingameUi.ChatPanel?.ChatTitlePanel?.IsVisible == true)
            return false;

        // Fullscreen panels (skill tree, atlas, syndicate board)
        if (ingameUi.FullscreenPanels.Any(x => x.IsVisible))
            return false;

        // Large panels (vendor, trade, map device, crafting bench)
        if (ingameUi.LargePanels.Any(x => x.IsVisible))
            return false;

        return true;
    }
    catch
    {
        return false;
    }
}
```

Always check `CanSendInput()` before any `Input.KeyPress` coroutine. Combine with a `DateTime _lastKeyPressAt` field and a configurable cooldown (e.g., `InputCooldownMs`) to prevent rapid-fire key spam.

### UI Panel Occlusion

No rendering should be done when a UI window is open that the overlay would interfere with. If the plugin cannot avoid rendering where the UI panel is displayed, skip rendering entirely:

```csharp
// In Render(), early guard:
var ingameUi = GameController.Game.IngameState.IngameUi;
if (ingameUi.FullscreenPanels.Any(x => x.IsVisible)) return;
if (ingameUi.LargePanels.Any(x => x.IsVisible)) return;
```

For plugins that render at fixed screen positions, also consider side panels:
```csharp
if (ingameUi.OpenLeftPanel.IsVisible) return;   // inventory etc.
if (ingameUi.OpenRightPanel.IsVisible) return;  // stash, maps etc.
```

Requires `using System.Linq;` for `.Any()`.

Panel types:
- **FullscreenPanels**: Skill tree, Atlas tree, Syndicate board — cover entire screen
- **LargePanels**: Vendor, Trade, Map Device, Crafting Bench, Ritual — large modal windows
- **OpenLeftPanel / OpenRightPanel**: Inventory, Stash — side panels

## C# Best Practices for Plugin Development

- Use file-scoped namespaces
- Seal leaf classes for virtual dispatch optimization
- Object-pool reusable instances to avoid per-frame GC pressure
- Use Dictionary/HashSet lookups instead of per-frame string comparisons
- Use switch expressions for tier/type mappings
- Use readonly fields for immutable configuration
- Any plugin with 4+ settings should use a dedicated `<PluginName>SettingsUi` class — see "Custom ImGui Settings UI" section for the full widget library

## Coordinate Systems

- **Grid** (`GridPosNum`, `Vector2`): tile-based map coordinates. Used for minimap drawing.
- **World** (`PosNum`, `Vector3`): 3D world coordinates. Convert to screen with `Camera.WorldToScreen`.
- **Screen** (`Vector2`): pixel coordinates. Used for ImGui and `Graphics.DrawText/DrawFrame`.
- GridToWorld multiplier: `250f / 23f`

## Input API

```csharp
// Mouse position — DIFFERENT coordinate spaces!
Input.MousePositionNum          // Vector2, WINDOW-relative (client area coords)
Input.ForceMousePositionNum     // Vector2, ABSOLUTE screen coords (set cursor)
Input.SetCursorPos(Vector2)     // expects ABSOLUTE screen coords

// Keyboard — reading state
Input.GetKeyState(Keys.X)       // true while held
Input.IsKeyDown(Keys.X)         // true while held (alias)
Input.RegisterKey(Keys.X)       // required before polling custom keys

// Keyboard — sending input (use sparingly — anti-cheat risk)
// ALWAYS use the coroutine pattern, NEVER raw KeyDown+KeyUp:
Core.ParallelRunner.Run(new Coroutine(Input.KeyPress(key), this, "MyPlugin.KeyPress"));
// Input.KeyPress is a coroutine that does KeyDown, yields 1ms, then KeyUp.
// This ensures the game registers the keypress across frames.
//
// WRONG — zero delay between down/up, game may not register it:
// Input.KeyDown(key);
// Input.KeyUp(key);
```

**WARNING**: `MousePositionNum` is window-relative. To convert to screen coords for `SetCursorPos`, add the window position from `GameController.Window.GetWindowRectangleTimeCache`.

## Common Patterns

### Entity Tracking
```csharp
// In EntityAdded — filter and cache
public override void EntityAdded(Entity entity)
{
    if (entity.Type == EntityType.Monster && entity.IsAlive)
        _trackedMonsters[entity.Id] = entity;
}

// In EntityRemoved — clean up
public override void EntityRemoved(Entity entity)
{
    _trackedMonsters.Remove(entity.Id);
}

// In AreaChange — clear all
public override void AreaChange(AreaInstance area)
{
    _trackedMonsters.Clear();
}
```

### Buff Monitoring
```csharp
var buffs = player.GetComponent<Buffs>()?.BuffsList;
if (buffs != null)
{
    for (int i = 0; i < buffs.Count; i++)
    {
        var buff = buffs[i];
        if (buff?.Name == "target_buff" && buff.Timer > 0)
        {
            // buff is active
        }
    }
}
```

### Rate-Limited Updates
```csharp
// Option 1: TimeCache — auto-invalidates after ms
private readonly TimeCache<List<MyData>> _cache;
_cache = new TimeCache<List<MyData>>(BuildData, 200); // refresh every 200ms

// Option 2: Stopwatch throttle
private readonly Stopwatch _timer = Stopwatch.StartNew();
if (_timer.ElapsedMilliseconds < 200) return null;
_timer.Restart();
```

### Edge Detection (Key Press / Release)
```csharp
private bool _wasKeyPressed;

// In Tick():
var pressed = Settings.SomeKey.IsPressed();
if (pressed && !_wasKeyPressed) { /* on press edge */ }
else if (!pressed && _wasKeyPressed) { /* on release edge */ }
_wasKeyPressed = pressed;
```

### Inter-Plugin Communication
```csharp
// Expose a method
GameController.PluginBridge.SaveMethod("MyPlugin.GetData", (Func<MyData>)GetData);

// Consume another plugin's method
var getData = GameController.PluginBridge.GetMethod<Func<OtherData>>("OtherPlugin.GetData");
if (getData != null) { var data = getData(); }

// Events
GameController.PluginBridge.PublishEvent("MyPlugin.SomethingHappened", eventData);
GameController.PluginBridge.ReceiveEvent<EventType>("OtherPlugin.Event", handler);
```

### Custom ImGui Settings UI

Any plugin with 4+ settings should use a dedicated `<PluginName>SettingsUi` class for a polished, tabbed settings panel instead of the default auto-generated tree.

**Hook pattern** — in your main plugin class:
```csharp
private <PluginName>SettingsUi _settingsUi;

public override void DrawSettings()
{
    _settingsUi ??= new <PluginName>SettingsUi();
    _settingsUi.Draw(Settings);
}
```

**Architecture** — the `SettingsUi` class is self-contained with:

1. **Color system** — unique accent per plugin + shared neutrals:
```csharp
private static uint Accent => Col(R, G, B);           // unique per plugin
private static uint AccentDim => Col(R, G, B);         // darker accent
private static uint AccentSoft => Col(R, G, B, 0.15f); // transparent accent
private static uint Label => Col(0.88f, 0.88f, 0.88f);
private static uint Desc => Col(0.38f, 0.40f, 0.44f);
private static uint TabOff => Col(0.48f, 0.48f, 0.52f);
private static uint CardBg => Col(0.05f, 0.05f, 0.07f, 1f);
private const float Row = 34f;   // vertical spacing per control row
```

2. **Color utilities**:
```csharp
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
```

3. **Tab bar pattern** — equal-width tabs with underline indicator:
```csharp
private int _activeTab;
private static readonly string[] Tabs = { "Tab1", "Tab2", "Tab3" };

// In Draw(): render tab bar at top of content region
float tabW = contentW / Tabs.Length;
for (int i = 0; i < Tabs.Length; i++)
{
    var tMin = new Vector2(contentMin.X + i * tabW, contentMin.Y);
    var tMax = new Vector2(contentMin.X + (i + 1) * tabW, contentMin.Y + 26f);
    ImGui.SetCursorScreenPos(tMin);
    ImGui.InvisibleButton($"##prefix_tab_{i}", tMax - tMin);
    if (ImGui.IsItemClicked()) _activeTab = i;
    if (i == _activeTab)
        dl.AddLine(tMin with { Y = tMax.Y - 1 } + new Vector2(3, 0),
            tMax - new Vector2(3, 1), Accent, 2f);
    CenterText(dl, Tabs[i], (tMin + tMax) * 0.5f,
        i == _activeTab ? Accent : TabOff);
}
```

4. **Content area** — dark card + pulsing border + scrollable child:
```csharp
// Size the content child to the remaining available height
var avail = new Vector2(contentW, ImGui.GetContentRegionAvail().Y);
ImGui.BeginChild("##prefix_content", avail, ImGuiChildFlags.None, ImGuiWindowFlags.None);
var cdl = ImGui.GetWindowDrawList();
var cMin = ImGui.GetWindowPos();
var cSz = ImGui.GetWindowSize();
cdl.AddRectFilled(cMin, cMin + cSz, CardBg, 3f);
float pulse = (float)(0.4 + 0.3 * Math.Sin(ImGui.GetTime() * 1.8));
cdl.AddRect(cMin, cMin + cSz, WithAlpha(Accent, pulse * 0.18f), 3f, ImDrawFlags.None, 1f);

// IMPORTANT: Subtract GetScrollY() so DrawList content scrolls with the child window.
float scrollY = ImGui.GetScrollY();
float y = cMin.Y + 10 - scrollY;
float x = cMin.X + 12;
float cx = cMin.X + cSz.X * 0.50f;  // control column at 50%
float sw = cSz.X * 0.40f;           // control width at 40%
// ... tab content (all rendering uses y, which scrolls) ...
ImGui.SetCursorScreenPos(new Vector2(x, y));  // advance ImGui cursor for content height
ImGui.Dummy(new Vector2(1, 4));               // register total content height for scrollbar
ImGui.EndChild();
```

5. **Widget primitives** — self-contained, ID-prefixed:

```csharp
// Animation state for smooth toggle transitions
private readonly Dictionary<string, float> _anims = new();

// ── PillToggle — animated 40x20 sliding toggle ──
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
    dl.AddCircleFilled(new Vector2(knobX, cur.Y + r), r - 2f,
        ImGui.GetColorU32(new Vector4(0.5f + 0.5f * animState, 0.5f + 0.5f * animState,
            0.5f + 0.5f * animState, 1f)));
    return changed;
}

// ── SliderInt — custom track + thumb slider ──
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
    dl.AddRectFilled(cur with { Y = trackY }, new Vector2(cur.X + f * width, trackY + th), Accent, 2f);
    float tx = cur.X + f * width, ty = cur.Y + h * 0.5f;
    dl.AddCircleFilled(new Vector2(tx, ty), tr, Accent);
    dl.AddCircleFilled(new Vector2(tx, ty), tr - 2f,
        ImGui.GetColorU32(new Vector4(0.95f, 0.95f, 0.95f, 1f)));
    return changed;
}
```

6. **Compound widgets** — label + description + control (two-column layout):

```csharp
// Toggle: label+desc on left, PillToggle on right at cx
private void Toggle(string key, ToggleNode node, ImDrawListPtr dl,
    float x, float cx, ref float y, string label, string desc)
{
    dl.AddText(new Vector2(x + 6, y + 1), Label, label);
    dl.AddText(new Vector2(x + 6, y + 16), Desc, desc);
    ImGui.SetCursorScreenPos(new Vector2(cx, y + 5));
    var v = node.Value;
    _anims.TryGetValue(key, out float a);
    if (PillToggle($"##{key}", ref v, ref a)) node.Value = v;
    _anims[key] = a;
    y += Row;
}

// IntSlider: label+desc on left, SliderInt on right
private void IntSlider(string key, RangeNode<int> node, ImDrawListPtr dl,
    float x, float cx, ref float y, float sw, string label, string desc)
{
    dl.AddText(new Vector2(x + 6, y + 1), Label, label);
    dl.AddText(new Vector2(x + 6, y + 16), Desc, desc);
    ImGui.SetCursorScreenPos(new Vector2(cx, y + 7));
    var v = node.Value;
    if (SliderInt($"##{key}", ref v, node.Min, node.Max, sw)) node.Value = v;
    dl.AddText(new Vector2(cx + sw + 6, y + 7), Accent, v.ToString());
    y += Row;
}

// ColorPicker: label+desc + ImGui.ColorEdit4 (single swatch — no custom DrawList swatch)
private void ColorPicker(string key, ColorNode node, ImDrawListPtr dl,
    float x, float cx, ref float y, string label, string desc)
{
    dl.AddText(new Vector2(x + 6, y + 1), Label, label);
    dl.AddText(new Vector2(x + 6, y + 16), Desc, desc);
    var c = node.Value;
    ImGui.SetCursorScreenPos(new Vector2(cx, y + 3));
    var colorVec = new Vector4(c.R / 255f, c.G / 255f, c.B / 255f, c.A / 255f);
    ImGui.SetNextItemWidth(120);
    if (ImGui.ColorEdit4($"##{key}", ref colorVec,
        ImGuiColorEditFlags.NoInputs | ImGuiColorEditFlags.NoLabel))
        node.Value = new SharpDX.Color(
            (int)(colorVec.X * 255), (int)(colorVec.Y * 255),
            (int)(colorVec.Z * 255), (int)(colorVec.W * 255));
    y += Row;
}

// SectionHeader: accent text + underline separator
private void SectionHeader(ImDrawListPtr dl, float x, ref float y, string title)
{
    y += 4f;
    dl.AddText(new Vector2(x, y), Accent, title);
    y += 18f;
    dl.AddLine(new Vector2(x, y - 4),
        new Vector2(x + ImGui.CalcTextSize(title).X + 40, y - 4),
        WithAlpha(Accent, 0.25f), 1f);
}
```

7. **HelpMarker `(?)` hints** — standard inline tooltip pattern:
```csharp
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
```

**ID prefix convention**: Use 2-3 letter prefix per plugin (e.g., `ua_` for UnboundAvatar) to avoid ImGui ID collisions.

### UI-First Feature Development

When adding a new feature, always design the settings UI experience in the same pass.

**Required UI enrichment for each feature type:**

| What you're adding | What the SettingsUi needs |
|---|---|
| New entity type or data source | Live count in a status bar + status line in the relevant tab |
| New color setting | Legend item showing a colored dot + what that color means in context |
| Bridge/integration (e.g., Radar) | Connection status indicator (green/red dot + status text) |
| Visual feature (arrows, circles, paths) | Preview widget showing what the in-game overlay will look like |
| New mechanic | Info block explaining how the mechanic works for users unfamiliar with it |

**Live runtime data pattern** — define a record to pass live state from `DrawSettings()`:
```csharp
public record MyPluginUiState(int EntityCount, bool BridgeConnected, ...);

// In DrawSettings():
_settingsUi.Draw(Settings, new MyPluginUiState(
    _tracked.Count(x => x.IsValid),
    _bridge != null
));
```

## Reference Plugins

Study other plugins in `Plugins/Source/` for pattern reference. Look at:
- Simple buff-tracking plugins for entity/buff patterns
- Minimap overlay plugins for coordinate system usage
- Settings-heavy plugins for ImGui custom UI patterns
