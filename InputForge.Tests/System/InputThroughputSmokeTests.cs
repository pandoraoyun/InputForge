using System.Diagnostics;
using FluentAssertions;
using Godot;
using InputForge.Enum;
using InputForge.Mappings;

namespace InputForge.Tests.System;

/// <summary>
/// Throughput / stability smoke tests — isolated from the default test run via the
/// "Smoke" trait. Run normally: `dotnet test` SKIPS these. Run only these:
/// `dotnet test --filter "Category=Smoke"`. Run everything except these (explicit):
/// `dotnet test --filter "Category!=Smoke"`.
/// </summary>
[Trait("Category", "Smoke")]
[Collection("InputForge")]
public class InputThroughputSmokeTests
{
    private readonly SceneTree _tree;

    public InputThroughputSmokeTests(InputForgeTestFixture fixture)
    {
        _tree = fixture.Tree
            ?? throw new InvalidOperationException("InputForgeTestFixture.Tree is null.");
    }

    private EnhancedInputSystem CreateSystem()
    {
        var system = new EnhancedInputSystem();
        _tree.Root.AddChild(system);
        return system;
    }

    private static InputEventKey KeyEvent(Key key, bool pressed) => new()
    {
        Keycode = key,
        Pressed = pressed,
        Echo = false
    };

    private static InputKey BooleanKey(Key key) => new()
    {
        InputType = InputType.Boolean,
        DeviceType = InputDeviceType.Keyboard,
        KeyboardKey = key
    };

    private static (List<InputEventKey> events, int expectedPresses) BuildBurst(Key key, int eventCount)
    {
        var events = new List<InputEventKey>(eventCount);
        for (int i = 0; i < eventCount; i++)
            events.Add(KeyEvent(key, pressed: i % 2 == 0));
        return (events, events.Count(e => e.Pressed));
    }

    private sealed class FakeSubscriber
    {
        public int Id { get; }
        public int CallCount { get; private set; }
        public FakeSubscriber(int id) => Id = id;
        public void OnJump(bool pressed) { if (pressed) CallCount++; }
    }

    private sealed partial class RawInputOverrideNode : Node
    {
        public int CallCount { get; private set; }
        public override void _Input(InputEvent @event)
        {
            if (@event is InputEventKey { Keycode: Key.Space, Pressed: true })
                CallCount++;
        }
    }

    [Fact]
    public void Burst_1000Events_AllCapturedInOrder()
    {
        var system = CreateSystem();
        try
        {
            const int eventCount = 1000;
            var jump = new InputAction { ActionName = "Jump" };
            var ctx = new InputMappingContext { ContextName = "Gameplay" };
            ctx.Mappings.Add(new InputMapping { TargetAction = jump, InputSource = BooleanKey(Key.Space) });
            system.AddContext(ctx);

            int receivedCount = 0;
            var receivedOrder = new List<int>();
            ctx.BindAction(jump, (bool pressed) =>
            {
                if (!pressed) return;
                receivedOrder.Add(receivedCount);
                receivedCount++;
            });

            var (events, expectedPresses) = BuildBurst(Key.Space, eventCount);
            var stopwatch = Stopwatch.StartNew();
            foreach (var evt in events) system._Input(evt);
            stopwatch.Stop();

            double totalMs = stopwatch.Elapsed.TotalMilliseconds;
            Console.WriteLine($"[Smoke] {eventCount} events in {totalMs:F3} ms ({totalMs / eventCount:F5} ms/event)");

            receivedCount.Should().Be(expectedPresses);
            receivedOrder.Should().Equal(Enumerable.Range(0, expectedPresses));
        }
        finally
        {
            system.QueueFree();
        }
    }

    [Fact]
    public void Burst_AcrossMultipleContexts_CapturesEachActionIndependently()
    {
        var system = CreateSystem();
        try
        {
            const int eventsPerAction = 150;
            var jump = new InputAction { ActionName = "Jump" };
            var confirm = new InputAction { ActionName = "Confirm" };

            var gameplay = new InputMappingContext { ContextName = "Gameplay" };
            gameplay.Mappings.Add(new InputMapping { TargetAction = jump, InputSource = BooleanKey(Key.Space) });

            var menu = new InputMappingContext { ContextName = "Menu" };
            menu.Mappings.Add(new InputMapping { TargetAction = confirm, InputSource = BooleanKey(Key.Enter) });

            system.AddContext(gameplay);
            system.AddContext(menu);

            var jumpOrder = new List<int>();
            var confirmOrder = new List<int>();
            gameplay.BindAction(jump, (bool pressed) => { if (pressed) jumpOrder.Add(jumpOrder.Count); });
            menu.BindAction(confirm, (bool pressed) => { if (pressed) confirmOrder.Add(confirmOrder.Count); });

            var events = new List<InputEventKey>();
            for (int i = 0; i < eventsPerAction; i++)
            {
                events.Add(KeyEvent(Key.Space, true));
                events.Add(KeyEvent(Key.Space, false));
                events.Add(KeyEvent(Key.Enter, true));
                events.Add(KeyEvent(Key.Enter, false));
            }

            var stopwatch = Stopwatch.StartNew();
            foreach (var evt in events) system._Input(evt);
            stopwatch.Stop();

            Console.WriteLine($"[Smoke] {events.Count} events across 2 contexts in {stopwatch.Elapsed.TotalMilliseconds:F3} ms");

            jumpOrder.Should().HaveCount(eventsPerAction);
            confirmOrder.Should().HaveCount(eventsPerAction);
            jumpOrder.Should().BeInAscendingOrder();
            confirmOrder.Should().BeInAscendingOrder();
        }
        finally
        {
            system.QueueFree();
        }
    }

