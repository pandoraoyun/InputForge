using Godot;
using InputForge.Mappings;

namespace InputForge;

/// <summary>
/// Carries the result of a processed input mapping to subscriber callbacks.
/// Produced by <see cref="EnhancedInputSystem"/> and delivered via <see cref="InputMappingContext.PushAction"/>.
/// </summary>
public readonly struct ContextualInputEvent
{
    /// <summary>The action that triggered this event.</summary>
    public InputAction Action { get; init; }

    /// <summary>
    /// The <see cref="InputKey"/> mapping that physically produced this event — i.e. which
    /// source fired. Lets a callback bound to a single action distinguish between multiple
    /// mappings driving it (e.g. WASD vs. mouse delta both feeding one Move action), by
    /// inspecting Source.InputType / Source.DeviceType / the bound key. May be null if the
    /// event was produced without an originating mapping.
    /// </summary>
    public InputKey Source { get; init; }

    /// <summary>The raw Godot input event before any processing.</summary>
    public InputEvent RawEvent { get; init; }

    /// <summary>The value after all modifiers have been applied.</summary>
    public Vector3 RawValue { get; init; }
}
