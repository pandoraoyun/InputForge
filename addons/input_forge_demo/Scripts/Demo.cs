using Godot;
using InputForge.Enum;
using InputForge.Mappings;

namespace InputForge.InputForgeDemo.Scripts;

public partial class Demo : Node2D
{
    [Export] private InputMappingContext CharacterContext { get; set; }
    [Export] private InputMappingContext DrivingContext { get; set; }
    [Export] private InputMappingContext MenuContext { get; set; }

    [Export] private InputAction MoveAction { get; set; }
    [Export] private InputAction SwitchDriveMode { get; set; }

    [Export] private InputAction CloseMenuAction { get; set; }
    [Export] private InputAction OpenMenuAction { get; set; }

    [Export] private CharacterBody2D Body { get; set; }
    [Export] private CanvasLayer GameUi { get; set; }
    [Export] private CanvasLayer ManuUi { get; set; }

    [Export] private AnimationTree AnimationTree { get; set; }
    [Export] private AnimatedSprite2D Sprite { get; set; }

    /// <summary>How quickly the sprite rotates to face the velocity direction while driving. Higher = snappier.</summary>
    [Export] private float DriveRotationSpeed { get; set; } = 8f;

    /// <summary>How fast the FORGE indicator glows fade back to gray (units of brightness per second).</summary>
    [Export] private float ForgeFadeSpeed { get; set; } = 3f;

    private RichTextLabel _rawLabel;
    private RichTextLabel _forgeLabel;
    private Label _actionLabel;
    private Label _valueLabel;

    private bool _isDriving;
    private Vector2 _cachedVelocity;
    
    /// 
    ///  Two-row input indicator
    ///
    ///  RAW   row: polled straight from Godot.Input every frame — "is this key
    ///             physically down right now". Lit while held, gray when not.
    ///  FORGE row: driven only by InputForge action callbacks. When an action fires,
    ///             the keys it represents flash to full brightness and then fade back
    ///             to gray. So the FORGE row only ever lights up for input that the
    ///             active context actually routed through the pipeline — move on foot
    ///             and the mouse_delta cell never lights, because Character doesn't
    ///             listen to it. That contrast is the whole point of the plugin.
    ///

    private enum Cell { W, A, S, D, Space, MouseDelta }

    private static readonly (Cell Cell, string Label, Key Key)[] KeyCells =
    {
        (Cell.W, "W", Key.W),
        (Cell.A, "A", Key.A),
        (Cell.S, "S", Key.S),
        (Cell.D, "D", Key.D),
        (Cell.Space, "Space", Key.Space),
    };

    private const string MouseLabel = "mouse_delta";

    // FORGE-row brightness per cell (0 = gray, 1 = full green). Decays each frame.
    private readonly float[] _forgeGlow = new float[6];

    private static readonly Color GrayColor  = new("6b7280");
    private static readonly Color GreenColor = new("4ade80");

    public override void _Ready()
    {
        _rawLabel   = GameUi.GetNode<RichTextLabel>("Control/VSplitContainer/PressedButton");
        _forgeLabel = GameUi.GetNode<RichTextLabel>("Control/VSplitContainer/PressedButton2");
        _actionLabel = GameUi.GetNode<Label>("Control/Value");
        _valueLabel  = GameUi.GetNode<Label>("Control/CalledAction");
        ManuUi.Visible = false;

        var system = EnhancedInputSystem.GetInstance();
        system.AddContext(CharacterContext);

        CharacterContext.BindAction(MoveAction, MoveActionHandler);
        CharacterContext.BindAction(SwitchDriveMode, SwitchDriveModeHandler);
        CharacterContext.BindAction(OpenMenuAction, MenuOpenActionHandler);

        DrivingContext.BindAction(MoveAction, MoveActionHandler);
        DrivingContext.BindAction(SwitchDriveMode, SwitchDriveModeHandler);
        // NOTE: DrivingInputMappingContext.tres has no InputMapping for OpenMenuAction
        // (only Move). Binding OpenMenuAction here would register a callback that can
        // never fire — there's nothing in the .tres to dispatch it. If Menu should be
        // reachable while driving too, add an InputMapping for OpenMenuAction (ESC) to
        // DrivingInputMappingContext.tres first, then bind it here.

        MenuContext.BindAction(CloseMenuAction, MenuCloseActionHandler);

        base._Ready();

        EnhancedInputSystem.GetInstance().ActiveContextChanged += HandleActiveContextChanged;
    }

