using FluentAssertions;
using Godot;
using InputForge.Enum;

namespace InputForge.Tests.System;

/// <summary>
/// Covers EnhancedInputSystem's stack-level signals (ActiveContextChanged, ContextPushed,
/// ContextPopped), InputMappingContext's own signals (Pushed, Popped, PriorityChanged),
/// GetCurrentContext(), and DuplicateContextBehavior (Ignore vs Replace).
/// </summary>
[Collection("InputForge")]
public class ContextStackSignalsTests
{
    private readonly SceneTree _tree;

    public ContextStackSignalsTests(InputForgeTestFixture fixture)
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

    private static InputMappingContext Context(string name) => new() { ContextName = name };

    [Fact]
    public void GetCurrentContext_IsNull_WhenStackIsEmpty()
    {
        var system = CreateSystem();
        try
        {
            system.GetCurrentContext().Should().BeNull();
        }
        finally
        {
            system.QueueFree();
        }
    }

    [Fact]
    public void GetCurrentContext_ReturnsLastPushed()
    {
        var system = CreateSystem();
        try
        {
            var gameplay = Context("Gameplay");
            var menu = Context("Menu");

            system.AddContext(gameplay);
            system.AddContext(menu);

            system.GetCurrentContext().Should().Be(menu);
        }
        finally
        {
            system.QueueFree();
        }
    }

    [Fact]
    public void GetCurrentContext_FallsBackToPrevious_AfterTopIsRemoved()
    {
        var system = CreateSystem();
        try
        {
            var gameplay = Context("Gameplay");
            var menu = Context("Menu");
            system.AddContext(gameplay);
            system.AddContext(menu);

            system.RemoveContext(menu);

            system.GetCurrentContext().Should().Be(gameplay);
        }
        finally
        {
            system.QueueFree();
        }
    }

    [Fact]
    public void ContextPushed_Signal_FiresOnAddContext()
    {
        var system = CreateSystem();
        try
        {
            var gameplay = Context("Gameplay");
            InputMappingContext received = null;
            int fireCount = 0;

            system.Connect(EnhancedInputSystem.SignalName.ContextPushed,
                Callable.From((InputMappingContext ctx) => { received = ctx; fireCount++; }));

            system.AddContext(gameplay);

            fireCount.Should().Be(1);
            received.Should().Be(gameplay);
        }
        finally
        {
            system.QueueFree();
        }
    }

    [Fact]
    public void ContextPopped_Signal_FiresOnRemoveContext()
    {
        var system = CreateSystem();
        try
        {
            var gameplay = Context("Gameplay");
            InputMappingContext received = null;
            int fireCount = 0;

            system.AddContext(gameplay);
            system.Connect(EnhancedInputSystem.SignalName.ContextPopped,
                Callable.From((InputMappingContext ctx) => { received = ctx; fireCount++; }));

            system.RemoveContext(gameplay);

            fireCount.Should().Be(1);
            received.Should().Be(gameplay);
        }
        finally
        {
            system.QueueFree();
        }
    }

    [Fact]
    public void ContextPopped_Signal_DoesNotFire_WhenContextWasNeverPushed()
    {
        var system = CreateSystem();
        try
        {
            var gameplay = Context("Gameplay");
            int fireCount = 0;

            system.Connect(EnhancedInputSystem.SignalName.ContextPopped,
                Callable.From((InputMappingContext ctx) => fireCount++));

            system.RemoveContext(gameplay); // never added

            fireCount.Should().Be(0);
        }
        finally
        {
            system.QueueFree();
        }
    }

    [Fact]
    public void ActiveContextChanged_Signal_FiresWhenTopChanges_OnPush()
    {
        var system = CreateSystem();
        try
        {
            var gameplay = Context("Gameplay");
            var menu = Context("Menu");
            var seenTops = new List<InputMappingContext>();

            system.Connect(EnhancedInputSystem.SignalName.ActiveContextChanged,
                Callable.From((InputMappingContext ctx) => seenTops.Add(ctx)));

            system.AddContext(gameplay); // null -> gameplay
            system.AddContext(menu);     // gameplay -> menu

            seenTops.Should().HaveCount(2);
            seenTops[0].Should().Be(gameplay);
            seenTops[1].Should().Be(menu);
        }
        finally
        {
            system.QueueFree();
        }
    }

    [Fact]
    public void ActiveContextChanged_Signal_FiresOnPop_WithNull_WhenStackBecomesEmpty()
    {
        var system = CreateSystem();
        try
        {
            var gameplay = Context("Gameplay");
            var seenTops = new List<InputMappingContext>();

            system.AddContext(gameplay);
            system.Connect(EnhancedInputSystem.SignalName.ActiveContextChanged,
                Callable.From((InputMappingContext ctx) => seenTops.Add(ctx)));

            system.RemoveContext(gameplay);

            seenTops.Should().HaveCount(1);
            seenTops[0].Should().BeNull();
        }
        finally
        {
            system.QueueFree();
        }
    }

