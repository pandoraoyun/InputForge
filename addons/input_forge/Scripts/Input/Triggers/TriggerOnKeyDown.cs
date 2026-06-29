using Godot;

namespace InputForge.Triggers;

/// <summary>
/// Fires once on the rising edge — the first frame the value becomes active (non-zero).
/// Subsequent frames while the input is held do not fire.
/// This is the default trigger for Boolean input mappings.
/// </summary>
[GlobalClass]
public sealed partial class TriggerOnKeyDown : InputTrigger
{
    private bool _previousActive;

    public override bool Evaluate(Vector3 value, InputEvent @event)
    {
        bool currentActive = value.Length() > 0f;
        bool risingEdge = currentActive && !_previousActive;
        _previousActive = currentActive;
        return risingEdge;
    }
}
