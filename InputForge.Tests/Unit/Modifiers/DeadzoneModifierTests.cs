using FluentAssertions;
using Godot;
using InputForge.Modifiers;

namespace InputForge.Tests.Unit.Modifiers;

[Collection("InputForge")]
public class DeadzoneModifierTests(InputForgeTestFixture fixture)
{
    [Fact]
    public void Apply_WhenValueLengthBelowDeadzone_ReturnsZero()
    {
        var modifier = new DeadzoneModifier { Deadzone = 0.2f };
        var result = modifier.Apply(new Vector3(0.1f, 0f, 0f));
        result.Should().Be(Vector3.Zero);
    }

    [Fact]
    public void Apply_WhenValueLengthAboveDeadzone_ReturnsOriginalValue()
    {
        var modifier = new DeadzoneModifier { Deadzone = 0.2f };
        var value = new Vector3(0.5f, 0f, 0f);
        var result = modifier.Apply(value);
        result.Should().Be(value);
    }

    [Fact]
    public void Apply_WhenValueLengthExactlyAtDeadzone_ReturnsZero()
    {
        var modifier = new DeadzoneModifier { Deadzone = 0.2f };
        var result = modifier.Apply(new Vector3(0.2f, 0f, 0f));
        result.Should().Be(Vector3.Zero);
    }

    [Fact]
    public void Apply_WhenValueIsZero_ReturnsZero()
    {
        var modifier = new DeadzoneModifier { Deadzone = 0.2f };
        var result = modifier.Apply(Vector3.Zero);
        result.Should().Be(Vector3.Zero);
    }

    [Fact]
    public void Apply_WhenDeadzoneIsZero_ReturnsOriginalValue()
    {
        var modifier = new DeadzoneModifier { Deadzone = 0f };
        var value = new Vector3(0.001f, 0f, 0f);
        var result = modifier.Apply(value);
        result.Should().Be(value);
    }

    [Fact]
    public void Apply_WithAxis2D_WhenVectorLengthBelowDeadzone_ReturnsZero()
    {
        var modifier = new DeadzoneModifier { Deadzone = 0.2f };
        // sqrt(0.1^2 + 0.1^2) ≈ 0.141 < 0.2
        var result = modifier.Apply(new Vector3(0.1f, 0.1f, 0f));
        result.Should().Be(Vector3.Zero);
    }

    [Fact]
    public void Apply_WithAxis2D_WhenVectorLengthAboveDeadzone_ReturnsOriginalValue()
    {
        var modifier = new DeadzoneModifier { Deadzone = 0.2f };
        // sqrt(0.5^2 + 0.5^2) ≈ 0.707 > 0.2
        var value = new Vector3(0.5f, 0.5f, 0f);
        var result = modifier.Apply(value);
        result.Should().Be(value);
    }
}
