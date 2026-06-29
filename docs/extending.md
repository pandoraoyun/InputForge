# Extending InputForge

InputForge is designed to be extended. The three main extension points are custom modifiers, custom triggers, and custom input sources.

## Custom Modifiers

Extend `InputModifier`, override `Apply`, and mark the class with `[GlobalClass]` so it appears in the Inspector.

```csharp
using Godot;
using InputForge.Modifiers;

/// <summary>Applies an exponential curve to the input value.</summary>
[GlobalClass]
public sealed partial class ExponentialCurveModifier : InputModifier
{
    [Export] public float Exponent { get; set; } = 2.0f;

    public override Vector3 Apply(Vector3 value)
    {
        float sign = Mathf.Sign(value.X);
        return new Vector3(sign * Mathf.Pow(Mathf.Abs(value.X), Exponent), value.Y, value.Z);
    }
}
```

## Custom Triggers

Extend `InputTrigger`, override `Evaluate`, and mark with `[GlobalClass]`.

```csharp
using Godot;
using InputForge.Triggers;

/// <summary>Fires after the value has been non-zero for a minimum duration.</summary>
[GlobalClass]
public sealed partial class TriggerOnHold : InputTrigger
{
    [Export] public float HoldTime { get; set; } = 0.5f;

    private float _heldFor = 0f;

    public override bool Evaluate(Vector3 value, InputEvent @event)
    {
        if (value.Length() > 0f)
        {
            _heldFor += (float)Engine.GetProcessDelta();
            if (_heldFor >= HoldTime)
            {
                _heldFor = 0f;
                return true;
            }
        }
        else
        {
            _heldFor = 0f;
        }
        return false;
    }
}
```

Note: `InputEvent` does not carry delta time. For time-based triggers, use `Engine.GetProcessDelta()` or track time externally.

## Multiple Bindings for One Action

You can bind the same action to multiple keys by adding multiple `InputMapping` entries to a context, all targeting the same `InputAction`:

```
InputMappingContext
  ├── InputMapping (Space → Jump)
  ├── InputMapping (GamepadSouth → Jump)   ← same action, different key
  └── InputMapping (WASD → Move)
```

Both mappings fire the same subscriber callbacks. The first one to match wins for that event.

## Sharing Contexts Across Scenes

`InputMappingContext` is a `Resource`. In Godot, resources saved to disk are shared by reference unless explicitly duplicated. This means you can define your contexts as `.tres` files and reuse them across multiple scenes — useful for consistent control schemes across levels or characters.

If you need per-instance state (e.g. different sensitivity per player), call `.Duplicate()` on the resource before adding it to the system.

## Runtime Rebinding

InputForge does not currently include a built-in rebinding UI. To rebind a key at runtime, update the `InputKey` resource's properties directly:

```csharp
myInputKey.KeyboardKey = Key.E;
```

Because `InputKey` is a `Resource`, changing it affects all mappings that reference the same resource instance. If you need per-player bindings, duplicate the resource first.
