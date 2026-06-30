using FluentAssertions;
using Godot;
using InputForge.Triggers;

namespace InputForge.Tests.Unit.Triggers;

[Collection("InputForge")]
public class TriggerOnChangeTests(InputForgeTestFixture fixture)
{
    [Fact]
    public void Evaluate_FiresOnFirstCall_WhenValueDiffersFromInitialZero()
    {
        var trigger = new TriggerOnChange();

        var result = trigger.Evaluate(new Vector3(0.5f, 0f, 0f), null);

        result.Should().BeTrue();
    }

    [Fact]
    public void Evaluate_DoesNotFire_WhenValueIsUnchanged()
    {
        var trigger = new TriggerOnChange();
        trigger.Evaluate(new Vector3(0.5f, 0f, 0f), null);

        var result = trigger.Evaluate(new Vector3(0.5f, 0f, 0f), null);

        result.Should().BeFalse();
    }

    [Fact]
    public void Evaluate_Fires_WhenValueChangesToZero()
    {
        // This is the case our mouse-delta-stops-moving regression depends on:
        // the trigger must fire on the transition back to zero too, so subscribers
        // are notified that motion has stopped.
        var trigger = new TriggerOnChange();
        trigger.Evaluate(new Vector3(0.5f, 0f, 0f), null);

        var result = trigger.Evaluate(Vector3.Zero, null);

        result.Should().BeTrue("releasing/stopping motion must still notify subscribers");
    }

    [Fact]
    public void Evaluate_Fires_OnEveryDistinctValue_ForContinuousAxisMotion()
    {
        var trigger = new TriggerOnChange();

        trigger.Evaluate(new Vector3(0.1f, 0f, 0f), null).Should().BeTrue();
        trigger.Evaluate(new Vector3(0.2f, 0f, 0f), null).Should().BeTrue();
        trigger.Evaluate(new Vector3(0.2f, 0f, 0f), null).Should().BeFalse("unchanged value should not refire");
        trigger.Evaluate(new Vector3(0.3f, 0f, 0f), null).Should().BeTrue();
    }
}