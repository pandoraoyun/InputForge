using FluentAssertions;
using Godot;
using InputForge.Triggers;

namespace InputForge.Tests.Unit.Triggers;

[Collection("InputForge")]
public class TriggerContinuousTests(InputForgeTestFixture fixture)
{
    [Fact]
    public void Evaluate_Fires_WhenValueIsNonZero()
    {
        var trigger = new TriggerContinuous();
        trigger.Evaluate(new Vector3(1f, 0f, 0f), null).Should().BeTrue();
    }

    [Fact]
    public void Evaluate_DoesNotFire_WhenValueIsZero()
    {
        var trigger = new TriggerContinuous();
        trigger.Evaluate(Vector3.Zero, null).Should().BeFalse();
    }

    [Fact]
    public void Evaluate_FiresEveryCall_WhileValueStaysNonZero()
    {
        var trigger = new TriggerContinuous();
        // No edge detection — must fire on every consecutive non-zero evaluation.
        trigger.Evaluate(new Vector3(0.5f, 0f, 0f), null).Should().BeTrue();
        trigger.Evaluate(new Vector3(0.5f, 0f, 0f), null).Should().BeTrue();
        trigger.Evaluate(new Vector3(0.5f, 0f, 0f), null).Should().BeTrue();
    }

    [Fact]
    public void Evaluate_Fires_OnAnyNonZeroAxis()
    {
        var trigger = new TriggerContinuous();
        trigger.Evaluate(new Vector3(0f, 0f, 0.01f), null)
            .Should().BeTrue("non-zero length on any axis counts as active");
    }
}
