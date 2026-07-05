# InputForgeDemo

A minimal scene demonstrating InputForge's core feature: the same physical key means
different things depending on the active context, switched entirely through Inspector
resources — no code changes to the input definitions themselves.

## What it shows

- **WASD** moves the player via a `Digital` `InputKey` while `GameplayContext` is active.
- **Space** toggles to `FollowContext`, where the mouse cursor's `Pointer` position
  drives movement instead.
- While Follow is active, `EnhancedInputSystem.PreventFallbackContext` is used so WASD
  is fully ignored rather than falling through to Gameplay's mapping.
- A status `Label` shows which context is active and the current input value, updated
  live via `BindAction` callbacks.

## How to run

This demo is not auto-loaded — it's a standalone scene you can open and run directly.
Open `Scenes/DemoScene.tscn` in the editor and press F6 (Run Current Scene).

## Not part of the plugin

This folder (`addons/input_forge_demo/`) is separate from `addons/input_forge/` — it is
not required for InputForge to function and is not enabled as a plugin itself. It's
included for reference only. Feel free to delete it after exploring the example.
