using FluentAssertions;
using Godot;
using InputForge.Enum;
using InputForge.Mappings;

namespace InputForge.Tests.Unit.Mappings;

/// <summary>
/// Covers InputKey's HandleInput routing for every InputType, plus the binding-identity
/// Equals/GetHashCode/== contract.
///
/// NOTE: Digital and Analog read live OS state via Godot.Input.IsKeyPressed/GetJoyAxis,
/// which synthetic InputEvent objects don't influence in a headless run. For those types
/// we assert routing (correct event type matched, returns true, value shape) rather than
/// a specific produced magnitude — matching the existing test suite's documented limitation.
/// Boolean, Delta and Pointer read straight off the event, so their values ARE asserted.
/// </summary>
[Collection("InputForge")]
public class InputKeyTests(InputForgeTestFixture fixture)
{
    // ---- helpers ----------------------------------------------------------

    private static InputKey Boolean(InputDeviceType device, Key k = Key.Space,
        JoyButton btn = JoyButton.A, MouseButton mb = MouseButton.Left) => new()
    {
        InputType = InputType.Boolean,
        DeviceType = device,
        KeyboardKey = k,
        GamepadButton = btn,
        MouseKey = mb
    };

    // ===================================================================
    //  HandleInput — Boolean
    // ===================================================================

    [Fact]
    public void HandleInput_BooleanKeyboard_MatchingKeyDown_ReturnsTrue_AndValueIsOne()
    {
        var key = Boolean(InputDeviceType.Keyboard, Key.Space);
        var handled = key.HandleInput(new InputEventKey { Keycode = Key.Space, Pressed = true });

        handled.Should().BeTrue();
        key.GetValue().Should().Be(new Vector3(1f, 0f, 0f));
    }

    [Fact]
    public void HandleInput_BooleanKeyboard_MatchingKeyUp_ReturnsTrue_AndValueIsZero()
    {
        var key = Boolean(InputDeviceType.Keyboard, Key.Space);
        var handled = key.HandleInput(new InputEventKey { Keycode = Key.Space, Pressed = false });

        handled.Should().BeTrue();
        key.GetValue().Should().Be(Vector3.Zero);
    }

    [Fact]
    public void HandleInput_BooleanKeyboard_NonMatchingKey_ReturnsFalse()
    {
        var key = Boolean(InputDeviceType.Keyboard, Key.Space);
        key.HandleInput(new InputEventKey { Keycode = Key.Enter, Pressed = true })
            .Should().BeFalse();
    }

    [Fact]
    public void HandleInput_BooleanKeyboard_EchoEvent_IsIgnored()
    {
        var key = Boolean(InputDeviceType.Keyboard, Key.Space);
        key.HandleInput(new InputEventKey { Keycode = Key.Space, Pressed = true, Echo = true })
            .Should().BeFalse("OS key-repeat (echo) events must be filtered out");
    }

    [Fact]
    public void HandleInput_BooleanKeyboard_WrongEventType_ReturnsFalse()
    {
        var key = Boolean(InputDeviceType.Keyboard, Key.Space);
        key.HandleInput(new InputEventMouseButton { ButtonIndex = MouseButton.Left, Pressed = true })
            .Should().BeFalse();
    }

    [Fact]
    public void HandleInput_BooleanMouseButton_MatchingButton_ReturnsTrue_AndValueIsOne()
    {
        var key = Boolean(InputDeviceType.MouseButton, mb: MouseButton.Right);
        var handled = key.HandleInput(new InputEventMouseButton { ButtonIndex = MouseButton.Right, Pressed = true });

        handled.Should().BeTrue();
        key.GetValue().Should().Be(new Vector3(1f, 0f, 0f));
    }

    [Fact]
    public void HandleInput_BooleanMouseButton_NonMatchingButton_ReturnsFalse()
    {
        var key = Boolean(InputDeviceType.MouseButton, mb: MouseButton.Right);
        key.HandleInput(new InputEventMouseButton { ButtonIndex = MouseButton.Left, Pressed = true })
            .Should().BeFalse();
    }

    [Fact]
    public void HandleInput_BooleanJoyButton_MatchingButton_ReturnsTrue_AndValueIsOne()
    {
        var key = Boolean(InputDeviceType.JoyButton, btn: JoyButton.X);
        var handled = key.HandleInput(new InputEventJoypadButton { ButtonIndex = JoyButton.X, Pressed = true });

        handled.Should().BeTrue();
        key.GetValue().Should().Be(new Vector3(1f, 0f, 0f));
    }

