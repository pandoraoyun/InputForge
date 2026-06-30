using FluentAssertions;
using InputForge;

namespace InputForge.Tests.Unit;

[Collection("InputForge")]
public class InputActionTests(InputForgeTestFixture fixture)
{
    [Fact]
    public void Equals_IsTrue_ForSameName()
    {
        var a = new InputAction { ActionName = "Jump" };
        var b = new InputAction { ActionName = "Jump" };
        a.Equals(b).Should().BeTrue();
    }

    [Fact]
    public void Equals_IsCaseInsensitive()
    {
        var a = new InputAction { ActionName = "Jump" };
        var b = new InputAction { ActionName = "JUMP" };
        a.Equals(b).Should().BeTrue();
    }

    [Fact]
    public void Equals_IsFalse_ForDifferentNames()
    {
        var a = new InputAction { ActionName = "Jump" };
        var b = new InputAction { ActionName = "Fire" };
        a.Equals(b).Should().BeFalse();
    }

    [Fact]
    public void Equals_IsFalse_AgainstNonInputAction()
    {
        var a = new InputAction { ActionName = "Jump" };
        a.Equals("Jump").Should().BeFalse("a raw string is not an InputAction");
    }

    [Fact]
    public void GetHashCode_Matches_ForCaseInsensitiveEqualNames()
    {
        var a = new InputAction { ActionName = "Jump" };
        var b = new InputAction { ActionName = "jump" };
        a.GetHashCode().Should().Be(b.GetHashCode());
    }

    [Fact]
    public void ImplicitStringConversion_ReturnsActionName()
    {
        var action = new InputAction { ActionName = "Dash" };
        string name = action;
        name.Should().Be("Dash");
    }

    [Fact]
    public void ImplicitStringConversion_OfNull_ReturnsEmptyString()
    {
        InputAction action = null;
        string name = action;
        name.Should().Be(string.Empty);
    }
}
