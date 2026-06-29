namespace InputForge.Enum;

/// <summary>
/// The physical device type for a Boolean <see cref="InputForge.Mappings.InputKey"/>.
/// Controls which Inspector fields are shown and which Godot event type is matched.
/// </summary>
public enum InputDeviceType
{
    Keyboard,
    MouseButton,
    JoyButton,

    // NOT PLANNED SOON — the following types are not yet supported
    // and will be addressed in a future release.
    Gyro,
    Gesture
}