    [Fact]
    public void MultipleListeners_OnSameAction_AllInvoked_InRegistrationOrder()
    {
        var system = CreateSystem();
        try
        {
            const int listenerCount = 10;
            const int pressCount = 100;
            var jump = new InputAction { ActionName = "Jump" };
            var ctx = new InputMappingContext { ContextName = "Gameplay" };
            ctx.Mappings.Add(new InputMapping { TargetAction = jump, InputSource = BooleanKey(Key.Space) });
            system.AddContext(ctx);

            var callLog = new List<(int listenerIndex, int globalSeq)>();
            int globalSeq = 0;
            for (int i = 0; i < listenerCount; i++)
            {
                int capturedIndex = i;
                ctx.BindAction(jump, (bool pressed) =>
                {
                    if (!pressed) return;
                    callLog.Add((capturedIndex, globalSeq++));
                });
            }

            var (events, expectedPresses) = BuildBurst(Key.Space, pressCount * 2);
            var stopwatch = Stopwatch.StartNew();
            foreach (var evt in events) system._Input(evt);
            stopwatch.Stop();

            int totalInvocations = listenerCount * pressCount;
            Console.WriteLine($"[Smoke] {listenerCount} listeners x {pressCount} presses = {totalInvocations} invocations in {stopwatch.Elapsed.TotalMilliseconds:F3} ms");

            callLog.Should().HaveCount(totalInvocations);

            for (int press = 0; press < pressCount; press++)
            {
                var thisPressCalls = callLog.Skip(press * listenerCount).Take(listenerCount).Select(c => c.listenerIndex).ToList();
                thisPressCalls.Should().Equal(Enumerable.Range(0, listenerCount));
            }
        }
        finally
        {
            system.QueueFree();
        }
    }

    /// <summary>
    /// STABILITY LOG — not a pass/fail timing test. Compares InputForge's BindAction
    /// dispatch vs N plain Godot Nodes each overriding _Input() with the same if-check,
    /// ramping handler count 10 -> 500 at a fixed 300-event burst.
    /// </summary>
    [Fact]
    public void StabilityLog_InputForge_vs_RawInputOverride_ByHandlerCount()
    {
        var system = CreateSystem();
        try
        {
            const int eventCount = 300;
            int[] handlerCounts = { 10, 25, 50, 100, 250, 500 };

            Console.WriteLine();
            Console.WriteLine("[Stability] handlers |  InputForge ms_total  ms/handler  |  RawInput ms_total  ms/handler  |  ratio");
            Console.WriteLine("[Stability] -------------------------------------------------------------------------------------");

            foreach (var handlerCount in handlerCounts)
            {
                var (events, expectedPresses) = BuildBurst(Key.Space, eventCount);

                var action = new InputAction { ActionName = $"Jump_{handlerCount}" };
                var ctx = new InputMappingContext { ContextName = $"Gameplay_{handlerCount}" };
                ctx.Mappings.Add(new InputMapping { TargetAction = action, InputSource = BooleanKey(Key.Space) });
                system.AddContext(ctx);

                var subscribers = Enumerable.Range(0, handlerCount).Select(id => new FakeSubscriber(id)).ToList();
                foreach (var subscriber in subscribers)
                    ctx.BindAction(action, subscriber.OnJump);

                var forgeStopwatch = Stopwatch.StartNew();
                foreach (var evt in events) system._Input(evt);
                forgeStopwatch.Stop();

                foreach (var subscriber in subscribers)
                    subscriber.CallCount.Should().Be(expectedPresses);

                system.RemoveContext(ctx);

                var rawNodes = new List<RawInputOverrideNode>(handlerCount);
                for (int i = 0; i < handlerCount; i++)
                {
                    var node = new RawInputOverrideNode();
                    _tree.Root.AddChild(node);
                    rawNodes.Add(node);
                }

                var rawStopwatch = Stopwatch.StartNew();
                foreach (var evt in events)
                    foreach (var node in rawNodes)
                        node._Input(evt);
                rawStopwatch.Stop();

                foreach (var node in rawNodes)
                    node.CallCount.Should().Be(expectedPresses);

                foreach (var node in rawNodes)
                    node.QueueFree();

                double forgeMs = forgeStopwatch.Elapsed.TotalMilliseconds;
                double rawMs = rawStopwatch.Elapsed.TotalMilliseconds;
                double forgeMsPerHandler = forgeMs / handlerCount;
                double rawMsPerHandler = rawMs / handlerCount;
                double ratio = forgeMsPerHandler > 0.0000001 ? rawMsPerHandler / forgeMsPerHandler : 0;

                Console.WriteLine(
                    $"[Stability] {handlerCount,8} |  {forgeMs,17:F4} {forgeMsPerHandler,11:F6}  |  "
                    + $"{rawMs,14:F4} {rawMsPerHandler,11:F6}  |  {ratio,5:F2}x");
            }

            Console.WriteLine("[Stability] -------------------------------------------------------------------------------------");
            Console.WriteLine();
        }
        finally
        {
            system.QueueFree();
        }
    }
}