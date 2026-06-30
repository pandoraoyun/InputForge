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

    // Case-insensitive dictionary keyed by action name.
    private readonly Dictionary<string, List<Delegate>> _actionEvents =
        new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Subscribe to an action and receive a boolean pressed/released value.</summary>
    public void BindAction(InputAction action, Action<bool> callback)    => Register(action, callback);

    /// <summary>Subscribe to an action and receive a 1D axis float value (X component).</summary>
    public void BindAction(InputAction action, Action<float> callback)   => Register(action, callback);

    /// <summary>Subscribe to an action and receive a 2D axis value (XY components).</summary>
    public void BindAction(InputAction action, Action<Vector2> callback) => Register(action, callback);

    /// <summary>Subscribe to an action and receive the full raw Vector3 value.</summary>
    public void BindAction(InputAction action, Action<Vector3> callback) => Register(action, callback);

    /// <summary>
    /// Subscribe to an action and receive the full <see cref="ContextualInputEvent"/>, including
    /// <see cref="ContextualInputEvent.Source"/> — the <see cref="Mappings.InputKey"/> that fired.
    /// Use this overload when one action is driven by multiple mappings and the callback needs
    /// to know which physical source produced the value (e.g. WASD vs. mouse delta on one Move).
    /// </summary>
    public void BindAction(InputAction action, Action<ContextualInputEvent> callback) => Register(action, callback);

    /// <summary>Unsubscribe a boolean callback from an action.</summary>
    public void UnbindAction(InputAction action, Action<bool> callback)    => Unregister(action, callback);

    /// <summary>Unsubscribe a float callback from an action.</summary>
    public void UnbindAction(InputAction action, Action<float> callback)   => Unregister(action, callback);

    /// <summary>Unsubscribe a Vector2 callback from an action.</summary>
    public void UnbindAction(InputAction action, Action<Vector2> callback) => Unregister(action, callback);

    /// <summary>Unsubscribe a Vector3 callback from an action.</summary>
    public void UnbindAction(InputAction action, Action<Vector3> callback) => Unregister(action, callback);

    /// <summary>Unsubscribe a <see cref="ContextualInputEvent"/> callback from an action.</summary>
    public void UnbindAction(InputAction action, Action<ContextualInputEvent> callback) => Unregister(action, callback);

    /// <summary>
    /// Called by <see cref="EnhancedInputSystem"/> to deliver a processed action value
    /// to all registered subscribers. Each callback receives the value cast to its declared type.
    /// The <paramref name="source"/> is the <see cref="Mappings.InputKey"/> that produced this
    /// event; it is forwarded to <see cref="ContextualInputEvent"/>-typed callbacks so they can
    /// tell which mapping fired when one action is driven by several.
    /// </summary>
    public void PushAction(InputAction action, Vector3 value, InputEvent @event, Mappings.InputKey source = null)
    {
        if (action == null || string.IsNullOrEmpty(action.ActionName)) return;
        if (!_actionEvents.TryGetValue(action.ActionName, out var callbacks)) return;

        foreach (var cb in callbacks)
        {
            switch (cb)
            {
                case Action<bool>    boolCb:  boolCb(value.X > 0.5f); break;
                case Action<float>   floatCb: floatCb(value.X); break;
                case Action<Vector2> vec2Cb:  vec2Cb(new Vector2(value.X, value.Y)); break;
                case Action<Vector3> vec3Cb:  vec3Cb(value); break;
                case Action<ContextualInputEvent> ctxCb:
                    ctxCb(new ContextualInputEvent
                    {
                        Action = action,
                        Source = source,
                        RawEvent = @event,
                        RawValue = value,
                    });
                    break;
            }
        }
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

    private void Register(InputAction action, Delegate callback)
    {
        if (action == null || string.IsNullOrEmpty(action.ActionName) || callback == null) return;

        if (!_actionEvents.TryGetValue(action.ActionName, out var list))
        {
            list = new List<Delegate>();
            _actionEvents[action.ActionName] = list;
        }
        list.Add(callback);
    }

    private void Unregister(InputAction action, Delegate callback)
    {
        if (action == null || string.IsNullOrEmpty(action.ActionName) || callback == null) return;
        if (_actionEvents.TryGetValue(action.ActionName, out var list))
            list.Remove(callback);
    }
}
