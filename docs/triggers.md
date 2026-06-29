# Triggers Reference

Triggers decide whether a processed value should cause an action to fire. They are evaluated **after** all modifiers have been applied.

Assign triggers to an `InputMapping` via the `LocalTriggers` array in the Inspector. Multiple triggers use **OR logic** — if any trigger returns true, the action fires.

## Default Triggers

When `LocalTriggers` is empty, a default trigger is selected based on `InputType`:

| InputType          | Default trigger   | Behaviour                        |
|--------------------|-------------------|----------------------------------|
| `Boolean`          | `TriggerOnKeyDown`| Fires on the first press frame   |
| `Digital`, `Analog`, `Delta` | `TriggerOnChange` | Fires whenever the value changes |

---

## TriggerOnKeyDown

Fires **once** on the rising edge — the first frame the value becomes non-zero. Does not fire while the input is held or when it is released.

No configurable properties.

Use for: jump, attack, interact — anything that should happen exactly once per press.

---

## TriggerOnKeyUp

Fires **once** on the falling edge — the first frame the value returns to zero.

No configurable properties.

Use for: charged attacks (release to fire), toggle-on-release, hold-and-release mechanics.

---

## TriggerOnChange

Fires whenever the value differs from the previous event's value. This includes both transitions to non-zero and back to zero.

No configurable properties.

This is the default for axis mappings because it ensures that releasing a key sends a zero value to the subscriber, correctly stopping movement or resetting velocity.

---

## TriggerContinuous

Fires every event while the value is non-zero.

No configurable properties.

Use for: actions that should repeat while a button is held. Note that for Digital input, OS key-repeat events are filtered by `InputKey`, so this trigger will fire on the first press but not on subsequent frames unless the value changes. For true frame-rate continuous input, consider polling `Input.IsKeyPressed` in `_Process` instead.

---

## Writing a Custom Trigger

Extend `InputTrigger` and override `Evaluate`:

```csharp
using Godot;
using InputForge.Triggers;

/// <summary>Fires only when the value exceeds a threshold.</summary>
[GlobalClass]
public sealed partial class TriggerOnThreshold : InputTrigger
{
    [Export] public float Threshold { get; set; } = 0.8f;

    public override bool Evaluate(Vector3 value, InputEvent @event)
        => value.Length() >= Threshold;
}
```
