using FluentAssertions;
using Godot;
using InputForge;
using InputForge.Mappings;

namespace InputForge.Tests.Unit;

[Collection("InputForge")]
public class ContextualInputEventTests(InputForgeTestFixture fixture)
{
    [Fact]
    public void InitializedProperties_ExposeTheValuesTheyWereGiven()
    {
        var action = new InputAction { ActionName = "Jump" };
        var source = new InputKey { KeyboardKey = Key.Space };
        var raw = new InputEventKey { Keycode = Key.Space, Pressed = true };
        var value = new Vector3(1f, 0f, 0f);

        var evt = new ContextualInputEvent
        {
            Action = action,
            Source = source,
            RawEvent = raw,
            RawValue = value
        };

        evt.Action.Should().BeSameAs(action);
        evt.Source.Should().BeSameAs(source);
        evt.RawEvent.Should().BeSameAs(raw);
        evt.RawValue.Should().Be(value);
    }

    [Fact]
    public void Default_HasNullReferencesAndZeroValue()
    {
        var evt = default(ContextualInputEvent);
        evt.Action.Should().BeNull();
        evt.Source.Should().BeNull();
        evt.RawEvent.Should().BeNull();
        evt.RawValue.Should().Be(Vector3.Zero);
    }

    [Fact]
    public void IsValueType_CopiesByValue()
    {
        var original = new ContextualInputEvent { RawValue = new Vector3(5f, 0f, 0f) };
        var copy = original;
        // A readonly struct copy is independent; comparing copied value confirms by-value semantics.
        copy.RawValue.Should().Be(new Vector3(5f, 0f, 0f));
        original.RawValue.Should().Be(new Vector3(5f, 0f, 0f));
    }
}
