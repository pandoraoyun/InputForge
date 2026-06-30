using FluentAssertions;
using Godot;
using InputForge.Enum;
using InputForge.Mappings;

namespace InputForge.Tests.System;

/// <summary>
/// End-to-end tests covering the full pipeline: EnhancedInputSystem dispatch,
/// context push/pop, action triggering via real Godot InputEvent objects,
/// and the PreventFallbackContext system flag.
///
/// Each test creates its own EnhancedInputSystem node, adds it to the live
/// SceneTree root so _Ready() runs, and removes it again in a finally block
/// to keep tests isolated from each other.
///
/// NOTE: Tests intentionally use Boolean InputKey mappings rather than Digital.
/// Digital reads live state via Godot.Input.IsKeyPressed(), which is not
/// affected by synthetic InputEvent objects dispatched directly to _Input() —
/// only Boolean reads the value straight off the InputEventKey itself.
/// </summary>
[Collection("InputForge")]
public class InputSystemTests
{
    private readonly SceneTree _tree;

    public InputSystemTests(InputForgeTestFixture fixture)
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

    [Fact]
    public void Input_TriggersBoundCallback_WhenKeyMatchesMapping()
    {
        var system = CreateSystem();
        try
        {
            var jump = Action("Jump");
            var ctx = Context("Gameplay", Mapping(jump, BooleanKey(Key.Space)));
            bool received = false;
            bool? receivedValue = null;
            ctx.BindAction(jump, (bool pressed) =>
            {
                received = true;
                receivedValue = pressed;
            });

            system.AddContext(ctx);
            system._Input(KeyEvent(Key.Space, pressed: true));

            received.Should().BeTrue();
            receivedValue.Should().BeTrue();
        }
        finally
        {
            system.QueueFree();
        }
    }

    [Fact]
    public void Input_DoesNotTrigger_WhenKeyDoesNotMatchMapping()
    {
        var system = CreateSystem();
        try
        {
            var jump = Action("Jump");
            var ctx = Context("Gameplay", Mapping(jump, BooleanKey(Key.Space)));
            bool received = false;
            ctx.BindAction(jump, (bool _) => received = true);

            system.AddContext(ctx);
            system._Input(KeyEvent(Key.Enter, pressed: true));

            received.Should().BeFalse();
        }
        finally
        {
            system.QueueFree();
        }
    }

    [Fact]
    public void Input_HigherPriorityContext_TakesPrecedenceOverLowerOne()
    {
        var system = CreateSystem();
        try
        {
            var confirm = Action("Confirm");
            var jump = Action("Jump");

            var gameplay = Context("Gameplay", Mapping(jump, BooleanKey(Key.Space)));
            var menu = Context("Menu", Mapping(confirm, BooleanKey(Key.Space)));

            bool jumpFired = false;
            bool confirmFired = false;
            gameplay.BindAction(jump, (bool _) => jumpFired = true);
            menu.BindAction(confirm, (bool _) => confirmFired = true);

            system.AddContext(gameplay);
            system.AddContext(menu); // higher priority, added last

            system._Input(KeyEvent(Key.Space, pressed: true));

            confirmFired.Should().BeTrue("the higher-priority Menu context owns Space");
            jumpFired.Should().BeFalse("Gameplay should not see the event once Menu consumed it");
        }
        finally
        {
            system.QueueFree();
        }
    }

    [Fact]
    public void Input_FallsThroughToLowerContext_WhenHigherContextHasNoMatchingMapping_AndFallbackAllowed()
    {
        var system = CreateSystem();
        try
        {
            var pause = Action("Pause");
            var accelerate = Action("Accelerate");

            var gameplay = Context("Gameplay", Mapping(pause, BooleanKey(Key.Escape)));
            var vehicle = Context("Vehicle", Mapping(accelerate, BooleanKey(Key.W)));

            bool pauseFired = false;
            gameplay.BindAction(pause, (bool _) => pauseFired = true);

            system.AddContext(gameplay);
            system.AddContext(vehicle); // higher priority, but doesn't map Escape

            system._Input(KeyEvent(Key.Escape, pressed: true));

            pauseFired.Should().BeTrue("Vehicle has no mapping for Escape, so it should fall through to Gameplay");
        }
        finally
        {
            system.QueueFree();
        }
    }

    [Fact]
    public void Input_PreventFallbackContext_StopsAtTopmostContext_EvenWithoutMatchingMapping()
    {
        var system = CreateSystem();
        try
        {
            var jump = Action("Jump");

            // Gameplay maps Space → Jump.
            var gameplay = Context("Gameplay", Mapping(jump, BooleanKey(Key.Space)));

            // Follow has no mapping at all for Space.
            var follow = Context("Follow");

            bool jumpFired = false;
            gameplay.BindAction(jump, (bool _) => jumpFired = true);

            system.AddContext(gameplay);
            system.AddContext(follow);
            system.PreventFallbackContext = true;

            system._Input(KeyEvent(Key.Space, pressed: true));

            jumpFired.Should().BeFalse(
                "PreventFallbackContext stops the loop after the topmost context, even with no match");
        }
        finally
        {
            system.QueueFree();
        }
    }

    [Fact]
    public void Input_PreventFallbackContext_False_AllowsNormalFallthrough()
    {
        var system = CreateSystem();
        try
        {
            var jump = Action("Jump");

            var gameplay = Context("Gameplay", Mapping(jump, BooleanKey(Key.Space)));
            var follow = Context("Follow");

            bool jumpFired = false;
            gameplay.BindAction(jump, (bool _) => jumpFired = true);

            system.AddContext(gameplay);
            system.AddContext(follow);
            // PreventFallbackContext defaults to false — no need to set it.

            system._Input(KeyEvent(Key.Space, pressed: true));

            jumpFired.Should().BeTrue(
                "with PreventFallbackContext false, the event should fall through to Gameplay's Jump mapping");
        }
        finally
        {
            system.QueueFree();
        }
    }

    [Fact]
    public void PreventFallbackContext_DefaultsToFalse()
    {
        var system = CreateSystem();
        try
        {
            system.PreventFallbackContext.Should().BeFalse();
        }
        finally
        {
            system.QueueFree();
        }
    }

    [Fact]
    public void RemoveContext_StopsDeliveringEventsToThatContext()
    {
        var system = CreateSystem();
        try
        {
            var jump = Action("Jump");
            var ctx = Context("Gameplay", Mapping(jump, BooleanKey(Key.Space)));
            int callCount = 0;
            ctx.BindAction(jump, (bool _) => callCount++);

            system.AddContext(ctx);
            system._Input(KeyEvent(Key.Space, pressed: true));

            system.RemoveContext(ctx);
            system._Input(KeyEvent(Key.Space, pressed: true));

            callCount.Should().Be(1, "the second event should not reach a removed context");
        }
        finally
        {
            system.QueueFree();
        }
    }
}