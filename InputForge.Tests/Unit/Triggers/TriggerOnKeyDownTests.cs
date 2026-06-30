using FluentAssertions;
using Godot;
using InputForge.Triggers;

namespace InputForge.Tests.Unit.Triggers;

[Collection("InputForge")]
public class TriggerOnKeyDownTests(InputForgeTestFixture fixture)
{
    [Fact]
    public void Evaluate_FiresOnce_WhenValueGoesFromZeroToNonZero()
    {
        var trigger = new TriggerOnKeyDown();

        var result = trigger.Evaluate(new Vector3(1f, 0f, 0f), null);

        result.Should().BeTrue();
    }

    [Fact]
    public void Evaluate_DoesNotFireAgain_WhileValueStaysNonZero()
    {
        var trigger = new TriggerOnKeyDown();
        trigger.Evaluate(new Vector3(1f, 0f, 0f), null); // rising edge consumed

        var result = trigger.Evaluate(new Vector3(1f, 0f, 0f), null);

        result.Should().BeFalse("holding the key should not retrigger");
    }

    [Fact]
    public void Evaluate_FiresAgain_AfterReturningToZeroThenNonZero()
    {
        var trigger = new TriggerOnKeyDown();
        trigger.Evaluate(new Vector3(1f, 0f, 0f), null); // press
        trigger.Evaluate(Vector3.Zero, null);             // release

        var result = trigger.Evaluate(new Vector3(1f, 0f, 0f), null); // press again

        result.Should().BeTrue();
    }

    [Fact]
    public void Evaluate_DoesNotFire_WhenValueStaysZero()
    {
        var trigger = new TriggerOnKeyDown();

        var result = trigger.Evaluate(Vector3.Zero, null);

        result.Should().BeFalse();
    }
}
