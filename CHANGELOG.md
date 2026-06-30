# Changelog

All notable changes to InputForge will be documented in this file.

## [Unreleased]

## [0.2.0]

### Added
- `InputType.Pointer` — an `InputKey` can now report the **absolute mouse position**, read live from the active `Viewport` via `EnhancedInputSystem`'s internal viewport hook (not derived from motion deltas). Falls back to the triggering event's own `Position` when no `EnhancedInputSystem` instance is available. Pointer answers "where is the cursor" as opposed to `Delta`'s "how much did it move", so it is its own input type rather than a flag on `Delta`.
- `DeviceType` on Boolean `InputKey`s (`Keyboard` / `JoyButton` / `MouseButton`) — the Boolean binding model now explicitly selects its physical device, cleanly covering keyboard keys, gamepad buttons, and mouse buttons through one type.
- Inspector visibility (`InputKey._ValidateProperty`) shows only the fields relevant to the selected `InputType` / `DeviceType` / `AxisDimension` (e.g. Pointer hides delta-only `Sensitivity`/`IsYAxis`; Boolean hides axis fields).
- Test suite expanded to ~84% line coverage of the addon assembly: modifier suites (`Invert`, `Scale`, `Swizzle`, `Normalize`), trigger suites (`TriggerOnKeyUp`, `TriggerContinuous`), full `InputKey` coverage (`HandleInput` routing for every type, `Equals`/`GetHashCode`/`==` binding identity, `_ValidateProperty` visibility matrix), plus `InputAction` and `ContextualInputEvent`. `InputForgeTestExtensions` adds deterministic singleton teardown for the shared 2dog engine. Coverage is measured via `coverage.runsettings` that excludes Godot's generated marshalling code; see `InputForge.Tests/README.md#coverage`.
- CI: `.github/workflows/tests.yml` runs the test suite with coverage on every pull request to `main` (headless Godot via 2dog, no separate Godot install). Smoke/benchmark tests are skipped in CI.

### Fixed
- `InputMappingContext.PriorityChanged(bool)` could not be emitted — the `bool` argument was marshalled as `Nullable<bool>`, throwing `InvalidOperationException: type not supported for conversion to/from Variant`. The value is now wrapped with `Variant.From(...)` so it marshals as a plain `bool`. Caught by unit tests.
- `EnhancedInputSystem._instance` was assigned in `_Ready` but never cleared, leaving a freed node referenced after teardown. Added `_ExitTree` cleanup (guarded against nulling a newer instance that already took over, e.g. during a reload).

## [0.1.1]

### Added
- `InputMappingContext` now supports equality comparison by `ContextName` (`==`, `!=`, `Equals`, `GetHashCode`), enabling checks like `EnhancedInputSystem.GetInstance().GetCurrentContext() == GameplayContext` without holding the same resource instance.
- `EnhancedInputSystem.GetCurrentContext()` returns the highest-priority active context, or `null` if none.
- Context stack signals on `EnhancedInputSystem`: `ActiveContextChanged`, `ContextPushed`, `ContextPopped`.
- Context-level signals on `InputMappingContext`: `Pushed`, `Popped`, `PriorityChanged(bool isTopmost)` — emitted internally by `EnhancedInputSystem` whenever the context is pushed, popped, or its topmost status changes.
- `EnhancedInputSystem.PreventFallbackContext` (bool, default `false`). When `true`, only the topmost active context is evaluated for input — the stack loop stops after it regardless of whether it matched anything, instead of falling through to lower-priority contexts on an unmatched event.
- `EnhancedInputSystem.DuplicateContextBehavior` enum (`Ignore` / `Replace`, default `Replace`), controlling what happens when `AddContext` is called with a context already on the stack. `Replace` moves the context to the top instead of leaving a stale duplicate lower in the stack.
- `InputForge.Tests/SMOKE_BENCH.md` — recorded throughput/stability benchmark results, including a side-by-side comparison of InputForge's single-dispatch-point architecture vs. N classes each overriding `_Input()` independently (InputForge ~1.5x cheaper at 10 handlers, ~6.8x cheaper at 500 handlers).
- New test suites: `ContextStackSignalsTests.cs` (16 tests covering all stack/context signals and `DuplicateContextBehavior`), expanded `InputSystemTests.cs` (push/pop, fallthrough, `PreventFallbackContext`), and `InputThroughputSmokeTests.cs` (throughput/stability characterization, isolated from the default test run via `[Trait("Category", "Smoke")]` — run explicitly with `dotnet test --filter "Category=Smoke"`).

### Fixed
- `DeadzoneModifier` now correctly zeroes values exactly equal to the deadzone threshold (changed `<` to `<=`). Caught by unit tests.
- `EnhancedInputSystem.RemoveContext` and `AddContext` were capturing the wrong "previous top" context before a stack mutation, which could cause `ActiveContextChanged` to fire incorrectly (or fail to fire) when popping a non-top context, or when re-pushing an already-topmost context under `DuplicateContextBehavior.Replace`. Both now capture the true topmost context before any mutation.

### Notes
- `.gitattributes` `export-ignore` scoped specifically to `addons/input_forge` (previously matched all of `/addons`), so other locally-installed addons/dependencies aren't bundled into Asset Library exports. Thanks to [@Gramps](https://github.com/Gramps) for flagging the original issue.
