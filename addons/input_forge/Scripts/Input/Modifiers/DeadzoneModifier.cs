using Godot;

namespace InputForge.Modifiers;

/// <summary>
/// Returns <see cref="Godot.Vector3.Zero"/> when the value's length is below the deadzone threshold.
/// Use this to eliminate analog stick drift and unintentional small inputs.
/// Place before other modifiers in the pipeline.
/// </summary>
[GlobalClass]
public sealed partial class DeadzoneModifier : InputModifier
{
    [Export] public float Deadzone { get; set; } = 0.2f;

    public override Vector3 Apply(Vector3 value)
        => value.Length() < Deadzone ? Vector3.Zero : value;
}
