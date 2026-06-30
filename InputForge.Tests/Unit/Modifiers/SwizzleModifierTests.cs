using FluentAssertions;
using Godot;
using InputForge.Modifiers;

namespace InputForge.Tests.Unit.Modifiers;

[Collection("InputForge")]
public class SwizzleModifierTests(InputForgeTestFixture fixture)
{
    // Use a distinct value per component so any reordering is unambiguous.
    private static readonly Vector3 Sample = new(1f, 2f, 3f);

    [Fact]
    public void Apply_WithDefaultOrder_SwapsXAndY()
    {
        // Default Order = YXZ.
        var modifier = new SwizzleModifier();
        var result = modifier.Apply(Sample);
        result.Should().Be(new Vector3(2f, 1f, 3f));
    }

    [Fact]
    public void Apply_YXZ_SwapsXAndY()
    {
        var modifier = new SwizzleModifier { Order = SwizzleModifier.SwizzleOrder.YXZ };
        modifier.Apply(Sample).Should().Be(new Vector3(2f, 1f, 3f));
    }

    [Fact]
    public void Apply_XZY_SwapsYAndZ()
    {
        var modifier = new SwizzleModifier { Order = SwizzleModifier.SwizzleOrder.XZY };
        modifier.Apply(Sample).Should().Be(new Vector3(1f, 3f, 2f));
    }

    [Fact]
    public void Apply_ZYX_SwapsXAndZ()
    {
        var modifier = new SwizzleModifier { Order = SwizzleModifier.SwizzleOrder.ZYX };
        modifier.Apply(Sample).Should().Be(new Vector3(3f, 2f, 1f));
    }

    [Fact]
    public void Apply_ZXY_RotatesComponents()
    {
        var modifier = new SwizzleModifier { Order = SwizzleModifier.SwizzleOrder.ZXY };
        modifier.Apply(Sample).Should().Be(new Vector3(3f, 1f, 2f));
    }

    [Fact]
    public void Apply_YZX_RotatesComponents()
    {
        var modifier = new SwizzleModifier { Order = SwizzleModifier.SwizzleOrder.YZX };
        modifier.Apply(Sample).Should().Be(new Vector3(2f, 3f, 1f));
    }
}
