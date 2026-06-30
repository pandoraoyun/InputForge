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

    /// <summary>Mouse or gyro motion delta — frame-to-frame relative movement. Produces Vector3(x, y, 0).</summary>
    Delta,

    /// <summary>
    /// Absolute mouse cursor position in viewport space. Conceptually distinct from Delta —
    /// it is not derived from the motion event's relative value at all. A mouse motion event
    /// is still required to trigger the read (this stays event-driven, not polled every frame),
    /// but the value itself comes directly from <see cref="Godot.Input.GetMousePosition"/>,
    /// ignoring the event's own relative/position data entirely. Produces Vector3(x, y, 0).
    /// </summary>
    Pointer
}
