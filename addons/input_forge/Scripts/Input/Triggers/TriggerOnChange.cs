using Godot;

namespace InputForge.Triggers;

/// <summary>
/// Fires whenever the value differs from the previous event's value.
/// This is the default trigger for axis (Digital, Analog, Delta) input mappings.
/// Ensures that both non-zero and zero values are forwarded to subscribers,
/// so velocity is correctly reset when a key is released.
/// </summary>
[GlobalClass]
public sealed partial class TriggerOnChange : InputTrigger
{
    private Vector3 _previousValue = Vector3.Zero;

    public override bool Evaluate(Vector3 value, InputEvent @event)
    {
        bool changed = value != _previousValue;
        _previousValue = value;
        return changed;
    }

    public override void Reset() => _previousValue = Vector3.Zero;
}
