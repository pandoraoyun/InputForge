using System;
using Godot;

namespace InputForge;

/// <summary>
/// Represents a named gameplay action (e.g. "Jump", "Move").
/// Acts as a value object: two instances with the same <see cref="ActionName"/>
/// are considered equal and share the same dictionary key.
/// </summary>
[GlobalClass]
public partial class InputAction : Resource
{
    /// <summary>The unique name identifying this action. Set via the Inspector.</summary>
    [Export] public string ActionName { get; set; } = "New Action";

    // Value object equality — two instances with the same name are the same action.
    public override bool Equals(object obj)
        => obj is InputAction other &&
           string.Equals(ActionName, other.ActionName, StringComparison.OrdinalIgnoreCase);

    public override int GetHashCode()
        => ActionName?.GetHashCode(StringComparison.OrdinalIgnoreCase) ?? 0;

    /// <summary>Allows implicit use of an <see cref="InputAction"/> where a string is expected.</summary>
    public static implicit operator string(InputAction action) => action?.ActionName ?? string.Empty;
}
