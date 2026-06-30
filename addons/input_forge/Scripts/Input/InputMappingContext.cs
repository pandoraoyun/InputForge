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

    /// <summary>Unsubscribe a boolean callback from an action.</summary>
    public void UnbindAction(InputAction action, Action<bool> callback)    => Unregister(action, callback);

    /// <summary>Unsubscribe a float callback from an action.</summary>
    public void UnbindAction(InputAction action, Action<float> callback)   => Unregister(action, callback);

    /// <summary>Unsubscribe a Vector2 callback from an action.</summary>
    public void UnbindAction(InputAction action, Action<Vector2> callback) => Unregister(action, callback);

    /// <summary>Unsubscribe a Vector3 callback from an action.</summary>
    public void UnbindAction(InputAction action, Action<Vector3> callback) => Unregister(action, callback);

    /// <summary>
    /// Called by <see cref="EnhancedInputSystem"/> to deliver a processed action value
    /// to all registered subscribers. Each callback receives the value cast to its declared type.
    /// </summary>
    public void PushAction(InputAction action, Vector3 value, InputEvent @event)
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
            }
        }
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