    private void MoveActionHandler(ContextualInputEvent e)
    {
        var direction = new Vector2(e.RawValue.X, e.RawValue.Y);
        Body.Velocity = direction * 100f;
        UpdateInGameUi("Move", direction.ToString());

        // FORGE row: light up exactly the cells for the source that actually fired.
        // One Move action is driven by TWO mappings in DrivingContext (Digital WASD +
        // Delta mouse). The callback can now tell them apart via e.Source.InputType —
        // no guessing from raw input state, no context sniffing. This is the whole
        // reason ContextualInputEvent carries Source.
        if (e.Source == null) return;

        if (e.Source.InputType == InputType.Delta)
        {
            FlashForge(Cell.MouseDelta);
        }
        else
        {
            // Digital/Analog source — derive the WASD cells from the direction vector.
            if (direction.X > 0f) FlashForge(Cell.D);
            if (direction.X < 0f) FlashForge(Cell.A);
            if (direction.Y > 0f) FlashForge(Cell.S);
            if (direction.Y < 0f) FlashForge(Cell.W);
        }
    }

    private void SwitchDriveModeHandler(bool pressed)
    {
        // UI reflects the raw value first, regardless of whether this is the rising
        // or falling edge — release IS expected to reach here too with the default
        // TriggerOnChange trigger, so this should show both true and false.
        UpdateInGameUi("SwitchDriveMode", pressed.ToString());

        if (!pressed) return;

        FlashForge(Cell.Space);

        var system = EnhancedInputSystem.GetInstance();

        AnimationTree.Active = !AnimationTree.Active;
        _isDriving = !AnimationTree.Active;

        if (AnimationTree.Active)
        {
            // Sprite name matches the SpriteFrames animation as authored in the scene
            // (kept as "Iddle" to match the existing SpriteFrames resource — not a
            // typo introduced here, just preserved to avoid a runtime lookup mismatch).
            Sprite.SetAnimation("Iddle");
            system.AddContext(CharacterContext);
            system.RemoveContext(DrivingContext);
            // Reset rotation back to upright when returning to on-foot mode.
            Sprite.Rotation = 0f;
            return;
        }

        system.AddContext(DrivingContext);
        Sprite.SetAnimation("Drive");
    }

    private void MenuOpenActionHandler(bool pressed)
    {
        UpdateInGameUi("OpenMenu", pressed.ToString());

        if (!pressed) return;

        FlashForge(Cell.Space);

        var system = EnhancedInputSystem.GetInstance();

        // Menu is pushed ON TOP of whichever gameplay context (Character or Driving)
        // is already active — that context is deliberately left in the stack rather
        // than removed, so closing Menu just has to pop Menu and the original context
        // is immediately exposed again underneath, with no re-push needed.
        system.AddContext(MenuContext);
        // While Menu is open, nothing should fall through to Character/Driving
        // beneath it — only Close (Space) should do anything.
        system.PreventFallbackContext = true;

        GameUi.Visible = false;
        ManuUi.Visible = true;
    }

    private void MenuCloseActionHandler(bool pressed)
    {
        UpdateInGameUi("CloseMenu", pressed.ToString());

        if (!pressed) return;

        var system = EnhancedInputSystem.GetInstance();

        // Just pop Menu — the gameplay context underneath (Character or Driving) was
        // never removed, so it's already back to being the top of the stack. No
        // re-push needed.
        system.RemoveContext(MenuContext);
        system.PreventFallbackContext = false;

        ManuUi.Visible = false;
        GameUi.Visible = true;
    }

    /// <summary>Sets a FORGE indicator cell to full brightness; it fades back in _Process.</summary>
    private void FlashForge(Cell cell) => _forgeGlow[(int)cell] = 1f;

