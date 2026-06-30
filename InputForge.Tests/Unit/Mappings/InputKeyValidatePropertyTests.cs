using FluentAssertions;
using Godot;
using InputForge.Enum;
using InputForge.Mappings;

namespace InputForge.Tests.Unit.Mappings;

/// <summary>
/// Verifies InputKey._ValidateProperty — the Inspector visibility logic that hides
/// fields irrelevant to the currently selected InputType / AxisDimension / DeviceType.
/// This is pure decision logic (no live engine state), so it's asserted directly:
/// build a property dictionary, run it through _ValidateProperty, then check whether
/// its "usage" flag was switched to NoEditor (hidden) or left untouched (visible).
/// </summary>
[Collection("InputForge")]
public class InputKeyValidatePropertyTests(InputForgeTestFixture fixture)
{
    /// <summary>
    /// Runs a single named property through _ValidateProperty and reports whether it
    /// ended up hidden. Starts from a "fully visible" usage flag so any Hide() call is
    /// observable as the NoEditor bit being set.
    /// </summary>
    private static bool IsHidden(InputKey key, string propertyName)
    {
        var property = new Godot.Collections.Dictionary
        {
            { "name", propertyName },
            { "usage", (int)PropertyUsageFlags.Default }
        };

        key._ValidateProperty(property);

        var usage = (PropertyUsageFlags)property["usage"].AsInt32();
        return usage == PropertyUsageFlags.NoEditor;
    }

    private static bool IsVisible(InputKey key, string propertyName) => !IsHidden(key, propertyName);

    // ===================================================================
    //  Boolean
    // ===================================================================

    [Fact]
    public void Boolean_Keyboard_ShowsKeyboardKey_HidesOtherDeviceFieldsAndAxisStuff()
    {
        var key = new InputKey { InputType = InputType.Boolean, DeviceType = InputDeviceType.Keyboard };

        IsVisible(key, nameof(InputKey.DeviceType)).Should().BeTrue();
        IsVisible(key, nameof(InputKey.KeyboardKey)).Should().BeTrue();
        IsHidden(key, nameof(InputKey.GamepadButton)).Should().BeTrue();
        IsHidden(key, nameof(InputKey.MouseKey)).Should().BeTrue();

        // Axis machinery is irrelevant to Boolean.
        IsHidden(key, nameof(InputKey.AxisDimension)).Should().BeTrue();
        IsHidden(key, nameof(InputKey.PositiveKey)).Should().BeTrue();
        IsHidden(key, nameof(InputKey.JoystickAxis)).Should().BeTrue();
        IsHidden(key, nameof(InputKey.Sensitivity)).Should().BeTrue();
    }

    [Fact]
    public void Boolean_JoyButton_ShowsOnlyGamepadButton_AmongDeviceFields()
    {
        var key = new InputKey { InputType = InputType.Boolean, DeviceType = InputDeviceType.JoyButton };

        IsVisible(key, nameof(InputKey.GamepadButton)).Should().BeTrue();
        IsHidden(key, nameof(InputKey.KeyboardKey)).Should().BeTrue();
        IsHidden(key, nameof(InputKey.MouseKey)).Should().BeTrue();
    }

    [Fact]
    public void Boolean_MouseButton_ShowsOnlyMouseKey_AmongDeviceFields()
    {
        var key = new InputKey { InputType = InputType.Boolean, DeviceType = InputDeviceType.MouseButton };

        IsVisible(key, nameof(InputKey.MouseKey)).Should().BeTrue();
        IsHidden(key, nameof(InputKey.KeyboardKey)).Should().BeTrue();
        IsHidden(key, nameof(InputKey.GamepadButton)).Should().BeTrue();
    }

    // ===================================================================
    //  Digital
    // ===================================================================

    [Fact]
    public void Digital1D_ShowsPrimaryAxisKeys_HidesSecondaryAndDeviceFields()
    {
        var key = new InputKey { InputType = InputType.Digital, AxisDimension = AxisDimension.Axis1D };

        IsVisible(key, nameof(InputKey.AxisDimension)).Should().BeTrue("AxisDimension is shown for non-Boolean types");
        IsVisible(key, nameof(InputKey.PositiveKey)).Should().BeTrue();
        IsVisible(key, nameof(InputKey.NegativeKey)).Should().BeTrue();

        // Y keys only matter in 2D.
        IsHidden(key, nameof(InputKey.PositiveKeyY)).Should().BeTrue();
        IsHidden(key, nameof(InputKey.NegativeKeyY)).Should().BeTrue();

        // Boolean device fields and analog/delta fields are irrelevant.
        IsHidden(key, nameof(InputKey.DeviceType)).Should().BeTrue();
        IsHidden(key, nameof(InputKey.KeyboardKey)).Should().BeTrue();
        IsHidden(key, nameof(InputKey.JoystickAxis)).Should().BeTrue();
        IsHidden(key, nameof(InputKey.Sensitivity)).Should().BeTrue();
    }

