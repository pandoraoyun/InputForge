using Godot;
using InputForge.Enum;

namespace InputForge.Mappings;

/// <summary>
/// Unified input source that captures keyboard, mouse, gamepad buttons and axes.
/// Configured entirely via the Inspector — no code required for common setups.
/// Produces a <see cref="Godot.Vector3"/> value that flows through the modifier
/// and trigger pipeline before reaching the subscriber callback.
///
/// Boolean input  → Vector3(1 or 0, 0, 0)
/// Axis1D input   → Vector3(value, 0, 0)
/// Axis2D input   → Vector3(x, y, 0)
///
/// This type is split across partial files:
///   InputKey.cs            — fields, exported properties, captured value
///   InputKey.ToolLogic.cs  — editor/Inspector concerns (_ValidateProperty visibility)
///   InputKey.Logic.cs      — runtime input handling + binding identity (Equals/GetHashCode)
/// </summary>
[Tool]
[GlobalClass]
public partial class InputKey : Resource
{
    private InputType _inputType = InputType.Boolean;
    private InputDeviceType _deviceType = InputDeviceType.Keyboard;
    private AxisDimension _axisDimension = AxisDimension.Axis1D;

    /// <summary>Selects the input category: button, digital axis, analog stick, mouse delta, or absolute pointer position.</summary>
    [Export]
    public InputType InputType
    {
        get => _inputType;
        set { _inputType = value; NotifyPropertyListChanged(); }
    }

    // Boolean fields — visible only when InputType == Boolean.
    /// <summary>The physical device type for boolean input.</summary>
    [Export]
    public InputDeviceType DeviceType
    {
        get => _deviceType;
        set { _deviceType = value; NotifyPropertyListChanged(); }
    }

    [Export] public Key KeyboardKey { get; set; } = Key.None;
    [Export] public JoyButton GamepadButton { get; set; } = JoyButton.Invalid;
    [Export] public MouseButton MouseKey { get; set; } = MouseButton.None;

    // Axis fields — visible only when InputType != Boolean.
    /// <summary>Whether this axis produces a 1D (X only) or 2D (XY) value.</summary>
    [Export]
    public AxisDimension AxisDimension
    {
        get => _axisDimension;
        set { _axisDimension = value; NotifyPropertyListChanged(); }
    }

    // Digital axis keys.
    [Export] public Key PositiveKey  { get; set; } = Key.D;
    [Export] public Key NegativeKey  { get; set; } = Key.A;
    [Export] public Key PositiveKeyY { get; set; } = Key.S;
    [Export] public Key NegativeKeyY { get; set; } = Key.W;

    // Analog joystick axes. Deadzone is handled by DeadzoneModifier.
    [Export] public JoyAxis JoystickAxis  { get; set; } = JoyAxis.LeftX;
    [Export] public JoyAxis JoystickAxisY { get; set; } = JoyAxis.LeftY;

    // Delta (mouse motion) fields.
    [Export] public float Sensitivity { get; set; } = 0.1f;
    /// <summary>When true, reads the Y axis of mouse motion instead of X (Axis1D only).</summary>
    [Export] public bool IsYAxis { get; set; } = false;

    private Vector3 _currentValue = Vector3.Zero;

    /// <summary>Returns the last captured raw value before modifiers are applied.</summary>
    public Vector3 GetValue() => _currentValue;
}
