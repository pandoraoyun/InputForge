# Context Stack

The context stack is the core of InputForge's runtime flexibility. It allows the same physical key to mean different things depending on what the player is currently doing.

## How the Stack Works

`EnhancedInputSystem` maintains an ordered list of active `InputMappingContext` resources. When a Godot input event arrives, contexts are evaluated **in reverse order** — the most recently added context has the highest priority.

When a context successfully handles an event (at least one mapping matches and passes its triggers), the event is consumed via `GetViewport().SetInputAsHandled()` and no lower-priority context sees it.

Mouse motion events are **never consumed** because multiple systems may need to react to them simultaneously.

```
Stack (bottom → top):
  [0] BaseContext      ← lowest priority
  [1] VehicleContext
  [2] MenuContext      ← highest priority (last added)
```

## Pushing and Popping Contexts

```csharp
var system = EnhancedInputSystem.GetInstance();

// Push when entering a state
system.AddContext(vehicleContext);

// Pop when leaving
system.RemoveContext(vehicleContext);
```

There is no strict push/pop ordering enforced — you can remove any context at any time regardless of insertion order. This means you can layer contexts arbitrarily:

```csharp
// Gameplay + vehicle simultaneously
system.AddContext(gameplayContext);
system.AddContext(vehicleContext);

// Only vehicle input reaches the vehicle; gameplay input still works for
// actions not handled by vehicleContext (e.g. pause, map)
```

## Binding and Unbinding Callbacks

Callbacks are registered on the context, not the system. This means the same context resource can be shared across multiple objects, each with their own callbacks:

```csharp
// Player A and Player B share the same GameplayContext resource
// but bind different callbacks
playerA.GameplayContext.BindAction(jumpAction, playerA.OnJump);
playerB.GameplayContext.BindAction(jumpAction, playerB.OnJump);
```

Always unbind callbacks when the subscriber is removed from the scene to avoid dangling references:

```csharp
public override void _ExitTree()
{
    EnhancedInputSystem.GetInstance().RemoveContext(GameplayContext);
    GameplayContext.UnbindAction(JumpAction, OnJump);
    GameplayContext.UnbindAction(MoveAction, OnMove);
}
```

## Callback Overloads

`BindAction` accepts four callback signatures. Choose the one that matches what you actually need from the value:

```csharp
// Boolean — pressed or released
context.BindAction(jumpAction, (bool pressed) => { });

// Float — single axis value (X component)
context.BindAction(scrollAction, (float delta) => { });

// Vector2 — two-axis value (XY components)
context.BindAction(moveAction, (Vector2 dir) => { });

// Vector3 — full raw value, all three components
context.BindAction(gyroAction, (Vector3 raw) => { });
```
