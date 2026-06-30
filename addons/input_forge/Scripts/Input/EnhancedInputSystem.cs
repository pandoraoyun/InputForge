using Godot;
using System.Collections.Generic;
using InputForge.Enum;

namespace InputForge;

/// <summary>
/// Core singleton node that processes input events and dispatches them
/// to all active <see cref="InputMappingContext"/> instances.
/// Add as an autoload in Project Settings.
/// </summary>
public partial class EnhancedInputSystem : Node
{
    private static EnhancedInputSystem _instance;
    public static EnhancedInputSystem GetInstance() => _instance;

    /// <summary>Emitted whenever the topmost (highest-priority) active context changes — including becoming null.</summary>
    [Signal] public delegate void ActiveContextChangedEventHandler(InputMappingContext newTop);

    /// <summary>Emitted right after a context is pushed onto the active stack.</summary>
    [Signal] public delegate void ContextPushedEventHandler(InputMappingContext context);

    /// <summary>Emitted right after a context is fully removed from the active stack.</summary>
    [Signal] public delegate void ContextPoppedEventHandler(InputMappingContext context);

    public override void _Ready() => _instance = this;

    public override void _ExitTree()
    {
        // Clear the singleton when this instance leaves the tree so a freed node
        // is never left dangling in _instance. Guarded in case another instance
        // has already taken over (e.g. during a reload).
        if (_instance == this) _instance = null;
    }

    private readonly List<InputMappingContext> _activeContexts = new();

    /// <summary>
    /// When true, only the highest-priority active context is evaluated for input —
    /// the loop breaks immediately after processing it, regardless of whether it
    /// handled the event. Lower-priority contexts in the stack are never reached.
    /// Toggle this on/off at runtime as needed (e.g. while a modal context like a
    /// pause menu or mouse-look mode is active). Defaults to false, which preserves
    /// the original fallback-to-lower-context behavior.
    /// </summary>
    public bool PreventFallbackContext { get; set; } = false;

    /// <summary>
    /// Controls what happens when <see cref="AddContext"/> is called with a context
    /// that is already present in the active stack. Defaults to <see cref="DuplicateContextBehavior.Replace"/>,
    /// which moves the context to the top (highest priority) instead of creating a duplicate entry.
    /// </summary>
    public DuplicateContextBehavior DuplicateContextBehavior { get; set; } = DuplicateContextBehavior.Replace;

    /// <summary>Returns the highest-priority (last added) active context, or null if the stack is empty.</summary>
    public InputMappingContext GetCurrentContext()
        => _activeContexts.Count > 0 ? _activeContexts[^1] : null;

    /// <summary>
    /// Internal hook so Resource-based types (which cannot call Node.GetViewport()
    /// themselves) can reach the live Viewport through the singleton instance.
    /// Used by InputKey's Pointer mode to query the current mouse position via
    /// Viewport.GetMousePosition(). Not part of the public API — only InputForge's
    /// own Resource types should depend on this.
    /// </summary>
    internal Viewport GetInputViewport() => GetViewport();

    /// <summary>
    /// Pushes a context onto the active stack. Last added context has highest priority.
    /// If the context is already in the stack, behavior is controlled by <see cref="DuplicateContextBehavior"/>:
    /// Ignore silently no-ops, Replace (default) moves it to the top.
    /// </summary>
    public void AddContext(InputMappingContext context)
    {
        if (context == null) return;

        // Capture the actual topmost context BEFORE any mutation. This matters even
        // in the Replace-duplicate case: if `context` itself is currently the top and
        // gets removed-then-re-added below, GetCurrentContext() would transiently
        // report null/something-else in between — capturing it here avoids that and
        // correctly reports "no change" when the context was already on top.
        var previousTop = GetCurrentContext();

        bool alreadyActive = _activeContexts.Contains(context);

        if (alreadyActive)
        {
            if (DuplicateContextBehavior == DuplicateContextBehavior.Ignore) return;

            // Replace: drop the existing entry first so re-adding brings it to the top
            // without leaving a stale duplicate lower in the stack. Unbind first so the
            // BindTriggers below doesn't double-subscribe this context's triggers.
            context.UnbindTriggers();
            _activeContexts.Remove(context);
        }

        _activeContexts.Add(context);
        // Bind BEFORE NotifyPriorityChangesAfterStackMutation so this context's triggers
        // are already subscribed when the push's own ActiveContextChanged fires — they
        // then reset to a clean baseline as part of becoming active.
        context.BindTriggers();
        context.NotifyPushed();
        EmitSignal(SignalName.ContextPushed, context);

        NotifyPriorityChangesAfterStackMutation(previousTop);
    }

    /// <summary>Removes a context from the active stack.</summary>
    public void RemoveContext(InputMappingContext context)
    {
        if (context == null) return;

        // Capture the actual topmost context BEFORE mutating the stack — the context
        // being removed is not necessarily (and usually isn't) the current top.
        var previousTop = GetCurrentContext();

        if (!_activeContexts.Remove(context)) return;

        // Unbind the leaving context's triggers; the remaining (now re-exposed) contexts
        // are still bound and will reset via the ActiveContextChanged emitted below.
        context.UnbindTriggers();
        context.NotifyPopped();
        EmitSignal(SignalName.ContextPopped, context);

        NotifyPriorityChangesAfterStackMutation(previousTop);
    }

    /// <summary>
    /// After a push/pop, compares the new topmost context against what it was before
    /// the mutation. If it changed, emits ActiveContextChanged and notifies both the
    /// old and new topmost contexts via PriorityChanged.
    /// </summary>
    private void NotifyPriorityChangesAfterStackMutation(InputMappingContext previousTop)
    {
        var newTop = GetCurrentContext();
        if (previousTop == newTop) return;

        if (previousTop != null) previousTop.NotifyPriorityChanged(isTopmost: false);
        if (newTop != null) newTop.NotifyPriorityChanged(isTopmost: true);

        EmitSignal(SignalName.ActiveContextChanged, newTop);
    }

    public override void _Input(InputEvent @event)
    {
        // Iterate contexts in reverse so the last added context has highest priority.
        for (int i = _activeContexts.Count - 1; i >= 0; i--)
        {
            var context = _activeContexts[i];
            bool contextHandledAnyInput = false;

            foreach (var mapping in context.Mappings)
            {
                if (mapping.InputSource == null || mapping.TargetAction == null) continue;
                if (!mapping.InputSource.HandleInput(@event)) continue;

                var value = mapping.InputSource.GetValue();
                value = mapping.ApplyModifiers(value);

                if (!mapping.EvaluateTriggers(value, @event)) continue;

                contextHandledAnyInput = true;
                context.PushAction(mapping.TargetAction, value, @event);
            }

            // Consume the event so lower-priority contexts don't process it.
            // Mouse motion is never consumed since multiple systems may need it.
            if (contextHandledAnyInput && @event is not InputEventMouseMotion)
            {
                GetViewport().SetInputAsHandled();
                return;
            }

            // When PreventFallbackContext is enabled, only the topmost context
            // is ever evaluated — stop after the first iteration regardless of
            // whether it matched anything.
            if (PreventFallbackContext) break;
        }
    }
}
