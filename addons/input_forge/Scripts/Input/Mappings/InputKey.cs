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
/// </summary>
[Tool]
[GlobalClass]
public partial class InputKey : Resource
{
    private InputType _inputType = InputType.Boolean;
    private InputDeviceType _deviceType = InputDeviceType.Keyboard;
    private AxisDimension _axisDimension = AxisDimension.Axis1D;

    /// <summary>Selects the input category: button, digital axis, analog stick, or mouse delta.</summary>
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

    public override void _ValidateProperty(Godot.Collections.Dictionary property)
    {
        string name = property["name"].AsString();
        bool isBoolean = _inputType == InputType.Boolean;
        bool isDigital = _inputType == InputType.Digital;
        bool isAnalog  = _inputType == InputType.Analog;
        bool isDelta   = _inputType == InputType.Delta;
        bool is2D      = _axisDimension == AxisDimension.Axis2D;

        if (name == nameof(DeviceType)    && !isBoolean) Hide(property);
        if (name == nameof(KeyboardKey)   && !(isBoolean && _deviceType == InputDeviceType.Keyboard))    Hide(property);
        if (name == nameof(GamepadButton) && !(isBoolean && _deviceType == InputDeviceType.JoyButton))   Hide(property);
        if (name == nameof(MouseKey)      && !(isBoolean && _deviceType == InputDeviceType.MouseButton)) Hide(property);

        if (name == nameof(AxisDimension) && isBoolean) Hide(property);

        if (name == nameof(PositiveKey)  && !isDigital) Hide(property);
        if (name == nameof(NegativeKey)  && !isDigital) Hide(property);
        if ((name == nameof(PositiveKeyY) || name == nameof(NegativeKeyY)) && !(isDigital && is2D)) Hide(property);

        if (name == nameof(JoystickAxis)  && !isAnalog) Hide(property);
        if (name == nameof(JoystickAxisY) && !(isAnalog && is2D)) Hide(property);

        if (name == nameof(Sensitivity) && !isDelta) Hide(property);
        if (name == nameof(IsYAxis)     && !(isDelta && !is2D)) Hide(property);
    }

    private static void Hide(Godot.Collections.Dictionary property)
        => property["usage"] = (int)PropertyUsageFlags.NoEditor;

    /// <summary>
    /// Processes the incoming Godot <see cref="InputEvent"/> and updates the internal value.
    /// Returns true if the event matched this key's configuration.
    /// </summary>
    public bool HandleInput(InputEvent @event)
    {
        if (Engine.IsEditorHint()) return false;

        return _inputType switch
        {
            InputType.Boolean => HandleBoolean(@event),
            InputType.Digital => HandleDigital(@event),
            InputType.Analog  => HandleAnalog(@event),
            InputType.Delta   => HandleDelta(@event),
            _ => false
        };
    }

    private bool HandleBoolean(InputEvent @event)
    {
        switch (_deviceType)
        {
            case InputDeviceType.Keyboard:
                if (@event is InputEventKey keyEvent && keyEvent.Keycode == KeyboardKey)
                {
                    if (keyEvent.IsEcho()) return false;
                    _currentValue = new Vector3(keyEvent.Pressed ? 1f : 0f, 0f, 0f);
                    return true;
                }
                break;

            case InputDeviceType.JoyButton:
                if (@event is InputEventJoypadButton joyEvent && joyEvent.ButtonIndex == GamepadButton)
                {
                    _currentValue = new Vector3(joyEvent.Pressed ? 1f : 0f, 0f, 0f);
                    return true;
                }
                break;

            case InputDeviceType.MouseButton:
                if (@event is InputEventMouseButton mouseEvent && mouseEvent.ButtonIndex == MouseKey)
                {
                    _currentValue = new Vector3(mouseEvent.Pressed ? 1f : 0f, 0f, 0f);
                    return true;
                }
                break;
        }
        return false;
    }

    private bool HandleDigital(InputEvent @event)
    {
        if (@event is not InputEventKey) return false;

        float x = (Godot.Input.IsKeyPressed(PositiveKey) ? 1f : 0f)
                - (Godot.Input.IsKeyPressed(NegativeKey) ? 1f : 0f);

        float y = _axisDimension == AxisDimension.Axis2D
            ? (Godot.Input.IsKeyPressed(PositiveKeyY) ? 1f : 0f)
              - (Godot.Input.IsKeyPressed(NegativeKeyY) ? 1f : 0f)
            : 0f;

        _currentValue = new Vector3(x, y, 0f);
        return true;
    }

    private bool HandleAnalog(InputEvent @event)
    {
        if (@event is not InputEventJoypadMotion joyMotion) return false;

        bool relevantAxis = joyMotion.Axis == JoystickAxis
            || (_axisDimension == AxisDimension.Axis2D && joyMotion.Axis == JoystickAxisY);

        if (!relevantAxis) return false;

        float x = Godot.Input.GetJoyAxis(joyMotion.Device, JoystickAxis);
        float y = _axisDimension == AxisDimension.Axis2D
            ? Godot.Input.GetJoyAxis(joyMotion.Device, JoystickAxisY)
            : 0f;

        // Deadzone filtering is delegated to DeadzoneModifier.
        _currentValue = new Vector3(x, y, 0f);
        return true;
    }

    private bool HandleDelta(InputEvent @event)
    {
        if (@event is not InputEventMouseMotion mouseMotion) return false;

        if (_axisDimension == AxisDimension.Axis2D)
        {
            _currentValue = new Vector3(
                mouseMotion.Relative.X * Sensitivity,
                mouseMotion.Relative.Y * Sensitivity,
                0f);
        }
        else
        {
            float delta = IsYAxis ? mouseMotion.Relative.Y : mouseMotion.Relative.X;
            _currentValue = new Vector3(delta * Sensitivity, 0f, 0f);
        }
        return true;
    }
}
