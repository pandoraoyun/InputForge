# Changelog

All notable changes to InputForge will be documented in this file.

## [Unreleased]

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
