using Godot;
using System.Collections.Generic;

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

    public override void _Ready() => _instance = this;

    private readonly List<InputMappingContext> _activeContexts = new();

    /// <summary>Pushes a context onto the active stack. Last added context has highest priority.</summary>
    public void AddContext(InputMappingContext context) => _activeContexts.Add(context);

    /// <summary>Removes a context from the active stack.</summary>
    public void RemoveContext(InputMappingContext context) => _activeContexts.Remove(context);

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
        }
    }
}
