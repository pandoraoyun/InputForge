using FluentAssertions;
using Godot;
using InputForge.Modifiers;

namespace InputForge.Tests.Unit.Modifiers;

[Collection("InputForge")]
public class NormalizeModifierTests(InputForgeTestFixture fixture)
{
    [Fact]
    public void Apply_OnUnitLengthVector_ReturnsItUnchanged()
    {
        var modifier = new NormalizeModifier();
        var result = modifier.Apply(new Vector3(1f, 0f, 0f));
        result.X.Should().BeApproximately(1f, 1e-5f);
        result.Y.Should().BeApproximately(0f, 1e-5f);
        result.Z.Should().BeApproximately(0f, 1e-5f);
    }

    [Fact]
    public void Apply_OnLongerVector_ScalesToUnitLength()
    {
        var modifier = new NormalizeModifier();
        var result = modifier.Apply(new Vector3(3f, 4f, 0f)); // length 5
        result.Length().Should().BeApproximately(1f, 1e-5f);
        // Direction preserved: 3/5, 4/5.
        result.X.Should().BeApproximately(0.6f, 1e-5f);
        result.Y.Should().BeApproximately(0.8f, 1e-5f);
    }

    [Fact]
    public void Apply_OnDiagonal_ProducesUnitLength_PreventingFasterDiagonalMovement()
    {
        // The whole point of Normalize: (1,1,0) has length √2 ≈ 1.41; must become length 1.
        var modifier = new NormalizeModifier();
        var result = modifier.Apply(new Vector3(1f, 1f, 0f));
        result.Length().Should().BeApproximately(1f, 1e-5f);
    }

    [Fact]
    public void Apply_OnZeroVector_ReturnsZero_WithoutProducingNaN()
    {
        // Below threshold -> clamped to zero rather than normalizing a zero vector (NaN guard).
        var modifier = new NormalizeModifier();
        var result = modifier.Apply(Vector3.Zero);
        result.Should().Be(Vector3.Zero);
    }

    [Fact]
    public void Apply_OnVectorBelowMinThreshold_ReturnsZero()
    {
        // Default MinValueThreshold = 0.001; a vector well below it is treated as zero.
        var modifier = new NormalizeModifier();
        var result = modifier.Apply(new Vector3(0.0001f, 0f, 0f));
        result.Should().Be(Vector3.Zero);
    }

    [Fact]
    public void Apply_WithCustomThreshold_ClampsValuesUnderIt()
    {
        var modifier = new NormalizeModifier { MinValueThreshold = 0.5f };
        // length 0.3 < 0.5 -> zero
        modifier.Apply(new Vector3(0.3f, 0f, 0f)).Should().Be(Vector3.Zero);
        // length 0.6 > 0.5 -> normalized
        modifier.Apply(new Vector3(0.6f, 0f, 0f)).Length().Should().BeApproximately(1f, 1e-5f);
    }
}
