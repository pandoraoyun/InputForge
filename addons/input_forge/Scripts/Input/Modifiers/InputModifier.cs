using Godot;

namespace InputForge.Modifiers;

/// <summary>
/// Base class for all input modifiers.
/// Modifiers transform the raw <see cref="Godot.Vector3"/> value produced by an
/// <see cref="InputForge.Mappings.InputKey"/> before it reaches trigger evaluation.
/// Apply modifiers in order via <see cref="InputForge.InputMapping.LocalModifiers"/>.
/// </summary>
[GlobalClass]
public abstract partial class InputModifier : Resource
{
    /// <summary>Transforms the input value and returns the modified result.</summary>
    public abstract Vector3 Apply(Vector3 value);
}
