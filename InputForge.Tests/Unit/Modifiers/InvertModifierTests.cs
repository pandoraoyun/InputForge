using FluentAssertions;
using Godot;
using InputForge.Modifiers;

namespace InputForge.Tests.Unit.Modifiers;

[Collection("InputForge")]
public class InvertModifierTests(InputForgeTestFixture fixture)
{
    [Fact]
    public void Apply_WithDefaults_InvertsOnlyX()
    {
        // Defaults: InvertX = true, InvertY = false, InvertZ = false.
        var modifier = new InvertModifier();
        var result = modifier.Apply(new Vector3(1f, 2f, 3f));
        result.Should().Be(new Vector3(-1f, 2f, 3f));
    }

    [Fact]
    public void Apply_WhenAllAxesInverted_NegatesEveryComponent()
    {
        var modifier = new InvertModifier { InvertX = true, InvertY = true, InvertZ = true };
        var result = modifier.Apply(new Vector3(1f, -2f, 3f));
        result.Should().Be(new Vector3(-1f, 2f, -3f));
    }

    [Fact]
    public void Apply_WhenNoAxesInverted_ReturnsValueUnchanged()
    {
        var modifier = new InvertModifier { InvertX = false, InvertY = false, InvertZ = false };
        var value = new Vector3(1f, 2f, 3f);
        var result = modifier.Apply(value);
        result.Should().Be(value);
    }

    [Fact]
    public void Apply_InvertsOnlySelectedAxes()
    {
        // Only Y inverted — X and Z must pass through untouched.
        var modifier = new InvertModifier { InvertX = false, InvertY = true, InvertZ = false };
        var result = modifier.Apply(new Vector3(5f, 5f, 5f));
        result.Should().Be(new Vector3(5f, -5f, 5f));
    }

    [Fact]
    public void Apply_OnZeroVector_ReturnsZero()
    {
        var modifier = new InvertModifier { InvertX = true, InvertY = true, InvertZ = true };
        var result = modifier.Apply(Vector3.Zero);
        result.Should().Be(Vector3.Zero);
    }
}
