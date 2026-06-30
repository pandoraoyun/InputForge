using Godot;

namespace InputForge.Triggers;

/// <summary>
/// Fires once on the falling edge — the first frame the value returns to zero.
/// Useful for detecting button release, e.g. charging attacks or hold-to-aim.
/// </summary>
[GlobalClass]
public sealed partial class TriggerOnKeyUp : InputTrigger
{
    private bool _previousActive;

    public override bool Evaluate(Vector3 value, InputEvent @event)
    {
        bool currentActive = value.Length() > 0f;
        bool fallingEdge = !currentActive && _previousActive;
        _previousActive = currentActive;
        return fallingEdge;
    }

    public override void Reset() => _previousActive = false;
}
