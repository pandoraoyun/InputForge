using Godot;

namespace InputForge.Modifiers;

/// <summary>
/// Negates the selected axes of the input value.
/// Useful for inverting joystick Y-axis or reversing scroll direction.
/// </summary>
[GlobalClass]
public sealed partial class InvertModifier : InputModifier
{
    [Export] public bool InvertX { get; set; } = true;
    [Export] public bool InvertY { get; set; } = false;
    [Export] public bool InvertZ { get; set; } = false;

    public override Vector3 Apply(Vector3 value)
        => new(
            InvertX ? -value.X : value.X,
            InvertY ? -value.Y : value.Y,
            InvertZ ? -value.Z : value.Z
        );
}
