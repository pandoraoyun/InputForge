using Godot;

namespace InputForge.Modifiers;

/// <summary>
/// Reorders the axes of the input vector.
/// Useful when mapping a mouse axis to a character rotation axis,
/// e.g. swapping XY so that horizontal mouse movement drives vertical look.
/// </summary>
[GlobalClass]
public sealed partial class SwizzleModifier : InputModifier
{
    public enum SwizzleOrder
    {
        YXZ, // Swap X and Y — most common use case
        XZY, // Swap Y and Z
        ZYX, // Swap X and Z
        ZXY,
        YZX
    }

    [Export] public SwizzleOrder Order { get; set; } = SwizzleOrder.YXZ;

    public override Vector3 Apply(Vector3 value) => Order switch
    {
        SwizzleOrder.YXZ => new Vector3(value.Y, value.X, value.Z),
        SwizzleOrder.XZY => new Vector3(value.X, value.Z, value.Y),
        SwizzleOrder.ZYX => new Vector3(value.Z, value.Y, value.X),
        SwizzleOrder.ZXY => new Vector3(value.Z, value.X, value.Y),
        SwizzleOrder.YZX => new Vector3(value.Y, value.Z, value.X),
        _                => value
    };
}
