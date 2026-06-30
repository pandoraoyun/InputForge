using FluentAssertions;
using Godot;
using InputForge.Enum;
using InputForge.Mappings;

namespace InputForge.Tests.Unit.Mappings;

[Collection("InputForge")]
public class InputKeyPointerTests(InputForgeTestFixture fixture)
{
    private static InputKey PointerKey() => new() { InputType = InputType.Pointer };

    [Fact]
    public void HandleInput_WithMouseMotionEvent_ReturnsTrue()
    {
        var key = PointerKey();
        var evt = new InputEventMouseMotion { Relative = new Vector2(5f, 5f), Position = new Vector2(10f, 10f) };

        var handled = key.HandleInput(evt);

        handled.Should().BeTrue("a mouse motion event is the only thing that should trigger a Pointer read");
    }

    [Fact]
    public void HandleInput_WithNonMotionEvent_ReturnsFalse()
    {
        var key = PointerKey();
        var evt = new InputEventKey { Keycode = Key.Space, Pressed = true };

        var handled = key.HandleInput(evt);

        handled.Should().BeFalse("Pointer only responds to mouse motion events, not keyboard/button events");
    }

    [Fact]
    public void HandleInput_WithMouseButtonEvent_ReturnsFalse()
    {
        var key = PointerKey();
        var evt = new InputEventMouseButton { ButtonIndex = MouseButton.Left, Pressed = true };

        var handled = key.HandleInput(evt);

        handled.Should().BeFalse("Pointer requires motion specifically — a button press alone shouldn't trigger a read");
    }

    [Fact]
    public void GetValue_AfterHandlingMotionEvent_IgnoresEventsOwnRelativeField()
    {
        // Relative is never used by Pointer regardless of viewport availability — this part
        // of the conceptual guarantee holds unconditionally.
        var key = PointerKey();
        var decoyRelative = new Vector2(99999f, 99999f);
        var evt = new InputEventMouseMotion { Relative = decoyRelative, Position = Vector2.Zero };

        key.HandleInput(evt);
        var result = key.GetValue();

        result.Should().NotBe(new Vector3(decoyRelative.X, decoyRelative.Y, 0f),
            "the value must never be sourced from the event's Relative field, regardless of viewport availability");
    }

    [Fact]
    public void GetValue_AfterHandlingMotionEvent_WhenNoEnhancedInputSystemInstanceExists_FallsBackToEventPosition()
    {
        // In this test environment, no EnhancedInputSystem node has been added to the tree
        // (the fixture only starts the Godot runtime, it doesn't auto-create one), so
        // GetInstance() returns null and HandlePointer falls back to the event's own
        // Position field. This documents and locks in that fallback behavior explicitly,
        // rather than treating it as an accidental side effect of the test environment.
        var key = PointerKey();
        var expectedFallbackPosition = new Vector2(-12345f, -12345f);
        var evt = new InputEventMouseMotion { Position = expectedFallbackPosition };

        // This shared-engine collection leaves EnhancedInputSystem nodes parented to the
        // tree (QueueFree never runs without a processed frame), so a leftover singleton
        // may linger. Synchronously evict it so the "no instance" precondition holds
        // regardless of execution order.
        InputForgeTestExtensions.DropEnhancedInputSystemInstance();

        EnhancedInputSystem.GetInstance().Should().BeNull(
            "this test assumes no EnhancedInputSystem instance is registered in the current test context");

        key.HandleInput(evt);
        var result = key.GetValue();

        result.Should().Be(new Vector3(expectedFallbackPosition.X, expectedFallbackPosition.Y, 0f),
            "without a live EnhancedInputSystem/Viewport, Pointer should fall back to the event's own Position");
    }

    [Fact]
    public void GetValue_AfterHandlingMotionEvent_WhenEnhancedInputSystemInstanceExists_ReadsFromViewportNotEvent()
    {
        var system = new EnhancedInputSystem();
        fixture.Tree.Root.AddChild(system);
        try
        {
            var key = PointerKey();
            var decoyEventPosition = new Vector2(-12345f, -12345f);
            var evt = new InputEventMouseMotion { Position = decoyEventPosition };

            EnhancedInputSystem.GetInstance().Should().Be(system,
                "the newly added node should register itself as the singleton instance via _Ready()");

            key.HandleInput(evt);
            var result = key.GetValue();

            // We can't assert a specific real cursor position in this headless environment,
            // but we CAN assert it did NOT just echo the event's own (deliberately distinct)
            // Position value — proving it actually went through GetInputViewport().GetMousePosition()
            // rather than silently falling back to the event.
            result.Should().NotBe(new Vector3(decoyEventPosition.X, decoyEventPosition.Y, 0f),
                "with a live EnhancedInputSystem instance available, the value should come from the " +
                "Viewport, not be an echo of the event's own Position field");
        }
        finally
        {
            system.QueueFree();
        }
    }

    [Fact]
    public void GetValue_AfterHandlingMotionEvent_AlwaysHasZeroZComponent()
    {
        var key = PointerKey();
        var evt = new InputEventMouseMotion();

        key.HandleInput(evt);
        var result = key.GetValue();

        result.Z.Should().Be(0f, "pointer position is inherently 2D");
    }

    [Fact]
    public void GetValue_BeforeAnyEvent_DefaultsToZero()
    {
        var key = PointerKey();

        var result = key.GetValue();

        result.Should().Be(Vector3.Zero);
    }

    [Fact]
    public void HandleInput_WhenInputTypeIsNotPointer_NeverCallsHandlePointer()
    {
        // Sanity check that switching InputType away from Pointer routes to a different
        // handler entirely — e.g. Boolean shouldn't react to mouse motion at all.
        var key = new InputKey
        {
            InputType = InputType.Boolean,
            DeviceType = InputDeviceType.Keyboard,
            KeyboardKey = Key.Space
        };
        var evt = new InputEventMouseMotion { Position = new Vector2(42f, 42f) };

        var handled = key.HandleInput(evt);

        handled.Should().BeFalse("a Boolean-type key should never react to mouse motion events");
    }
}