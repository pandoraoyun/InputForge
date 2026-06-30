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

    /// <summary>
    /// Resets any edge/change-detection state this trigger holds (e.g. "was this active
    /// last event") back to its clean default. Invoked from this trigger's own
    /// ActiveContextChanged handler (wired up in <see cref="Bind"/>): whenever the active
    /// context stack changes, every bound trigger returns to a known-clean baseline.
    ///
    /// This fixes the "missed release" failure mode — while a context is shadowed (e.g.
    /// beneath a modal context that blocked input via PreventFallbackContext), it never
    /// sees the release event, so its "previous value" goes stale and the next press can
    /// be swallowed. Resetting on every context change sidesteps that entirely.
    ///
    /// Default no-op — only triggers that hold edge/change state (TriggerOnKeyDown,
    /// TriggerOnKeyUp, TriggerOnChange) override this.
    /// </summary>
    public virtual void Reset() { }

    /// <summary>
    /// Subscribes this trigger to EnhancedInputSystem's global ActiveContextChanged
    /// signal so it resets its edge-state on every stack change. Reached through the
    /// singleton — no system reference needed. Called by InputMappingContext when its
    /// context is pushed onto the active stack. Stateless triggers inherit this too;
    /// their Reset() is a no-op, so the subscription simply does nothing.
    /// </summary>
    public void Bind()
    {
        var system = EnhancedInputSystem.GetInstance();
        if (system != null) system.ActiveContextChanged += OnActiveContextChanged;
    }

    /// <summary>Counterpart to <see cref="Bind"/>; called when the context is popped. Unsubscribes.</summary>
    public void Unbind()
    {
        var system = EnhancedInputSystem.GetInstance();
        if (system != null) system.ActiveContextChanged -= OnActiveContextChanged;
    }

    private void OnActiveContextChanged(InputMappingContext _) => Reset();
}
