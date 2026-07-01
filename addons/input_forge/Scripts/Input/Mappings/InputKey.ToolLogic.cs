using Godot;
using InputForge.Enum;

namespace InputForge.Mappings;

/// <summary>
/// Editor / Inspector concerns for <see cref="InputKey"/>: controls which exported
/// fields are visible for the currently selected InputType / DeviceType / AxisDimension,
/// so the Inspector only ever shows the properties that apply to the active configuration.
/// This is tooling logic — it runs in the editor and shapes the UI, not the runtime value.
/// </summary>
public partial class InputKey
{
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

        // Sensitivity and IsYAxis only apply to Delta — Pointer reads an absolute
        // position from the live Viewport, where neither concept makes sense.
        if (name == nameof(Sensitivity) && !isDelta) Hide(property);
        if (name == nameof(IsYAxis)     && !(isDelta && !is2D)) Hide(property);
    }

    private static void Hide(Godot.Collections.Dictionary property)
        => property["usage"] = (int)PropertyUsageFlags.NoEditor;
}
