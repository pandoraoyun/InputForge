using Godot;
using InputForge.Enum;

namespace InputForge.Triggers;

/// <summary>
/// Fires while the value is non-zero. Two modes:
///
/// • Default (<see cref="Pulse"/> = false): fires on every event while the value is non-zero
///   — the original continuous behavior. Use for per-event actions like accumulating charge.
///
/// • Pulse (<see cref="Pulse"/> = true): fires at most once every <see cref="PulseInterval"/>
///   seconds while the value stays non-zero, throttling a held input down to a steady cadence.
///   Use for repeat-fire weapons, held-button repeats, step-wise scrolling, etc.
///
/// The pulse cadence is measured in real time via <see cref="Time.GetTicksMsec"/>, not by
/// counting events or frames — event arrival rate isn't constant (no fixed physics step here),
/// so a wall-clock interval is the only stable measure. This still relies on events arriving
/// to be evaluated; a held key/button streams events, so the clock is checked frequently enough
/// to pulse on time. When the value returns to zero (input released) the clock resets, so the
/// next press pulses immediately rather than inheriting a stale timer.
///
/// Note: for Digital input, OS key-repeat events are filtered by
/// <see cref="InputForge.Mappings.InputKey"/>, so on a held key this trigger only re-evaluates
/// when the axis value changes. Pulse mode is most predictable with input that streams events
/// continuously (mouse motion, analog axes).
/// </summary>
[Tool]
[GlobalClass]
public sealed partial class TriggerContinuous : InputTrigger
{
    private bool _pulse;

    /// <summary>
    /// When true, throttle firing to once per <see cref="PulseInterval"/> seconds while the
    /// value stays non-zero, instead of firing on every event.
    /// </summary>
    [Export]
    public bool Pulse
    {
        get => _pulse;
        set { _pulse = value; NotifyPropertyListChanged(); }
    }

    /// <summary>
    /// Seconds between pulses while the value is held non-zero. Only used when <see cref="Pulse"/>
    /// is true. Lower = faster repeat. Visible in the Inspector only when Pulse is enabled.
    /// </summary>
    [Export] public float PulseInterval { get; set; } = 0.2f;

    // Wall-clock time (ms) of the last pulse fire. Reset to 0 means "fire the next chance".
    private ulong _lastPulseMsec;

    public override bool Evaluate(Vector3 value, InputEvent @event)
    {
        bool active = value.Length() > 0f;

        if (!active)
        {
            // Released: clear the clock so the next press pulses immediately.
            _lastPulseMsec = 0;
            return false;
        }

        if (!_pulse) return true;  // default continuous behavior

        ulong now = Time.GetTicksMsec();

        // First event of a fresh hold (clock cleared) fires right away.
        if (_lastPulseMsec == 0)
        {
            _lastPulseMsec = now;
            return true;
        }

        ulong intervalMsec = (ulong)Mathf.Max(0f, PulseInterval * 1000f);
        if (now - _lastPulseMsec >= intervalMsec)
        {
            _lastPulseMsec = now;
            return true;
        }

        return false;
    }

    public override void Reset() => _lastPulseMsec = 0;

    public override void _ValidateProperty(Godot.Collections.Dictionary property)
    {
        // PulseInterval only matters when Pulse is enabled.
        if (property["name"].AsString() == nameof(PulseInterval) && !_pulse)
            property["usage"] = (int)PropertyUsageFlags.NoEditor;
    }
}