    [Fact]
    public void HandleInput_BooleanJoyButton_NonMatchingButton_ReturnsFalse()
    {
        var key = Boolean(InputDeviceType.JoyButton, btn: JoyButton.X);
        key.HandleInput(new InputEventJoypadButton { ButtonIndex = JoyButton.Y, Pressed = true })
            .Should().BeFalse();
    }

    // ===================================================================
    //  HandleInput — Digital (routing only; value reads live OS state)
    // ===================================================================

    [Fact]
    public void HandleInput_Digital_WithKeyEvent_ReturnsTrue_AndZComponentZero()
    {
        var key = new InputKey { InputType = InputType.Digital, AxisDimension = AxisDimension.Axis1D };
        var handled = key.HandleInput(new InputEventKey { Keycode = Key.D, Pressed = true });

        handled.Should().BeTrue("any key event drives a digital axis re-read");
        key.GetValue().Z.Should().Be(0f);
    }

    [Fact]
    public void HandleInput_Digital_WithNonKeyEvent_ReturnsFalse()
    {
        var key = new InputKey { InputType = InputType.Digital };
        key.HandleInput(new InputEventMouseMotion()).Should().BeFalse();
    }

    [Fact]
    public void HandleInput_Digital2D_WithKeyEvent_ReturnsTrue()
    {
        var key = new InputKey { InputType = InputType.Digital, AxisDimension = AxisDimension.Axis2D };
        key.HandleInput(new InputEventKey { Keycode = Key.W, Pressed = true }).Should().BeTrue();
    }

    // ===================================================================
    //  HandleInput — Analog (routing only; value reads live OS state)
    // ===================================================================

    [Fact]
    public void HandleInput_Analog_RelevantAxisMotion_ReturnsTrue()
    {
        var key = new InputKey { InputType = InputType.Analog, JoystickAxis = JoyAxis.LeftX };
        key.HandleInput(new InputEventJoypadMotion { Axis = JoyAxis.LeftX, AxisValue = 0.7f })
            .Should().BeTrue();
    }

    [Fact]
    public void HandleInput_Analog_IrrelevantAxisMotion_ReturnsFalse()
    {
        var key = new InputKey
        {
            InputType = InputType.Analog,
            AxisDimension = AxisDimension.Axis1D,
            JoystickAxis = JoyAxis.LeftX
        };
        // A different axis on a 1D analog binding shouldn't be handled.
        key.HandleInput(new InputEventJoypadMotion { Axis = JoyAxis.RightY, AxisValue = 0.7f })
            .Should().BeFalse();
    }

    [Fact]
    public void HandleInput_Analog_WithNonJoypadMotionEvent_ReturnsFalse()
    {
        var key = new InputKey { InputType = InputType.Analog };
        key.HandleInput(new InputEventKey { Keycode = Key.A, Pressed = true }).Should().BeFalse();
    }

    [Fact]
    public void HandleInput_Analog2D_SecondaryAxisMotion_ReturnsTrue()
    {
        var key = new InputKey
        {
            InputType = InputType.Analog,
            AxisDimension = AxisDimension.Axis2D,
            JoystickAxis = JoyAxis.LeftX,
            JoystickAxisY = JoyAxis.LeftY
        };
        // On a 2D binding the Y axis is also relevant.
        key.HandleInput(new InputEventJoypadMotion { Axis = JoyAxis.LeftY, AxisValue = 0.5f })
            .Should().BeTrue();
    }

    // ===================================================================
    //  HandleInput — Delta (value read straight off the event)
    // ===================================================================

    [Fact]
    public void HandleInput_Delta1D_X_ScalesRelativeXBySensitivity()
    {
        var key = new InputKey
        {
            InputType = InputType.Delta,
            AxisDimension = AxisDimension.Axis1D,
            Sensitivity = 0.5f,
            IsYAxis = false
        };
        var handled = key.HandleInput(new InputEventMouseMotion { Relative = new Vector2(10f, 4f) });

        handled.Should().BeTrue();
        key.GetValue().Should().Be(new Vector3(5f, 0f, 0f)); // 10 * 0.5
    }

