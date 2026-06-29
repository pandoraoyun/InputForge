using Godot;

namespace InputForge.Triggers;

/// <summary>
/// Fires every event while the value is non-zero.
/// Use for actions that should repeat as long as a button is held,
/// such as sprinting, charging, or continuous fire.
/// Note: for Digital input, OS key-repeat events are filtered by
/// <see cref="InputForge.Mappings.InputKey"/>, so this trigger
/// will not fire on held keys unless the axis value changes each frame.
/// Consider pairing with a polling-based approach for frame-perfect continuous input.
/// </summary>
[GlobalClass]
public sealed partial class TriggerContinuous : InputTrigger
{
    public override bool Evaluate(Vector3 value, InputEvent @event)
        => value.Length() > 0f;
}
