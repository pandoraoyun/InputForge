using Godot;

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

    private Label _pressedButtonLabel;
    private Label _calledActionLabel;

    private bool _isDriving;

    public override void _Ready()
    {
        _pressedButtonLabel = GameUi.GetNode<Label>("Control/PressedButton");
        _calledActionLabel = GameUi.GetNode<Label>("Control/CalledAction");
        ManuUi.Visible = false;

        var system = EnhancedInputSystem.GetInstance();
        system.AddContext(CharacterContext);

        CharacterContext.BindAction(MoveAction, MoveActionHandler);
        CharacterContext.BindAction(SwitchDriveMode, SwitchDriveModeHandler);
        CharacterContext.BindAction(OpenMenuAction, MenuOpenActionHandler);
            
        
        // DrivingContext.BindAction(MoveAction, MoveActionHandler);
        // DrivingContext.BindAction(SwitchDriveMode, SwitchDriveModeHandler);
        // NOTE: DrivingInputMappingContext.tres has no InputMapping for OpenMenuAction
        // (only Move). Binding OpenMenuAction here would register a callback that can
        // never fire — there's nothing in the .tres to dispatch it. If Menu should be
        // reachable while driving too, add an InputMapping for OpenMenuAction (ESC) to
        // DrivingInputMappingContext.tres first, then bind it here.

        MenuContext.BindAction(CloseMenuAction, MenuCloseActionHandler);

        base._Ready();
    }

    private void MoveActionHandler(Vector2 direction)
    {
        Body.Velocity = direction * 100f;
        UpdateInGameUi("Move", direction.ToString());
    }

    private void SwitchDriveModeHandler(bool pressed)
    {
        
        // UI reflects the raw value first, regardless of whether this is the rising
        // or falling edge — release IS expected to reach here too with the default
        // TriggerOnChange trigger, so this should show both true and false.
        UpdateInGameUi("SwitchDriveMode", pressed.ToString());

        if (!pressed) return;

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
        GD.Print("Open menu: ", pressed);

        UpdateInGameUi("OpenMenu", pressed.ToString());

        if (!pressed) return;

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
        GD.Print("Close menu: ", pressed);
        // UI reflects the raw value first, same reasoning as SwitchDriveModeHandler
        // above — we want to actually SEE the release reach this handler, not just
        // the press, to confirm whether the underlying trigger is behaving correctly.
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

    /// <summary>
    /// Drives the two debug labels in the in-game UI: which action last fired, and
    /// what value/state it carried. Kept intentionally simple — this is demo-only
    /// wiring, not something InputForge itself needs.
    /// </summary>
    private void UpdateInGameUi(string actionName, string value)
    {
        if (_calledActionLabel != null) _calledActionLabel.Text = $"Action: {actionName}";
        if (_pressedButtonLabel != null) _pressedButtonLabel.Text = $"Value: {value}";
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
}