    [Fact]
    public void HandleInput_Delta1D_Y_ReadsRelativeYWhenIsYAxis()
    {
        var key = new InputKey
        {
            InputType = InputType.Delta,
            AxisDimension = AxisDimension.Axis1D,
            Sensitivity = 1f,
            IsYAxis = true
        };
        key.HandleInput(new InputEventMouseMotion { Relative = new Vector2(10f, 4f) });
        key.GetValue().Should().Be(new Vector3(4f, 0f, 0f)); // reads Y
    }

    [Fact]
    public void HandleInput_Delta2D_ScalesBothAxesBySensitivity()
    {
        var key = new InputKey
        {
            InputType = InputType.Delta,
            AxisDimension = AxisDimension.Axis2D,
            Sensitivity = 2f
        };
        key.HandleInput(new InputEventMouseMotion { Relative = new Vector2(3f, 5f) });
        key.GetValue().Should().Be(new Vector3(6f, 10f, 0f));
    }

    [Fact]
    public void HandleInput_Delta_WithNonMotionEvent_ReturnsFalse()
    {
        var key = new InputKey { InputType = InputType.Delta };
        key.HandleInput(new InputEventKey { Keycode = Key.A, Pressed = true }).Should().BeFalse();
    }

    // ===================================================================
    //  HandleInput — Pointer (falls back to event Position with no instance)
    // ===================================================================

    [Fact]
    public void HandleInput_Pointer_FallsBackToEventPosition_WhenNoInstance()
    {
        InputForgeTestExtensions.DropEnhancedInputSystemInstance();

        var key = new InputKey { InputType = InputType.Pointer };
        var handled = key.HandleInput(new InputEventMouseMotion { Position = new Vector2(123f, 456f) });

        handled.Should().BeTrue();
        key.GetValue().Should().Be(new Vector3(123f, 456f, 0f));
    }

    [Fact]
    public void HandleInput_Pointer_WithNonMotionEvent_ReturnsFalse()
    {
        var key = new InputKey { InputType = InputType.Pointer };
        key.HandleInput(new InputEventMouseButton { ButtonIndex = MouseButton.Left }).Should().BeFalse();
    }

    // ===================================================================
    //  GetValue default
    // ===================================================================

    [Fact]
    public void GetValue_BeforeAnyEvent_IsZero()
    {
        new InputKey().GetValue().Should().Be(Vector3.Zero);
    }

    // ===================================================================
    //  Equals / GetHashCode / operators — binding identity
    // ===================================================================

    [Fact]
    public void Equals_IsFalse_ForDifferentInputType()
    {
        var a = new InputKey { InputType = InputType.Boolean };
        var b = new InputKey { InputType = InputType.Digital };
        a.Equals(b).Should().BeFalse();
    }

    [Fact]
    public void Equals_IsFalse_AgainstNonInputKey()
    {
        new InputKey().Equals("not a key").Should().BeFalse();
    }

    [Fact]
    public void Equals_BooleanKeyboard_SameKey_AreEqual()
    {
        var a = Boolean(InputDeviceType.Keyboard, Key.Space);
        var b = Boolean(InputDeviceType.Keyboard, Key.Space);
        a.Equals(b).Should().BeTrue();
        a.GetHashCode().Should().Be(b.GetHashCode());
    }

    [Fact]
    public void Equals_BooleanKeyboard_DifferentKey_AreNotEqual()
    {
        var a = Boolean(InputDeviceType.Keyboard, Key.Space);
        var b = Boolean(InputDeviceType.Keyboard, Key.Enter);
        a.Equals(b).Should().BeFalse();
    }

    [Fact]
    public void Equals_Boolean_DifferentDevice_AreNotEqual()
    {
        var a = Boolean(InputDeviceType.Keyboard, Key.Space);
        var b = Boolean(InputDeviceType.MouseButton, mb: MouseButton.Left);
        a.Equals(b).Should().BeFalse();
    }

    [Fact]
    public void Equals_BooleanMouse_SameButton_AreEqual()
    {
        var a = Boolean(InputDeviceType.MouseButton, mb: MouseButton.Right);
        var b = Boolean(InputDeviceType.MouseButton, mb: MouseButton.Right);
        a.Equals(b).Should().BeTrue();
    }

