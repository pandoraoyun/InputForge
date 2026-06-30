using FluentAssertions;
using Godot;
using InputForge.Enum;
using InputForge.Modifiers;

namespace InputForge.Tests.Unit.Modifiers;

[Collection("InputForge")]
public class PointerSpaceModifierTests(InputForgeTestFixture fixture)
{
    // --- RelativeToRect: pure math, fully deterministic regardless of headless environment ---

    [Fact]
    public void Apply_RelativeToRect_WhenValueIsRectTopLeft_ReturnsZeroZero()
    {
        var modifier = new PointerSpaceModifier
        {
            Space = PointerSpace.RelativeToRect,
            TargetRect = new Rect2(100, 50, 800, 600)
        };

        var result = modifier.Apply(new Vector3(100f, 50f, 0f));

        result.Should().Be(new Vector3(0f, 0f, 0f));
    }

    [Fact]
    public void Apply_RelativeToRect_WhenValueIsRectBottomRight_ReturnsOneOne()
    {
        var modifier = new PointerSpaceModifier
        {
            Space = PointerSpace.RelativeToRect,
            TargetRect = new Rect2(100, 50, 800, 600)
        };

        var result = modifier.Apply(new Vector3(900f, 650f, 0f));

        result.X.Should().BeApproximately(1f, 0.0001f);
        result.Y.Should().BeApproximately(1f, 0.0001f);
    }

    [Fact]
    public void Apply_RelativeToRect_WhenValueIsRectCenter_ReturnsHalfHalf()
    {
        var modifier = new PointerSpaceModifier
        {
            Space = PointerSpace.RelativeToRect,
            TargetRect = new Rect2(0, 0, 1000, 1000)
        };

        var result = modifier.Apply(new Vector3(500f, 500f, 0f));

        result.X.Should().BeApproximately(0.5f, 0.0001f);
        result.Y.Should().BeApproximately(0.5f, 0.0001f);
    }

    [Fact]
    public void Apply_RelativeToRect_WhenValueIsOutsideRect_IsNotClamped()
    {
        var modifier = new PointerSpaceModifier
        {
            Space = PointerSpace.RelativeToRect,
            TargetRect = new Rect2(0, 0, 100, 100)
        };

        // 200 is twice the rect width past the origin — should report 2.0, not clamp to 1.0.
        var result = modifier.Apply(new Vector3(200f, -50f, 0f));

        result.X.Should().BeApproximately(2f, 0.0001f);
        result.Y.Should().BeApproximately(-0.5f, 0.0001f);
    }

    [Fact]
    public void Apply_RelativeToRect_WhenRectWidthIsZero_DoesNotDivideByZero()
    {
        var modifier = new PointerSpaceModifier
        {
            Space = PointerSpace.RelativeToRect,
            TargetRect = new Rect2(0, 0, 0, 100)
        };

        var act = () => modifier.Apply(new Vector3(50f, 50f, 0f));

        act.Should().NotThrow();
        var result = modifier.Apply(new Vector3(50f, 50f, 0f));
        result.X.Should().Be(0f, "a zero-width rect has no valid horizontal ratio to report");
    }

    [Fact]
    public void Apply_RelativeToRect_WhenRectHeightIsZero_DoesNotDivideByZero()
    {
        var modifier = new PointerSpaceModifier
        {
            Space = PointerSpace.RelativeToRect,
            TargetRect = new Rect2(0, 0, 100, 0)
        };

        var act = () => modifier.Apply(new Vector3(50f, 50f, 0f));

        act.Should().NotThrow();
        var result = modifier.Apply(new Vector3(50f, 50f, 0f));
        result.Y.Should().Be(0f, "a zero-height rect has no valid vertical ratio to report");
    }

    [Fact]
    public void Apply_RelativeToRect_PreservesZComponentAsZero()
    {
        var modifier = new PointerSpaceModifier
        {
            Space = PointerSpace.RelativeToRect,
            TargetRect = new Rect2(0, 0, 100, 100)
        };

        var result = modifier.Apply(new Vector3(50f, 50f, 999f));

        result.Z.Should().Be(0f, "pointer space is inherently 2D — Z should never carry through");
    }

    // --- ScreenSpace / RelativeToScreen: depend on DisplayServer / SceneTree.Root, which
    // report dummy/zero values in the headless 2dog test runtime. These tests only assert
    // the modifier doesn't throw and returns a structurally valid (non-NaN) Vector3 — actual
    // screen-resolution-dependent values can't be asserted deterministically here. ---

    [Fact]
    public void Apply_ScreenSpace_DoesNotThrow_AndReturnsFiniteVector()
    {
        var modifier = new PointerSpaceModifier { Space = PointerSpace.ScreenSpace };

        var act = () => modifier.Apply(new Vector3(123f, 456f, 0f));

        act.Should().NotThrow();
        var result = modifier.Apply(new Vector3(123f, 456f, 0f));
        result.Z.Should().Be(0f);
        float.IsNaN(result.X).Should().BeFalse();
        float.IsNaN(result.Y).Should().BeFalse();
    }

    [Fact]
    public void Apply_RelativeToScreen_DoesNotThrow_AndDoesNotDivideByZero()
    {
        var modifier = new PointerSpaceModifier { Space = PointerSpace.RelativeToScreen };

        // Even if DisplayServer reports a zero-size screen in this headless environment,
        // the modifier should guard against dividing by zero rather than producing NaN/Inf.
        var act = () => modifier.Apply(new Vector3(123f, 456f, 0f));

        act.Should().NotThrow();
        var result = modifier.Apply(new Vector3(123f, 456f, 0f));
        float.IsNaN(result.X).Should().BeFalse();
        float.IsNaN(result.Y).Should().BeFalse();
        float.IsInfinity(result.X).Should().BeFalse();
        float.IsInfinity(result.Y).Should().BeFalse();
    }

    // --- Defaults ---

    [Fact]
    public void Space_DefaultsToRelativeToScreen()
    {
        var modifier = new PointerSpaceModifier();

        modifier.Space.Should().Be(PointerSpace.RelativeToScreen);
    }

    [Fact]
    public void TargetRect_HasNonZeroDefault()
    {
        var modifier = new PointerSpaceModifier();

        modifier.TargetRect.Size.X.Should().BeGreaterThan(0f);
        modifier.TargetRect.Size.Y.Should().BeGreaterThan(0f);
    }
}
