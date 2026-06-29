using Godot;
using Godot.Collections;
using InputForge.Enum;
using InputForge.Modifiers;
using InputForge.Triggers;
using InputForge.Mappings;

namespace InputForge;

/// <summary>
/// Binds an <see cref="InputKey"/> source to a target <see cref="InputAction"/>,
/// with an optional modifier and trigger pipeline.
/// </summary>
[GlobalClass]
public partial class InputMapping : Resource
{
    [Export] public InputAction TargetAction { get; set; }
    [Export] public InputKey InputSource { get; set; }

    /// <summary>Modifiers applied to the raw value before trigger evaluation.</summary>
    [Export] public Array<InputModifier> LocalModifiers { get; set; } = new();

    /// <summary>Triggers that decide whether the action fires. Empty list uses the default trigger.</summary>
    [Export] public Array<InputTrigger> LocalTriggers { get; set; } = new();

    // Lazily initialized default triggers to avoid allocation on every frame.
    private TriggerOnKeyDown _defaultBooleanTrigger;
    private TriggerOnChange _defaultAxisTrigger;

    /// <summary>Applies all modifiers in order. Returns the raw value if the list is empty.</summary>
    public Vector3 ApplyModifiers(Vector3 value)
    {
        foreach (var modifier in LocalModifiers)
            value = modifier.Apply(value);
        return value;
    }

    /// <summary>
    /// Evaluates all triggers using OR logic — fires if any trigger returns true.
    /// Falls back to the default trigger when the list is empty:
    /// Boolean input uses <see cref="TriggerOnKeyDown"/>, axis input uses <see cref="TriggerOnChange"/>.
    /// </summary>
    public bool EvaluateTriggers(Vector3 value, InputEvent @event)
    {
        if (LocalTriggers.Count == 0)
            return DefaultTrigger(value, @event);

        foreach (var trigger in LocalTriggers)
            if (trigger.Evaluate(value, @event)) return true;

        return false;
    }

    private bool DefaultTrigger(Vector3 value, InputEvent @event)
    {
        if (InputSource == null) return false;

        if (InputSource.InputType == InputType.Boolean)
        {
            _defaultBooleanTrigger ??= new TriggerOnKeyDown();
            return _defaultBooleanTrigger.Evaluate(value, @event);
        }

        _defaultAxisTrigger ??= new TriggerOnChange();
        return _defaultAxisTrigger.Evaluate(value, @event);
    }
}
