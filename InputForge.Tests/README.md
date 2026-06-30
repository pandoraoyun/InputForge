# InputForge.Tests

Unit tests for InputForge using [2dog](https://github.com/outfox/2dog) and xUnit.

## Why this structure is weird

Godot C# unit testing requires the Godot runtime to be present — classes like `Resource`, `Node`, and `GodotObject` call into native Godot binaries at construction time. Without the runtime, any test that instantiates a `Resource` subclass will immediately crash with an access violation.

2dog solves this by embedding the Godot binary inside a .NET process, letting xUnit tests run against the real Godot runtime without opening the editor.

**The catch:** as of writing, 2dog embeds Godot **4.6.x**, while InputForge targets **Godot 4.7.0**. The GodotSharp method binding hashes changed between versions, causing a `NativeMethodBindNotFoundException` on `Resource..cctor()` when using the 4.7 GodotSharp assembly against the 4.6 binary.

## The workaround

`TestProject/` contains a minimal Godot 4.6 project and a stub `.csproj` (`InputForge.Stub.csproj`) that compiles the same InputForge source files using `Godot.NET.Sdk/4.6.2`. The test project references this stub instead of the main `InputForge.csproj`, so everything runs against matching 4.6 binaries.

```
InputForge.Tests/
  TestProject/
    project.godot          ← minimal Godot 4.6 project (required by 2dog)
    Dummy.tscn             ← empty main scene (required by Godot)
    InputForge.Stub.csproj ← Godot.NET.Sdk/4.6.2, points to ../../addons/input_forge/**
  InputForgeTestFixture.cs ← starts Godot headless via 2dog
  GodotHeadlessCollection.cs
  Unit/
    Modifiers/
    Triggers/
```

## TODO

Once 2dog ships Godot 4.7 support, delete `TestProject/` entirely and update `InputForge.Tests.csproj` to reference `../InputForge.csproj` directly. The test code itself requires no changes.

Track progress: https://github.com/outfox/2dog

## Coverage

The addon assembly sits at **~84% line coverage** (Coverlet, `InputForge` assembly only). Tests run against a real headless Godot runtime via 2dog, so the numbers reflect actual engine behavior rather than mocks.

**Reading the number honestly — Godot inflates raw coverage.** Every `[GlobalClass]`/`Resource`/`Node` is `partial`, so Godot's source generators emit a large amount of marshalling glue per class (`InvokeGodotClassMethod`, `Get`/`SetGodotClassPropertyValue`, serialization, `*_ScriptMethods.generated.cs`, etc.). An unfiltered coverage run counts all of that auto-generated, untestable code and drags the reported figure down into the ~30–40% range even when the hand-written logic is well covered. The ~84% figure is measured **after excluding** generated sources and the editor-only `InputForgePlugin`, via [`coverage.runsettings`](coverage.runsettings) (`ExcludeByFile`/`ExcludeByAttribute`) plus `[ExcludeFromCodeCoverage]` on the plugin. If you run a report with a bare `**/coverage.cobertura.xml` glob and no filtering, expect a much lower headline number for exactly this reason — it's an artifact of the generated code, not the test suite.

**Environment-bound paths.** `InputType.Digital`/`Analog` read live OS state through `Godot.Input.IsKeyPressed`/`GetJoyAxis`, which synthetic `InputEvent` objects don't drive in a headless run. These are covered at the routing level (correct event matched, value shape) rather than by asserting a produced magnitude.

Reproduce locally:

```bash
dotnet test --settings coverage.runsettings --collect:"XPlat Code Coverage"
reportgenerator -reports:"TestResults/*/coverage.cobertura.xml" -targetdir:"coveragereport" "-reporttypes:Html;TextSummary"
```

The coverage tooling setup, the generated-code filtering in `coverage.runsettings`, and the test suite that produced these figures were prepared with the assistance of **Claude Opus 4.8** (Anthropic).

