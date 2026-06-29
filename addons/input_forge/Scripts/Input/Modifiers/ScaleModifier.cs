using Godot;

namespace InputForge.Modifiers;

/// <summary>
/// Multiplies the input value per axis.
/// Use for sensitivity scaling or axis weighting.
/// </summary>
[GlobalClass]
public sealed partial class ScaleModifier : InputModifier
{
    [Export] public Vector3 Scale { get; set; } = Vector3.One;

    public override Vector3 Apply(Vector3 value) => value * Scale;
}
