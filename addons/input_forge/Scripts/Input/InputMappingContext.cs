using System;
using System.Collections.Generic;
using Godot;

namespace InputForge;

/// <summary>
/// A named set of <see cref="InputMapping"/> entries that can be pushed onto or
/// popped from the <see cref="EnhancedInputSystem"/> context stack at runtime.
/// Subscribers bind typed callbacks via <c>BindAction</c> overloads and receive
/// the processed value when an action fires.
/// </summary>
[GlobalClass]
public partial class InputMappingContext : Resource
{
    [Export] public string ContextName { get; set; } = "New Context";
    [Export] public Godot.Collections.Array<InputMapping> Mappings { get; set; } = new();

    /// <summary>Emitted when <see cref="EnhancedInputSystem"/> pushes this context onto the stack.</summary>
    [Signal] public delegate void PushedEventHandler();

    /// <summary>Emitted when <see cref="EnhancedInputSystem"/> removes this context from the stack.</summary>
    [Signal] public delegate void PoppedEventHandler();

    /// <summary>
    /// Emitted when this context's position in the active stack changes — e.g. another
    /// context was pushed above or popped from above it, changing whether this context
    /// is currently the highest-priority (topmost) one. Passes whether this context is
    /// now the topmost active context.
    /// </summary>
    [Signal] public delegate void PriorityChangedEventHandler(bool isTopmost);

    /// <summary>
    /// Per-action subscriber lists, one list per callback shape. Keeping each callback type in
    /// its own list means the dispatch path (<see cref="PushAction"/>) invokes each callback
    /// directly with no per-event type switching and no wrapper allocation — the only cost is
    /// the lists themselves, allocated once when the first callback of a given type binds.
    /// </summary>
    private sealed class Subscribers
    {
        public List<Action<bool>> Bool;
        public List<Action<float>> Float;
        public List<Action<Vector2>> Vec2;
        public List<Action<Vector3>> Vec3;
        public List<Action<ContextualInputEvent>> Contextual;
    }

    // Case-insensitive dictionary keyed by action name.
    private readonly Dictionary<string, Subscribers> _actionEvents =
        new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Subscribe to an action and receive a boolean pressed/released value.</summary>
    public void BindAction(InputAction action, Action<bool> callback)
        => GetOrCreate(action, callback)?.Bool?.Add(callback);

    /// <summary>Subscribe to an action and receive a 1D axis float value (X component).</summary>
    public void BindAction(InputAction action, Action<float> callback)
        => GetOrCreate(action, callback)?.Float?.Add(callback);

    /// <summary>Subscribe to an action and receive a 2D axis value (XY components).</summary>
    public void BindAction(InputAction action, Action<Vector2> callback)
        => GetOrCreate(action, callback)?.Vec2?.Add(callback);

    /// <summary>Subscribe to an action and receive the full raw Vector3 value.</summary>
    public void BindAction(InputAction action, Action<Vector3> callback)
        => GetOrCreate(action, callback)?.Vec3?.Add(callback);

    /// <summary>
    /// Subscribe to an action and receive the full <see cref="ContextualInputEvent"/>, including
    /// <see cref="ContextualInputEvent.Source"/> — the <see cref="Mappings.InputKey"/> that fired.
    /// Use this overload when one action is driven by multiple mappings and the callback needs
    /// to know which physical source produced the value (e.g. WASD vs. mouse delta on one Move).
    /// </summary>
    public void BindAction(InputAction action, Action<ContextualInputEvent> callback)
        => GetOrCreate(action, callback)?.Contextual?.Add(callback);

    /// <summary>Unsubscribe a boolean callback from an action.</summary>
    public void UnbindAction(InputAction action, Action<bool> callback)
        => Subs(action)?.Bool?.Remove(callback);

    /// <summary>Unsubscribe a float callback from an action.</summary>
    public void UnbindAction(InputAction action, Action<float> callback)
        => Subs(action)?.Float?.Remove(callback);

    /// <summary>Unsubscribe a Vector2 callback from an action.</summary>
    public void UnbindAction(InputAction action, Action<Vector2> callback)
        => Subs(action)?.Vec2?.Remove(callback);

    /// <summary>Unsubscribe a Vector3 callback from an action.</summary>
    public void UnbindAction(InputAction action, Action<Vector3> callback)
        => Subs(action)?.Vec3?.Remove(callback);

    /// <summary>Unsubscribe a <see cref="ContextualInputEvent"/> callback from an action.</summary>
    public void UnbindAction(InputAction action, Action<ContextualInputEvent> callback)
        => Subs(action)?.Contextual?.Remove(callback);

