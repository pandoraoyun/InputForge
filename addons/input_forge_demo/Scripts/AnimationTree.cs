using Godot;

namespace InputForge.InputForgeDemo.Scripts;

public partial class AnimationTree : Godot.AnimationTree
{
    [Export] private CharacterBody2D Body2D { get; set; }

    public override void _Ready()
    {
        GD.Print(_GetPropertyList());
        base._Ready();
    }

    public override void _Process(double delta)
    {
        Set("parameters/BlendSpace1D/blend_position", Body2D.Velocity.X);
        base._Process(delta);
    }
}