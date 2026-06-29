# Modifiers Reference

Modifiers transform the raw `Vector3` value produced by an `InputKey` before it reaches trigger evaluation. They are applied in array order — the output of each modifier is the input of the next.

Assign modifiers to an `InputMapping` via the `LocalModifiers` array in the Inspector.

---

## DeadzoneModifier

Returns `Vector3.Zero` when the value's length is below the threshold. Use this to eliminate analog stick drift.

| Property   | Default | Description                          |
|------------|---------|--------------------------------------|
| `Deadzone` | `0.2`   | Minimum length required to pass through |

Place this **first** in the modifier list so downstream modifiers receive clean values.

---

## InvertModifier

Negates selected axes.

| Property  | Default | Description         |
|-----------|---------|---------------------|
| `InvertX` | `true`  | Negate the X axis   |
| `InvertY` | `false` | Negate the Y axis   |
| `InvertZ` | `false` | Negate the Z axis   |

Common use: inverting a joystick Y axis, reversing scroll direction.

---

## NormalizeModifier

Scales the vector to unit length (magnitude 1). Values with squared length below `MinValueThreshold` are clamped to zero, preventing unexpected direction snapping on release.

| Property            | Default  | Description                                        |
|---------------------|----------|----------------------------------------------------|
| `MinValueThreshold` | `0.001`  | Values below this (squared) are treated as zero    |

Use on Digital Axis2D mappings to keep diagonal movement the same speed as cardinal movement.

**Place after `DeadzoneModifier`** — normalizing before deadzone filtering can amplify drift to full magnitude.

---

## ScaleModifier

Multiplies each axis by a per-axis scale factor.

| Property | Default         | Description                |
|----------|-----------------|----------------------------|
| `Scale`  | `Vector3.One`   | Per-axis multiplier        |

Use for sensitivity adjustment or weighting one axis differently from another.

---

## SwizzleModifier

Reorders the axes of the value vector.

| Property | Default | Description                 |
|----------|---------|-----------------------------|
| `Order`  | `YXZ`   | The new axis order to apply |

Available orders:

| Order | Result                        | Common use                            |
|-------|-------------------------------|---------------------------------------|
| `YXZ` | `(Y, X, Z)` — swap X and Y   | Map horizontal mouse to vertical look |
| `XZY` | `(X, Z, Y)` — swap Y and Z   |                                       |
| `ZYX` | `(Z, Y, X)` — swap X and Z   |                                       |
| `ZXY` | `(Z, X, Y)`                   |                                       |
| `YZX` | `(Y, Z, X)`                   |                                       |

---

## Writing a Custom Modifier

Extend `InputModifier` and override `Apply`:

```csharp
using Godot;
using InputForge.Modifiers;

[GlobalClass]
public partial class ClampModifier : InputModifier
{
    [Export] public float Max { get; set; } = 1.0f;

    public override Vector3 Apply(Vector3 value)
        => new(
            Mathf.Clamp(value.X, -Max, Max),
            Mathf.Clamp(value.Y, -Max, Max),
            Mathf.Clamp(value.Z, -Max, Max)
        );
}
```
