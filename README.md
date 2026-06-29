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

1. Copy the `addons/input_forge` folder into your project's `addons/` directory.
2. In Godot: **Project → Project Settings → Plugins** and enable **InputForge**.
3. Add `EnhancedInputSystem` as an autoload: **Project → Project Settings → Autoload**, point it to `addons/input_forge/Scripts/Input/EnhancedInputSystem.cs`.

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

## Documentation

- [Architecture Overview](docs/architecture.md)
- [InputKey Reference](docs/input-key.md)
- [Modifiers Reference](docs/modifiers.md)
- [Triggers Reference](docs/triggers.md)
- [Context Stack](docs/context-stack.md)
- [Extending InputForge](docs/extending.md)

---

## License

MIT
