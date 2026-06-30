using FluentAssertions;

namespace InputForge.Tests.Unit;

[Collection("InputForge")]
public class InputMappingContextEqualityTests(InputForgeTestFixture fixture)
{
    [Fact]
    public void Equality_TwoContextsWithSameName_AreEqual()
    {
        var a = new InputMappingContext { ContextName = "Gameplay" };
        var b = new InputMappingContext { ContextName = "Gameplay" };

        (a == b).Should().BeTrue();
    }

    [Fact]
    public void Equality_IsCaseInsensitive()
    {
        var a = new InputMappingContext { ContextName = "Gameplay" };
        var b = new InputMappingContext { ContextName = "GAMEPLAY" };

        (a == b).Should().BeTrue();
    }

    [Fact]
    public void Equality_DifferentNames_AreNotEqual()
    {
        var a = new InputMappingContext { ContextName = "Gameplay" };
        var b = new InputMappingContext { ContextName = "Menu" };

        (a != b).Should().BeTrue();
    }
}
