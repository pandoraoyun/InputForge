namespace InputForge.Enum;

/// <summary>
/// Controls how many axes an <see cref="InputForge.Mappings.InputKey"/> produces
/// for Digital, Analog, and Delta input types.
/// </summary>
public enum AxisDimension
{
    /// <summary>Single axis. Produces Vector3(x, 0, 0).</summary>
    Axis1D,

    /// <summary>Two axes. Produces Vector3(x, y, 0).</summary>
    Axis2D
}