    [Fact]
    public void Digital2D_AlsoShowsSecondaryAxisKeys()
    {
        var key = new InputKey { InputType = InputType.Digital, AxisDimension = AxisDimension.Axis2D };

        IsVisible(key, nameof(InputKey.PositiveKey)).Should().BeTrue();
        IsVisible(key, nameof(InputKey.NegativeKey)).Should().BeTrue();
        IsVisible(key, nameof(InputKey.PositiveKeyY)).Should().BeTrue();
        IsVisible(key, nameof(InputKey.NegativeKeyY)).Should().BeTrue();
    }

    // ===================================================================
    //  Analog
    // ===================================================================

    [Fact]
    public void Analog1D_ShowsPrimaryJoystickAxis_HidesSecondary()
    {
        var key = new InputKey { InputType = InputType.Analog, AxisDimension = AxisDimension.Axis1D };

        IsVisible(key, nameof(InputKey.JoystickAxis)).Should().BeTrue();
        IsHidden(key, nameof(InputKey.JoystickAxisY)).Should().BeTrue();

        // Digital and delta fields don't apply.
        IsHidden(key, nameof(InputKey.PositiveKey)).Should().BeTrue();
        IsHidden(key, nameof(InputKey.Sensitivity)).Should().BeTrue();
    }

    [Fact]
    public void Analog2D_AlsoShowsSecondaryJoystickAxis()
    {
        var key = new InputKey { InputType = InputType.Analog, AxisDimension = AxisDimension.Axis2D };

        IsVisible(key, nameof(InputKey.JoystickAxis)).Should().BeTrue();
        IsVisible(key, nameof(InputKey.JoystickAxisY)).Should().BeTrue();
    }

    // ===================================================================
    //  Delta
    // ===================================================================

    [Fact]
    public void Delta1D_ShowsSensitivityAndIsYAxis()
    {
        var key = new InputKey { InputType = InputType.Delta, AxisDimension = AxisDimension.Axis1D };

        IsVisible(key, nameof(InputKey.Sensitivity)).Should().BeTrue();
        IsVisible(key, nameof(InputKey.IsYAxis)).Should().BeTrue("IsYAxis picks the single axis in 1D delta mode");
    }

    [Fact]
    public void Delta2D_ShowsSensitivity_ButHidesIsYAxis()
    {
        var key = new InputKey { InputType = InputType.Delta, AxisDimension = AxisDimension.Axis2D };

        IsVisible(key, nameof(InputKey.Sensitivity)).Should().BeTrue();
        // IsYAxis only makes sense for a single-axis (1D) delta — 2D reads both axes.
        IsHidden(key, nameof(InputKey.IsYAxis)).Should().BeTrue();
    }

    // ===================================================================
    //  Pointer — neither Sensitivity nor IsYAxis applies
    // ===================================================================

    [Fact]
    public void Pointer_HidesSensitivityAndIsYAxis()
    {
        var key = new InputKey { InputType = InputType.Pointer };

        // Pointer reads an absolute Viewport position — delta-specific knobs are hidden.
        IsHidden(key, nameof(InputKey.Sensitivity)).Should().BeTrue();
        IsHidden(key, nameof(InputKey.IsYAxis)).Should().BeTrue();
    }

    [Fact]
    public void Pointer_HidesAllBooleanDeviceFields()
    {
        var key = new InputKey { InputType = InputType.Pointer };

        IsHidden(key, nameof(InputKey.DeviceType)).Should().BeTrue();
        IsHidden(key, nameof(InputKey.KeyboardKey)).Should().BeTrue();
        IsHidden(key, nameof(InputKey.PositiveKey)).Should().BeTrue();
        IsHidden(key, nameof(InputKey.JoystickAxis)).Should().BeTrue();
    }

    // ===================================================================
    //  Unrelated properties are never touched
    // ===================================================================

    [Fact]
    public void UnrelatedProperty_IsLeftVisible_RegardlessOfInputType()
    {
        var key = new InputKey { InputType = InputType.Digital };
        // A property the validator doesn't know about (e.g. InputType itself) must pass through untouched.
        IsVisible(key, nameof(InputKey.InputType)).Should().BeTrue();
    }
}
