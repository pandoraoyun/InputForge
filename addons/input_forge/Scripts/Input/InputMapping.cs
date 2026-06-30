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
    /// Boolean input uses <see cref="TriggerOnKeyDown"/> (rising-edge only — fires exactly
    /// once per physical press, which is the correct/expected default for a bool "isPressed"
    /// style action). Axis input (Digital, Analog, Delta, Pointer) uses <see cref="TriggerOnChange"/>.
    ///
    /// IMPORTANT: every trigger in LocalTriggers is evaluated on every call, even after one
    /// has already returned true. Each trigger that holds its own edge/change-detection state
    /// needs to see every event to keep that state correct — short-circuiting on the first
    /// true would leave the remaining triggers' "previous value" stale, which can cause them
    /// to misfire or fail to fire on a later event that should have updated them.
    /// </summary>
    public bool EvaluateTriggers(Vector3 value, InputEvent @event)
    {
        if (LocalTriggers.Count == 0)
            return DefaultTrigger(value, @event);

        bool fired = false;
        foreach (var trigger in LocalTriggers)
        {
            // No short-circuit: every trigger must see this event to keep its own
            // internal state correct, regardless of whether an earlier trigger
            // already decided this should fire.
            if (trigger.Evaluate(value, @event)) fired = true;
        }

        return fired;
    }

    private bool DefaultTrigger(Vector3 value, InputEvent @event)
    {
        var trigger = EnsureDefaultTrigger();
        return trigger?.Evaluate(value, @event) ?? false;
    }

    /// <summary>
    /// Lazily creates and returns the default trigger appropriate for this mapping's
    /// input type — <see cref="TriggerOnKeyDown"/> for Boolean (rising-edge), otherwise
    /// <see cref="TriggerOnChange"/>. Returns null when there is no InputSource. Shared by
    /// <see cref="DefaultTrigger"/> (evaluation) and <see cref="BindTriggers"/> so the exact
    /// instance that will run is also the one subscribed to ActiveContextChanged.
    /// </summary>
    private InputTrigger EnsureDefaultTrigger()
    {
        if (InputSource == null) return null;

        if (InputSource.InputType == InputType.Boolean)
            return _defaultBooleanTrigger ??= new TriggerOnKeyDown();

        return _defaultAxisTrigger ??= new TriggerOnChange();
    }

    /// <summary>
    /// Subscribes this mapping's active triggers to EnhancedInputSystem's ActiveContextChanged
    /// signal (so they reset their edge-state whenever the active stack changes). Called by
    /// InputMappingContext when the context is pushed. Mirrors EvaluateTriggers' selection:
    /// binds the explicit LocalTriggers when present, otherwise the default trigger (created
    /// eagerly here so the subscribed instance is the same one that later evaluates).
    /// </summary>
    internal void BindTriggers()
    {
        if (LocalTriggers.Count > 0)
            foreach (var trigger in LocalTriggers) trigger?.Bind();
        else
            EnsureDefaultTrigger()?.Bind();
    }

    /// <summary>Counterpart to <see cref="BindTriggers"/>; called when the context is popped.</summary>
    internal void UnbindTriggers()
    {
        if (LocalTriggers.Count > 0)
            foreach (var trigger in LocalTriggers) trigger?.Unbind();
        else
            EnsureDefaultTrigger()?.Unbind();
    }
}
