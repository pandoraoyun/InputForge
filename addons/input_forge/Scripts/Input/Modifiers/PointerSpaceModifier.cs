using Godot;
using InputForge.Enum;

namespace InputForge.Modifiers;

/// <summary>
/// Reinterprets a Pointer InputKey's raw viewport-local pixel position into a different
/// coordinate space. Intended specifically for <see cref="InputType.Pointer"/> mappings —
/// applying it to other input types has no meaningful effect since they don't produce
/// screen-space coordinates.
///
/// Multi-monitor note: a Pointer InputKey's raw value is always relative to the game's
/// own viewport (top-left = (0,0) of the window), which is correct for in-game UI but
/// not for global desktop-aware logic. Use <see cref="PointerSpace.ScreenSpace"/> to get
/// true desktop coordinates spanning all monitors.
/// </summary>
[GlobalClass]
public sealed partial class PointerSpaceModifier : InputModifier
{
    private PointerSpace _space = PointerSpace.RelativeToScreen;

    /// <summary>Selects which coordinate space the pointer position is reinterpreted into.</summary>
    [Export]
    public PointerSpace Space
    {
        get => _space;
        set { _space = value; NotifyPropertyListChanged(); }
    }

    /// <summary>Target rect for <see cref="PointerSpace.RelativeToRect"/>, in viewport-local pixel coordinates.</summary>
    [Export] public Rect2 TargetRect { get; set; } = new Rect2(0, 0, 1920, 1080);

    public override void _ValidateProperty(Godot.Collections.Dictionary property)
    {
        string name = property["name"].AsString();
        if (name == nameof(TargetRect) && _space != PointerSpace.RelativeToRect)
            property["usage"] = (int)PropertyUsageFlags.NoEditor;
    }

    public override Vector3 Apply(Vector3 value)
    {
        // value.X/Y are viewport-local pixel coordinates, as produced by HandlePointer.
        var viewportLocal = new Vector2(value.X, value.Y);

        return _space switch
        {
            PointerSpace.ScreenSpace      => ToScreenSpace(viewportLocal),
            PointerSpace.RelativeToScreen => ToRelativeToScreen(viewportLocal),
            PointerSpace.RelativeToRect   => ToRelativeToRect(viewportLocal),
            _ => value
        };
    }

    private Vector3 ToScreenSpace(Vector2 viewportLocal)
    {
        var window = (Engine.GetMainLoop() as SceneTree)?.Root;
        var windowPosition = window != null ? window.Position : Vector2I.Zero;

        var screenPosition = viewportLocal + new Vector2(windowPosition.X, windowPosition.Y);
        return new Vector3(screenPosition.X, screenPosition.Y, 0f);
    }

    private Vector3 ToRelativeToScreen(Vector2 viewportLocal)
    {
        var screenIndex = DisplayServer.WindowGetCurrentScreen();
        var screenSize = DisplayServer.ScreenGetSize(screenIndex);

        // Convert viewport-local to absolute screen coordinates first (same as ScreenSpace),
        // then normalize against that screen's resolution.
        var absolute = ToScreenSpace(viewportLocal);
        float nx = screenSize.X > 0 ? absolute.X / screenSize.X : 0f;
        float ny = screenSize.Y > 0 ? absolute.Y / screenSize.Y : 0f;

        return new Vector3(nx, ny, 0f);
    }

    private Vector3 ToRelativeToRect(Vector2 viewportLocal)
    {
        float nx = TargetRect.Size.X != 0 ? (viewportLocal.X - TargetRect.Position.X) / TargetRect.Size.X : 0f;
        float ny = TargetRect.Size.Y != 0 ? (viewportLocal.Y - TargetRect.Position.Y) / TargetRect.Size.Y : 0f;

        return new Vector3(nx, ny, 0f);
    }
}
