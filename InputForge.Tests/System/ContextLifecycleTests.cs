using FluentAssertions;
using Godot;
using InputForge.Enum;
using InputForge.Mappings;

namespace InputForge.Tests.System;

/// <summary>
/// Context-lifecycle edge-detection tests. When an action's callback deactivates
/// its own context (e.g. a "close menu" action that calls RemoveContext), that
/// context never sees the RELEASE of the key that triggered it, leaving its
/// TriggerOnKeyDown with _previousActive == true. The fix resets trigger edge-state
/// on every ActiveContextChanged, so re-activation fires cleanly on the first press.
///
/// Re-added piece by piece after a 2dog harness cache issue. Starting with the
/// Control test to confirm the harness and rising-edge logic are sound.
/// </summary>
[Collection("InputForge")]
public class ContextLifecycleTests
{
    private readonly SceneTree _tree;

    public ContextLifecycleTests(InputForgeTestFixture fixture)
    {
        _tree = fixture.Tree
            ?? throw new InvalidOperationException(
                "InputForgeTestFixture.Tree is null — the Godot engine failed to start.");
    }

    private EnhancedInputSystem CreateSystem()
    {
        var system = new EnhancedInputSystem();
        _tree.Root.AddChild(system);
        return system;
    }

    private static InputAction Action(string name) => new() { ActionName = name };

    private static InputKey BooleanKey(Key key) => new()
    {
        InputType = InputType.Boolean,
        DeviceType = InputDeviceType.Keyboard,
        KeyboardKey = key
    };

    private static InputMappingContext Context(string name, params InputMapping[] mappings)
    {
        var ctx = new InputMappingContext { ContextName = name };
        foreach (var m in mappings) ctx.Mappings.Add(m);
        return ctx;
    }

    private static InputMapping Mapping(InputAction action, InputKey key) => new()
    {
        TargetAction = action,
        InputSource = key
    };

    private static InputEventKey KeyEvent(Key key, bool pressed) => new()
    {
        Keycode = key,
        Pressed = pressed,
        Echo = false
    };

    /// <summary>
    /// Control: a stable context that never deactivates itself toggles correctly
    /// every press/release cycle. Must stay green — proves the rising-edge logic
    /// and the test harness are sound.
    /// </summary>
    [Fact]
    public void StableContext_Control_FiresEveryPress_AcrossReleaseCycles()
    {
        var system = CreateSystem();
        try
        {
            var jump = Action("Jump");
            var ctx = Context("Gameplay", Mapping(jump, BooleanKey(Key.Space)));

            int jumpCount = 0;
            ctx.BindAction(jump, (bool pressed) => { if (pressed) jumpCount++; });

            system.AddContext(ctx);

            system._Input(KeyEvent(Key.Space, pressed: true));
            system._Input(KeyEvent(Key.Space, pressed: false));
            system._Input(KeyEvent(Key.Space, pressed: true));
            system._Input(KeyEvent(Key.Space, pressed: false));

            jumpCount.Should().Be(2);
        }
        finally
        {
            system.QueueFree();
        }
    }
}
