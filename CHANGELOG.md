# Changelog

All notable changes to InputForge will be documented in this file.

## [Unreleased]

### Fixed
- `DeadzoneModifier` now correctly zeroes values exactly equal to the deadzone threshold (changed `<` to `<=`). Caught by unit tests.
- Mouse `Delta` input no longer continues reporting stale values once motion stops — value is now reset every frame via `EnhancedInputSystem._Process`.

### Added
- `InputMappingContext` now supports equality comparison by `ContextName` (`==`, `!=`, `Equals`, `GetHashCode`), enabling checks like `EnhancedInputSystem.GetInstance().GetCurrentContext() == GameplayContext`.
- `EnhancedInputSystem.GetCurrentContext()` returns the highest-priority active context, or `null` if none.
- `EnhancedInputSystem.ContextChanged` signal, emitted whenever a context is pushed or popped.
- `InputMappingContext.BlocksLowerContexts` flag. When `true`, the context fully consumes input while active — unmatched events are not forwarded to lower-priority contexts. Defaults to `false` (preserves prior fallback behavior).

### Notes
- `.gitattributes` added per Asset Library submission guidelines so installs only pull the `addons/` folder instead of the full repository (thanks @Gramps).