    /// <summary>
    /// Drives the two debug labels in the in-game UI: which action last fired, and
    /// what value/state it carried. Kept intentionally simple — this is demo-only
    /// wiring, not something InputForge itself needs.
    /// </summary>
    private void UpdateInGameUi(string actionName, string value)
    {
        if (_actionLabel != null) _actionLabel.Text = $"Action: {actionName}";
        if (_valueLabel  != null) _valueLabel.Text  = $"Value: {value}";
    }

    public override void _PhysicsProcess(double delta)
    {
        base._PhysicsProcess(delta);

        Body.MoveAndSlide();

        // Only while driving: tween the sprite's facing toward the current velocity
        // direction instead of snapping, so turning reads as a vehicle steering rather
        // than an instant flip. Skipped entirely when standing still (no velocity to
        // derive a direction from) and skipped on foot (character sprite stays upright).
        if (_isDriving && Body.Velocity.LengthSquared() > 1f)
        {
            float targetAngle = Body.Velocity.Angle();
            Sprite.Rotation = Mathf.LerpAngle(Sprite.Rotation, targetAngle, (float)delta * DriveRotationSpeed);
        }
    }

    public override void _Process(double delta)
    {
        RenderRawRow();
        RenderForgeRow((float)delta);
    }

    /// <summary>
    /// RAW row — polled directly from Godot.Input. A cell is fully lit while its key is
    /// physically held. This bypasses InputForge on purpose: "is the key down right now"
    /// is an instantaneous hardware question, not an action the pipeline dispatched.
    /// </summary>
    private void RenderRawRow()
    {
        if (_rawLabel == null) return;

        var sb = new System.Text.StringBuilder("RAW   ");
        foreach (var (_, label, key) in KeyCells)
            sb.Append(Colored(label, Input.IsKeyPressed(key) ? 1f : 0f)).Append("  ");

        bool mouseMoving = Input.GetLastMouseVelocity().LengthSquared() > 1f;
        sb.Append(Colored(MouseLabel, mouseMoving ? 1f : 0f));

        _rawLabel.Text = sb.ToString();
    }

    /// <summary>
    /// FORGE row — driven only by InputForge action callbacks. Each cell flashes to full
    /// brightness when an action lights it (see FlashForge) and fades back to gray here.
    /// Cells only ever light for input the active context actually routed, so e.g.
    /// mouse_delta stays dark on foot but pulses while driving.
    /// </summary>
    private void RenderForgeRow(float delta)
    {
        if (_forgeLabel == null) return;

        var sb = new System.Text.StringBuilder("FORGE ");
        for (int i = 0; i < KeyCells.Length; i++)
        {
            _forgeGlow[i] = Mathf.MoveToward(_forgeGlow[i], 0f, ForgeFadeSpeed * delta);
            sb.Append(Colored(KeyCells[i].Label, _forgeGlow[i])).Append("  ");
        }

        int mouseIdx = (int)Cell.MouseDelta;
        _forgeGlow[mouseIdx] = Mathf.MoveToward(_forgeGlow[mouseIdx], 0f, ForgeFadeSpeed * delta);
        sb.Append(Colored(MouseLabel, _forgeGlow[mouseIdx]));

        _forgeLabel.Text = sb.ToString();
    }

    /// <summary>Wraps text in a BBCode color lerped from gray (t=0) to green (t=1).</summary>
    private static string Colored(string text, float t)
    {
        Color c = GrayColor.Lerp(GreenColor, Mathf.Clamp(t, 0f, 1f));
        return $"[color=#{c.ToHtml(false)}]{text}[/color]";
    }

    private void HandleActiveContextChanged(InputMappingContext ctx)
    {
        bool enteringMenu = ctx == MenuContext;
        bool resumingDriving = ctx == DrivingContext;

        if (enteringMenu)       _cachedVelocity = Body.Velocity; // park
        Body.Velocity = resumingDriving ? _cachedVelocity        // resume
            : Vector2.Zero;          // freeze (menu) or reset (on-foot)
    }
}
