# What's an Unbound Avatar?

> Auto-activate Avatar of the Wilds at 100 fury stacks when the right enemies are nearby.

Part of the **WhatsA** plugin family for ExileApi.

## What It Does

- Monitors fury stack accumulation and auto-activates the Avatar of the Wilds keystone at 100 stacks
- Only triggers when configurable enemy conditions are met (unique nearby, rare pack threshold, or always)
- Displays a progress bar showing fury stack count with color-coded state (charging / ready / active / cooldown)

## Getting Started

1. Download and place in `Plugins/Source/What's an Unbound Avatar/`
2. HUD auto-compiles on next launch
3. Enable in plugin list
4. Set the **Skill Key** to match your in-game keybind for the Avatar skill
5. Configure trigger conditions in the Triggers tab

## Settings

| Setting | Default | Description |
|---------|---------|-------------|
| **Skill Key** | Mouse 4 | Key bound to Avatar of the Wilds in-game |
| **Activate on Unique** | On | Auto-activate when a Unique enemy is within range |
| **Activate on Rare Count** | On | Auto-activate when N+ rare/unique enemies are nearby |
| Rare Count Threshold | 3 | Minimum rare+ enemy count to trigger |
| **Activate Always** | Off | Activate whenever ready, regardless of enemies |
| Activation Range | 80 | Grid-unit radius for enemy scanning |
| Input Cooldown | 500ms | Minimum time between key presses |
| **Show HUD** | On | Display the fury progress bar |
| HUD Position X / Y | 500 / 36 | Progress bar screen position |
| Bar Width / Height | 220 / 22 | Progress bar dimensions |

<details>
<summary>Technical Details</summary>

### State Machine

The plugin tracks four states via buff detection:

1. **Charging** -- Accumulating fury stacks (0-99). Green-to-yellow gradient bar.
2. **Ready** -- 100 stacks reached. Lime green bar. Waiting for enemy conditions.
3. **Active** -- Skill active (10s duration). Orange bar with countdown.
4. **Cooldown** -- Buff expired. Blue bar. Waiting for stacks to rebuild.

### Buff Detection

- Fury stacks: `ailment_bearer_activation_buff` (charges = stack count)
- Active effect: `ailment_bearer_buff` (timer = remaining duration)

### Safety Guards

- Skips activation during grace period (spawn immunity)
- `CanSendInput()` check prevents input when game window is unfocused or chat is open
- Multiple trigger conditions are OR'd -- any matching condition activates
- Input cooldown prevents key press spam

### Architecture

- Enemy scanning iterates `ValidEntitiesByType[EntityType.Monster]`, counting alive/hostile within range
- Separates unique from rare counts for independent trigger evaluation
- Custom ImGui settings UI with 3 tabs (Status, Triggers, HUD) and electric blue accent
- Live state display shows current fury stacks, active duration, and nearby enemy counts

</details>

## About This Project

These plugins are built with AI-assisted development using Claude Code and the
ExileApiScaffolding (private development workspace) workspace.

The developer works professionally in cybersecurity and high-risk software --
AI compensates for a C# knowledge gap specifically, not engineering judgment.
Plugin data comes from the PoE Wiki and PoEDB data mining.

The focus is on UX: friction points and missing expected features that the
existing plugin ecosystem doesn't address. Every hour spent developing is an
hour not spent on league progression, so feedback is the best way to support
the project.

## WhatsA Plugin Family

| Plugin | Description |
|--------|-------------|
| [What's a Blade Vortex?](https://github.com/ParogDev/WhatsABladeVortex) | Blade Vortex stack tracker with Minion Pact snapshot detection |
| [What's a Breakpoint?](https://github.com/ParogDev/WhatsABreakpoint) | Kinetic Fusillade attack speed breakpoint visualizer |
| [What's a Crowd Control?](https://github.com/ParogDev/WhatsACrowdControl) | OmniCC-style CC effect overlay with timers |
| [What's a Mirage?](https://github.com/ParogDev/WhatsAMirage) | League mechanic overlay for spawners, chests, and wishes |
| [What's a Tincture?](https://github.com/ParogDev/WhatsATincture) | Automated tincture management with burn stack tracking |
| [What's a Tooltip?](https://github.com/ParogDev/WhatsATooltip) | Shared rich tooltip service for WhatsA plugins |
| [What's an AI Bridge?](https://github.com/ParogDev/WhatsAnAiBridge) | File-based IPC for AI-assisted plugin development |
| **What's an Unbound Avatar?** | Auto-activation for Avatar of the Wilds at 100 fury |

Built with ExileApiScaffolding (private development workspace)