    /// <summary>
    /// Called by <see cref="EnhancedInputSystem"/> to deliver a processed action value to all
    /// registered subscribers. Each callback type has its own list, so this resolves the action
    /// once and then walks each list with a plain indexed loop — no per-event type switching.
    /// The <paramref name="source"/> is the <see cref="Mappings.InputKey"/> that produced this
    /// event; it is forwarded to <see cref="ContextualInputEvent"/>-typed callbacks so they can
    /// tell which mapping fired when one action is driven by several.
    /// </summary>
    public void PushAction(InputAction action, Vector3 value, InputEvent @event, Mappings.InputKey source = null)
    {
        if (action == null || string.IsNullOrEmpty(action.ActionName)) return;
        if (!_actionEvents.TryGetValue(action.ActionName, out var subs)) return;

        // Each value is built once here and shared across that type's subscribers; the
        // Invoke extension (SubscriberListExtensions.cs) is null/empty-safe, so absent
        // callback types simply no-op.
        subs.Bool.Invoke(value.X > 0.5f);
        subs.Float.Invoke(value.X);
        subs.Vec2.Invoke(new Vector2(value.X, value.Y));
        subs.Vec3.Invoke(value);
        subs.Contextual.Invoke(new ContextualInputEvent
        {
            Action = action,
            Source = source,
            RawEvent = @event,
            RawValue = value,
        });
    }

    /// <summary>Called by <see cref="EnhancedInputSystem"/> when this context is pushed onto the stack.</summary>
    internal void NotifyPushed() => EmitSignal(SignalName.Pushed);

    /// <summary>Called by <see cref="EnhancedInputSystem"/> when this context is removed from the stack.</summary>
    internal void NotifyPopped() => EmitSignal(SignalName.Popped);

    /// <summary>
    /// Called by <see cref="EnhancedInputSystem"/> whenever the active stack changes in a way
    /// that may affect whether this context is the topmost one.
    /// </summary>
    internal void NotifyPriorityChanged(bool isTopmost) => EmitSignal(SignalName.PriorityChanged, Variant.From(isTopmost));

    /// <summary>
    /// Subscribes every mapping's triggers to the system's ActiveContextChanged signal.
    /// Called by EnhancedInputSystem when this context is pushed onto the active stack,
    /// so the triggers reset their edge-state on any subsequent stack change.
    /// </summary>
    internal void BindTriggers()
    {
        foreach (var mapping in Mappings)
            mapping?.BindTriggers();
    }

    /// <summary>Counterpart to <see cref="BindTriggers"/>; called when this context is popped.</summary>
    internal void UnbindTriggers()
    {
        foreach (var mapping in Mappings)
            mapping?.UnbindTriggers();
    }

    /// <summary>
    /// Two contexts are equal if they share the same ContextName (case-insensitive).
    /// Allows checks like: EnhancedInputSystem.GetInstance().GetCurrentContext() == GameplayContext
    /// </summary>
    public static bool operator ==(InputMappingContext a, InputMappingContext b)
    {
        if (a is null && b is null) return true;
        if (a is null || b is null) return false;
        return string.Equals(a.ContextName, b.ContextName, StringComparison.OrdinalIgnoreCase);
    }

    public static bool operator !=(InputMappingContext a, InputMappingContext b) => !(a == b);

    public override bool Equals(object obj)
        => obj is InputMappingContext other && this == other;

    public override int GetHashCode()
        => ContextName?.ToLowerInvariant().GetHashCode() ?? 0;

    /// <summary>Returns the subscriber bucket for an action, or null if nothing is bound to it.</summary>
    private Subscribers Subs(InputAction action)
    {
        if (action == null || string.IsNullOrEmpty(action.ActionName)) return null;
        return _actionEvents.TryGetValue(action.ActionName, out var subs) ? subs : null;
    }

    /// <summary>
    /// Returns the subscriber bucket for an action, creating it (and the specific typed list the
    /// caller is about to add to) on demand. Returns null if the action is invalid, so the
    /// null-conditional Add in each BindAction overload simply no-ops. The list selected for
    /// lazy creation is chosen by the callback's runtime type.
    /// </summary>
    private Subscribers GetOrCreate(InputAction action, Delegate callback)
    {
        if (action == null || string.IsNullOrEmpty(action.ActionName) || callback == null) return null;

        if (!_actionEvents.TryGetValue(action.ActionName, out var subs))
        {
            subs = new Subscribers();
            _actionEvents[action.ActionName] = subs;
        }

        switch (callback)
        {
            case Action<bool>:                  subs.Bool ??= new(); break;
            case Action<float>:                 subs.Float ??= new(); break;
            case Action<Vector2>:               subs.Vec2 ??= new(); break;
            case Action<Vector3>:               subs.Vec3 ??= new(); break;
            case Action<ContextualInputEvent>:  subs.Contextual ??= new(); break;
        }

        return subs;
    }
}
