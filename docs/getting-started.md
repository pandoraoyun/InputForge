# Getting Started

This guide walks through setting up InputForge from scratch using only the Inspector — no code required for resource configuration.

---

## Step 1 — Create an InputAction

Right-click in the FileSystem panel → **New Resource** → search for `InputAction` → **Create**.

![InputAction resource selection](images/01_create_input_action.png)

Set `Action Name` to a descriptive string such as `MoveAction`. Save as `MoveAction.tres`.

![InputAction inspector](images/02_input_action_inspector.png)

---

## Step 2 — Create an InputMappingContext

Right-click in the FileSystem panel → **New Resource** → search for `InputMappingContext` → **Create**.

![InputMappingContext resource selection](images/03_create_input_mapping_context.png)

Set `Context Name` to something descriptive such as `GamePlayContext`. Save as `GameplayMappingContext.tres`.

![InputMappingContext inspector](images/04_input_mapping_context_inspector.png)

---

## Step 3 — Add a Mapping

Click **Mappings** → set **Size** to `1`. A new `InputMapping` entry appears.

- Set **Target Action** to your `MoveAction.tres`
- Set **Input Source** to a new `InputKey` resource
- Set `Input Type` → `Digital`, `Axis Dimension` → `Axis2D`
- Assign `D / A / S / W` to the positive/negative keys

![InputMapping with WASD InputKey](images/05_input_mapping_wasd.png)

---

## Step 4 — Assign to your Node

Select your player node in the scene tree. In the Inspector, assign:

- **Gameplay Context** → `GameplayMappingContext.tres`
- **Move Action** → `MoveAction.tres`
- **Key Display Label** → your Label node

![Player Inspector with resources assigned](images/06_player_inspector.png)

---

## Step 5 — Subscribe in code

```csharp
public override void _Ready()
{
    EnhancedInputSystem.GetInstance().AddContext(GameplayContext);
    GameplayContext.BindAction(MoveAction, OnMove);
}

private void OnMove(Vector2 value)
{
    _moveInput = value;
}
```

---

## Swapping input without changing code

The key advantage of InputForge is that the `InputKey` resource is fully decoupled from your code. To switch from WASD to mouse delta, simply change `Input Type` from `Digital` to `Delta` in the Inspector — your `OnMove` callback receives the new value with zero code changes.
