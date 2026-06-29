using Godot;

namespace InputForge;

/// <summary>
/// Carries the result of a processed input mapping to subscriber callbacks.
/// Produced by <see cref="EnhancedInputSystem"/> and delivered via <see cref="InputMappingContext.PushAction"/>.
/// </summary>
public readonly struct ContextualInputEvent
{
    /// <summary>The action that triggered this event.</summary>
    public InputAction Action { get; init; }

    /// <summary>The raw Godot input event before any processing.</summary>
    public InputEvent RawEvent { get; init; }

    /// <summary>The value after all modifiers have been applied.</summary>
    public Vector3 RawValue { get; init; }
}
