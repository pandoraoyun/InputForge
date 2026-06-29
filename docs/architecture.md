# Architecture Overview

InputForge processes input through a layered pipeline. Understanding this pipeline makes it easy to reason about why an action fires or doesn't.

```
Godot _Input(event)
  └── EnhancedInputSystem
        └── for each active InputMappingContext (last-in, highest priority)
              └── for each InputMapping
                    ├── InputKey.HandleInput(event)   → does this event match?
                    ├── InputKey.GetValue()            → raw Vector3 value
                    ├── InputMapping.ApplyModifiers()  → transform the value
                    ├── InputMapping.EvaluateTriggers() → should it fire?
                    └── InputMappingContext.PushAction() → deliver to subscribers
```

## Core Types

### EnhancedInputSystem
A singleton `Node` added as an autoload. Receives all Godot input events and routes them through the active context stack. Contexts are evaluated in reverse order — the most recently added context has the highest priority. When a context handles an input event, the event is consumed and lower-priority contexts do not see it. Mouse motion events are never consumed.

### InputAction
A `Resource` with a single `ActionName` string. Acts as a value object — two `InputAction` instances with the same name are equal and share the same subscriber list. Assign one per logical action (e.g. `"Jump"`, `"Move"`, `"Attack"`).

### InputKey
A unified `Resource` that captures one physical input source. Configured entirely in the Inspector — no subclasses needed. Produces a `Vector3` carrier value:
- **Boolean** → `Vector3(1 or 0, 0, 0)`
- **Axis1D** → `Vector3(value, 0, 0)`
- **Axis2D** → `Vector3(x, y, 0)`

### InputMapping
Binds an `InputKey` to an `InputAction` and holds the modifier and trigger lists for that specific binding. Multiple mappings can target the same action (multi-key binding).

### InputMappingContext
A named collection of `InputMapping` entries. Push it onto the `EnhancedInputSystem` stack when it becomes relevant (entering gameplay, opening a menu, entering a vehicle) and pop it when it no longer applies.

### InputModifier
Transforms the raw `Vector3` value before trigger evaluation. Modifiers are applied in array order. See [Modifiers Reference](modifiers.md).

### InputTrigger
Decides whether the processed value should cause the action to fire this event. Triggers are evaluated with OR logic — if any trigger returns true, the action fires. See [Triggers Reference](triggers.md).

## Value Shape

All values flow as `Vector3` internally. This allows a single modifier and trigger interface to work across all input types. Subscribers receive the value cast to their declared type:

| Callback type  | Receives            |
|----------------|---------------------|
| `Action<bool>` | `value.X > 0.5f`    |
| `Action<float>`| `value.X`           |
| `Action<Vector2>`| `new Vector2(value.X, value.Y)` |
| `Action<Vector3>`| `value` (raw)     |

## Default Triggers

When no triggers are assigned to a mapping, InputForge selects a sensible default:
- **Boolean input** → `TriggerOnKeyDown` (fires on the rising edge only)
- **Axis input** → `TriggerOnChange` (fires whenever the value changes, including returning to zero)
