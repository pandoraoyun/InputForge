using FluentAssertions;
using Godot;
using InputForge.Modifiers;

namespace InputForge.Tests.Unit.Modifiers;

[Collection("InputForge")]
public class ScaleModifierTests(InputForgeTestFixture fixture)
{
    [Fact]
    public void Apply_WithDefaultScaleOfOne_ReturnsValueUnchanged()
    {
        // Default Scale = Vector3.One, so Apply is a no-op.
        var modifier = new ScaleModifier();
        var value = new Vector3(1f, 2f, 3f);
        var result = modifier.Apply(value);
        result.Should().Be(value);
    }

    [Fact]
    public void Apply_MultipliesEachComponentByItsScale()
    {
        var modifier = new ScaleModifier { Scale = new Vector3(2f, 3f, 4f) };
        var result = modifier.Apply(new Vector3(1f, 1f, 1f));
        result.Should().Be(new Vector3(2f, 3f, 4f));
    }

    [Fact]
    public void Apply_WithNonUniformScale_ScalesAxesIndependently()
    {
        var modifier = new ScaleModifier { Scale = new Vector3(0.5f, 2f, 0f) };
        var result = modifier.Apply(new Vector3(4f, 4f, 4f));
        result.Should().Be(new Vector3(2f, 8f, 0f));
    }

    [Fact]
    public void Apply_WithNegativeScale_InvertsAndScales()
    {
        var modifier = new ScaleModifier { Scale = new Vector3(-1f, -2f, -1f) };
        var result = modifier.Apply(new Vector3(3f, 3f, 3f));
        result.Should().Be(new Vector3(-3f, -6f, -3f));
    }

    [Fact]
    public void Apply_WithZeroScale_ReturnsZero()
    {
        var modifier = new ScaleModifier { Scale = Vector3.Zero };
        var result = modifier.Apply(new Vector3(9f, 9f, 9f));
        result.Should().Be(Vector3.Zero);
    }
}