    [Fact]
    public void Equals_BooleanJoyButton_SameButton_AreEqual()
    {
        var a = Boolean(InputDeviceType.JoyButton, btn: JoyButton.X);
        var b = Boolean(InputDeviceType.JoyButton, btn: JoyButton.X);
        a.Equals(b).Should().BeTrue();
    }

    [Fact]
    public void Equals_Digital1D_SamePositiveNegative_AreEqual()
    {
        var a = new InputKey { InputType = InputType.Digital, PositiveKey = Key.D, NegativeKey = Key.A };
        var b = new InputKey { InputType = InputType.Digital, PositiveKey = Key.D, NegativeKey = Key.A };
        a.Equals(b).Should().BeTrue();
        a.GetHashCode().Should().Be(b.GetHashCode());
    }

    [Fact]
    public void Equals_Digital1D_DifferentKeys_AreNotEqual()
    {
        var a = new InputKey { InputType = InputType.Digital, PositiveKey = Key.D, NegativeKey = Key.A };
        var b = new InputKey { InputType = InputType.Digital, PositiveKey = Key.Right, NegativeKey = Key.Left };
        a.Equals(b).Should().BeFalse();
    }

    [Fact]
    public void Equals_Digital_DifferentDimension_AreNotEqual()
    {
        var a = new InputKey { InputType = InputType.Digital, AxisDimension = AxisDimension.Axis1D };
        var b = new InputKey { InputType = InputType.Digital, AxisDimension = AxisDimension.Axis2D };
        a.Equals(b).Should().BeFalse();
    }

    [Fact]
    public void Equals_Digital2D_ComparesSecondaryAxisKeys()
    {
        var a = new InputKey { InputType = InputType.Digital, AxisDimension = AxisDimension.Axis2D, PositiveKeyY = Key.S, NegativeKeyY = Key.W };
        var b = new InputKey { InputType = InputType.Digital, AxisDimension = AxisDimension.Axis2D, PositiveKeyY = Key.Down, NegativeKeyY = Key.Up };
        a.Equals(b).Should().BeFalse("2D digital bindings must also match on the Y-axis keys");
    }

    [Fact]
    public void Equals_Analog1D_SameAxis_AreEqual()
    {
        var a = new InputKey { InputType = InputType.Analog, JoystickAxis = JoyAxis.LeftX };
        var b = new InputKey { InputType = InputType.Analog, JoystickAxis = JoyAxis.LeftX };
        a.Equals(b).Should().BeTrue();
        a.GetHashCode().Should().Be(b.GetHashCode());
    }

    [Fact]
    public void Equals_Analog_DifferentAxis_AreNotEqual()
    {
        var a = new InputKey { InputType = InputType.Analog, JoystickAxis = JoyAxis.LeftX };
        var b = new InputKey { InputType = InputType.Analog, JoystickAxis = JoyAxis.RightX };
        a.Equals(b).Should().BeFalse();
    }

    [Fact]
    public void Equals_AllDeltaKeys_AreEqual_RegardlessOfSensitivity()
    {
        var a = new InputKey { InputType = InputType.Delta, Sensitivity = 0.1f };
        var b = new InputKey { InputType = InputType.Delta, Sensitivity = 9f };
        a.Equals(b).Should().BeTrue("all Delta keys read the same mouse motion stream");
    }

    [Fact]
    public void Equals_AllPointerKeys_AreEqual()
    {
        var a = new InputKey { InputType = InputType.Pointer };
        var b = new InputKey { InputType = InputType.Pointer };
        a.Equals(b).Should().BeTrue("all Pointer keys read the same Viewport cursor position");
    }

    [Fact]
    public void EqualityOperator_BothNull_IsTrue()
    {
        InputKey a = null, b = null;
        (a == b).Should().BeTrue();
    }

    [Fact]
    public void EqualityOperator_OneNull_IsFalse()
    {
        var a = new InputKey { InputType = InputType.Pointer };
        (a == null).Should().BeFalse();
        (null == a).Should().BeFalse();
    }

    [Fact]
    public void InequalityOperator_DifferentBindings_IsTrue()
    {
        var a = Boolean(InputDeviceType.Keyboard, Key.Space);
        var b = Boolean(InputDeviceType.Keyboard, Key.Enter);
        (a != b).Should().BeTrue();
    }
}
