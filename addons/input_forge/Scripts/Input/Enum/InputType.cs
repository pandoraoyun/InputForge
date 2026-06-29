namespace InputForge.Enum;

/// <summary>
/// Defines the input category for an <see cref="InputForge.Mappings.InputKey"/>.
/// Determines how the raw hardware event is interpreted and what value shape is produced.
/// </summary>
public enum InputType
{
    /// <summary>Keyboard key, gamepad button, or mouse button. Produces Vector3(1 or 0, 0, 0).</summary>
    Boolean,

    /// <summary>Two keyboard keys acting as a signed axis (e.g. A/D). Produces Vector3(x, y, 0).</summary>
    Digital,

    /// <summary>Analog joystick axis. Produces Vector3(x, y, 0).</summary>
    Analog,

    /// <summary>Mouse or gyro motion delta. Produces Vector3(x, y, 0).</summary>
    Delta
}
