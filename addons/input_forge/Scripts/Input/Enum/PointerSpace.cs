namespace InputForge.Enum;

/// <summary>
/// Defines how <see cref="InputForge.Modifiers.PointerSpaceModifier"/> reinterprets a
/// Pointer InputKey's value (which arrives as raw viewport-local pixel coordinates).
/// </summary>
public enum PointerSpace
{
    /// <summary>
    /// Global desktop coordinates across the entire virtual screen space — relevant for
    /// multi-monitor setups where the game window doesn't cover the whole desktop, or
    /// where you need a position consistent across monitors of different sizes/positions.
    /// Produces raw pixel coordinates, not normalized.
    /// </summary>
    ScreenSpace,

    /// <summary>
    /// Normalized 0–1 position relative to the primary screen's resolution. Useful for
    /// "where on the monitor" logic independent of window size, e.g. edge-of-screen
    /// detection that should behave the same on a 1080p and a 4K display.
    /// </summary>
    RelativeToScreen,

    /// <summary>
    /// Normalized 0–1 position relative to an arbitrary <see cref="Godot.Rect2"/> (set via
    /// <see cref="InputForge.Modifiers.PointerSpaceModifier.TargetRect"/>) — e.g. a specific
    /// UI panel or viewport region. (0,0) is the rect's top-left, (1,1) is its bottom-right.
    /// Values outside the rect are not clamped, so they can go negative or beyond 1.
    /// </summary>
    RelativeToRect
}
