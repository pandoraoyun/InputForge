using Godot;

namespace InputForge.Triggers;

/// <summary>
/// Base class for all input triggers.
/// Triggers decide whether a processed value should cause an action to fire.
/// When no trigger is assigned to a mapping, a default trigger is selected
/// automatically based on the input type.
/// </summary>
[GlobalClass]
public abstract partial class InputTrigger : Resource
{
    /// <summary>
    /// Evaluates whether the action should fire given the current value and event.
    /// </summary>
    /// <param name="value">The value after all modifiers have been applied.</param>
    /// <param name="event">The raw Godot input event.</param>
    /// <returns>True if the action should fire this frame.</returns>
    public abstract bool Evaluate(Vector3 value, InputEvent @event);
}