    [Fact]
    public void ActiveContextChanged_Signal_DoesNotFire_WhenTopUnaffectedByLowerPop()
    {
        var system = CreateSystem();
        try
        {
            var gameplay = Context("Gameplay");
            var menu = Context("Menu");
            system.AddContext(gameplay);
            system.AddContext(menu); // menu is now top

            int fireCount = 0;
            system.Connect(EnhancedInputSystem.SignalName.ActiveContextChanged,
                Callable.From((InputMappingContext ctx) => fireCount++));

            system.RemoveContext(gameplay); // removing a lower context shouldn't change the top

            fireCount.Should().Be(0);
            system.GetCurrentContext().Should().Be(menu);
        }
        finally
        {
            system.QueueFree();
        }
    }

    [Fact]
    public void Context_PushedSignal_FiresWhenAddedToSystem()
    {
        var system = CreateSystem();
        try
        {
            var gameplay = Context("Gameplay");
            int fireCount = 0;
            gameplay.Connect(InputMappingContext.SignalName.Pushed,
                Callable.From(() => fireCount++));

            system.AddContext(gameplay);

            fireCount.Should().Be(1);
        }
        finally
        {
            system.QueueFree();
        }
    }

    [Fact]
    public void Context_PoppedSignal_FiresWhenRemovedFromSystem()
    {
        var system = CreateSystem();
        try
        {
            var gameplay = Context("Gameplay");
            system.AddContext(gameplay);

            int fireCount = 0;
            gameplay.Connect(InputMappingContext.SignalName.Popped,
                Callable.From(() => fireCount++));

            system.RemoveContext(gameplay);

            fireCount.Should().Be(1);
        }
        finally
        {
            system.QueueFree();
        }
    }

    [Fact]
    public void Context_PriorityChangedSignal_FiresTrue_WhenItBecomesTopmost()
    {
        var system = CreateSystem();
        try
        {
            var gameplay = Context("Gameplay");
            bool? isTopmost = null;
            gameplay.Connect(InputMappingContext.SignalName.PriorityChanged,
                Callable.From((bool top) => isTopmost = top));

            system.AddContext(gameplay);

            isTopmost.Should().BeTrue();
        }
        finally
        {
            system.QueueFree();
        }
    }

    [Fact]
    public void Context_PriorityChangedSignal_FiresFalse_WhenDisplacedByHigherContext()
    {
        var system = CreateSystem();
        try
        {
            var gameplay = Context("Gameplay");
            var menu = Context("Menu");
            system.AddContext(gameplay);

            bool? isTopmost = null;
            gameplay.Connect(InputMappingContext.SignalName.PriorityChanged,
                Callable.From((bool top) => isTopmost = top));

            system.AddContext(menu); // gameplay is displaced from the top

            isTopmost.Should().BeFalse();
        }
        finally
        {
            system.QueueFree();
        }
    }

    [Fact]
    public void DuplicateContextBehavior_DefaultsToReplace()
    {
        var system = CreateSystem();
        try
        {
            system.DuplicateContextBehavior.Should().Be(DuplicateContextBehavior.Replace);
        }
        finally
        {
            system.QueueFree();
        }
    }

    [Fact]
    public void DuplicateContextBehavior_Replace_MovesExistingContextToTop_WithoutDuplicate()
    {
        var system = CreateSystem();
        try
        {
            var gameplay = Context("Gameplay");
            var menu = Context("Menu");

            system.AddContext(gameplay);
            system.AddContext(menu);
            system.AddContext(gameplay); // re-push — should move to top, not duplicate

            system.GetCurrentContext().Should().Be(gameplay);

            // Popping once should fully remove it — no stale duplicate left behind.
            system.RemoveContext(gameplay);
            system.GetCurrentContext().Should().Be(menu);
        }
        finally
        {
            system.QueueFree();
        }
    }

    [Fact]
    public void DuplicateContextBehavior_Ignore_LeavesExistingPositionUnchanged()
    {
        var system = CreateSystem();
        try
        {
            var gameplay = Context("Gameplay");
            var menu = Context("Menu");

            system.DuplicateContextBehavior = DuplicateContextBehavior.Ignore;

            system.AddContext(gameplay);
            system.AddContext(menu);
            system.AddContext(gameplay); // re-push with Ignore — should be a no-op

            system.GetCurrentContext().Should().Be(menu, "Ignore should leave Gameplay's original (lower) position untouched");
        }
        finally
        {
            system.QueueFree();
        }
    }

    [Fact]
    public void DuplicateContextBehavior_Replace_DoesNotEmitRedundantActiveContextChanged_WhenAlreadyOnTop()
    {
        var system = CreateSystem();
        try
        {
            var gameplay = Context("Gameplay");
            system.AddContext(gameplay); // gameplay becomes top

            int fireCount = 0;
            system.Connect(EnhancedInputSystem.SignalName.ActiveContextChanged,
                Callable.From((InputMappingContext ctx) => fireCount++));

            system.AddContext(gameplay); // re-push while already on top

            fireCount.Should().Be(0, "the topmost context didn't actually change");
        }
        finally
        {
            system.QueueFree();
        }
    }
}
