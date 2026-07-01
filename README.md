# InputForge

A UE5-inspired Enhanced Input System for Godot 4 C#. Context-based input mapping with a modifier and trigger pipeline — configured entirely via the Inspector, no custom editor UI required.

> Pure C# · Inspector-based · Zero Godot InputMap dependency · Godot 4.x

---

## Why InputForge?

Godot's built-in input system hardcodes action names as strings. The same physical key always maps to the same action — there is no built-in way to have `Space` mean `Jump` in gameplay and `Confirm` in a menu without managing that yourself.

InputForge solves this with **input contexts**: a stack of mappings that can be pushed and popped at runtime. The same key can mean different things in different contexts, and the topmost context always wins.

---

## Features

- **Context stack** — push/pop `InputMappingContext` resources at runtime
- **Unified `InputKey`** — keyboard, mouse, gamepad buttons and axes in one Inspector-friendly resource
- **Modifier pipeline** — transform raw values before they reach your code (deadzone, invert, normalize, scale, swizzle)
- **Trigger pipeline** — control when an action fires (on press, on release, on change, continuous)
- **Type-safe callbacks** — bind `Action<bool>`, `Action<float>`, `Action<Vector2>`, or `Action<Vector3>` — no casting
- **No Godot InputMap dependency** — bypasses hardcoded action strings entirely

---

## Installation

### Option A — Godot Asset Library

Search for **InputForge** in the Godot editor's AssetLib tab and click Install.

### Option B — degit (recommended for version-controlled projects)

If you have [Node.js](https://nodejs.org) installed, use [degit](https://github.com/Rich-Harris/degit) to pull only the plugin folder — no git history, no extra files:

```bash
npx degit pandoraoyun/InputForge/addons/input_forge addons/input_forge
```

Run the same command again to update to the latest version.

### Option C — Manual

Copy the `addons/input_forge` folder into your project's `addons/` directory.

---

After installation, enable the plugin in Godot: **Project → Project Settings → Plugins → InputForge**.

`EnhancedInputSystem` is registered as an autoload automatically.

---

## Quick Start

### 1. Create an InputAction resource

In the FileSystem panel, right-click → **New Resource** → `InputAction`. Set `ActionName` to `"Jump"`.

### 2. Create an InputKey resource

Right-click → **New Resource** → `InputKey`. Set:
- `InputType` → `Boolean`
- `DeviceType` → `Keyboard`
- `KeyboardKey` → `Space`

### 3. Create an InputMapping resource

Right-click → **New Resource** → `InputMapping`. Assign:
- `TargetAction` → your `Jump` action
- `InputSource` → your `InputKey`

### 4. Create an InputMappingContext resource

Right-click → **New Resource** → `InputMappingContext`. Add your `InputMapping` to the `Mappings` array.

### 5. Subscribe in code

```csharp
[Export] public InputMappingContext GameplayContext { get; set; }
[Export] public InputAction JumpAction { get; set; }

public override void _Ready()
{
    EnhancedInputSystem.GetInstance().AddContext(GameplayContext);
    GameplayContext.BindAction(JumpAction, OnJump);
}

private void OnJump(bool pressed)
{
    if (!pressed) return;
    GD.Print("Jumped!");
}

public override void _ExitTree()
{
    EnhancedInputSystem.GetInstance().RemoveContext(GameplayContext);
    GameplayContext.UnbindAction(JumpAction, OnJump);
}
```

---

## Benchmark / Smoke

`InputForge.Tests` includes throughput/stability smoke tests (correctness-only, no hard timing assertions — timing is logged for humans to read, not used as a CI gate).

**Subscriber count × event count** — dispatch cost stays well below linear as subscriber count grows:

| subscribers | events | ms/event | ms/invocation |
|---|---|---|---|
| 1 | 1000 | 0.00045 | 0.000890 |
| 50 | 1000 | 0.00081 | 0.000032 |
| 500 | 1000 | 0.00308 | 0.000012 |

**InputForge vs. raw `_Input()` override** — same simple logic expression, two architectures, fixed 300-event burst:

| handlers | InputForge ms/handler | Raw `_Input()` ms/handler | ratio (Raw/Forge) |
|---|---|---|---|
| 10 | 0.019260 | 0.028570 | 1.48x |
| 100 | 0.002632 | 0.010652 | 4.05x |
| 500 | 0.001606 | 0.010850 | 6.76x |

InputForge pays Godot's native `_Input()` dispatch cost once per event regardless of subscriber count, then fans out in-process. N separate `_Input()` overrides pay that dispatch cost N times per event — so the gap widens as handler count grows. Note the raw column here doesn't even include Godot's own native per-node dispatch/marshalling overhead, so this is a lower bound in InputForge's favor.

Full tables, methodology, and caveats: [`InputForge.Tests/SMOKE_BENCH.md`](InputForge.Tests/SMOKE_BENCH.md). Test source: [`InputForge.Tests/System/InputThroughputSmokeTests.cs`](InputForge.Tests/System/InputThroughputSmokeTests.cs).

**Coverage:** the addon assembly sits at ~84% line coverage, measured against a real headless Godot runtime. See [`InputForge.Tests/README.md`](InputForge.Tests/README.md#coverage) for how that figure is computed and why an unfiltered run reports much lower (Godot's generated marshalling code inflates the raw number).

---

## Documentation

- [Architecture Overview](docs/architecture.md)
- [InputKey Reference](docs/input-key.md)
- [Modifiers Reference](docs/modifiers.md)
- [Triggers Reference](docs/triggers.md)
- [Context Stack](docs/context-stack.md)
- [Extending InputForge](docs/extending.md)

---

## AI Use Disclosure

InputForge is human-designed and human-maintained; AI tools were used as a reviewed
assistant for tests, docs, and tooling. See [AI_DISCLOSURE.md](AI_DISCLOSURE.md).

---

## License

MIT
