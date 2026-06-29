using Godot;

namespace InputForge.Modifiers;

/// <summary>
/// Normalizes the input vector to unit length (magnitude 1).
/// Use on Digital Axis2D mappings to prevent diagonal movement being faster than cardinal.
/// Values below <see cref="MinValueThreshold"/> are clamped to zero to avoid
/// near-zero vectors producing unexpected directions on release.
/// </summary>
[GlobalClass]
public sealed partial class NormalizeModifier : InputModifier
{
    /// <summary>Values with squared length below this threshold are treated as zero.</summary>
    [Export] public float MinValueThreshold { get; set; } = 0.001f;

    public override Vector3 Apply(Vector3 value)
        => value.LengthSquared() < MinValueThreshold * MinValueThreshold
            ? Vector3.Zero
            : value.Normalized();
}
