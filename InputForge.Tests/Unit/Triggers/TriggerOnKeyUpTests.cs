using FluentAssertions;
using Godot;
using InputForge.Triggers;

namespace InputForge.Tests.Unit.Triggers;

[Collection("InputForge")]
public class TriggerOnKeyUpTests(InputForgeTestFixture fixture)
{
    private static Vector3 Active => new(1f, 0f, 0f);
    private static Vector3 Inactive => Vector3.Zero;

    [Fact]
    public void Evaluate_DoesNotFire_OnInitialPress()
    {
        var trigger = new TriggerOnKeyUp();
        // First time active — rising edge, not a release.
        trigger.Evaluate(Active, null).Should().BeFalse();
    }

    [Fact]
    public void Evaluate_Fires_OnFallingEdge_WhenValueReturnsToZero()
    {
        var trigger = new TriggerOnKeyUp();
        trigger.Evaluate(Active, null);   // press (no fire)
        trigger.Evaluate(Inactive, null)  // release -> fire
            .Should().BeTrue();
    }

    [Fact]
    public void Evaluate_DoesNotFire_WhileHeld()
    {
        var trigger = new TriggerOnKeyUp();
        trigger.Evaluate(Active, null);              // press
        trigger.Evaluate(Active, null).Should().BeFalse(); // still held — no release yet
    }

    [Fact]
    public void Evaluate_DoesNotFire_OnSecondConsecutiveZero()
    {
        var trigger = new TriggerOnKeyUp();
        trigger.Evaluate(Active, null);   // press
        trigger.Evaluate(Inactive, null); // release -> fire
        trigger.Evaluate(Inactive, null)  // still zero — only the edge fires, not the flat
            .Should().BeFalse();
    }

    [Fact]
    public void Evaluate_DoesNotFire_FromInitialZeroState()
    {
        var trigger = new TriggerOnKeyUp();
        // Never pressed; a zero on a fresh trigger is not a falling edge.
        trigger.Evaluate(Inactive, null).Should().BeFalse();
    }

    [Fact]
    public void Evaluate_FiresAgain_AcrossSeparatePressReleaseCycles()
    {
        var trigger = new TriggerOnKeyUp();
        trigger.Evaluate(Active, null);
        trigger.Evaluate(Inactive, null).Should().BeTrue();  // first release
        trigger.Evaluate(Active, null);
        trigger.Evaluate(Inactive, null).Should().BeTrue();  // second release
    }

    [Fact]
    public void Reset_ClearsActiveState_SoNextZeroIsNotTreatedAsRelease()
    {
        var trigger = new TriggerOnKeyUp();
        trigger.Evaluate(Active, null);  // now _previousActive = true
        trigger.Reset();                 // clears it
        // Without Reset this zero would be a falling edge and fire; after Reset it must not.
        trigger.Evaluate(Inactive, null).Should().BeFalse();
    }
}
