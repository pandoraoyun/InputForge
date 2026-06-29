using twodog;
using Godot;
using System.Reflection;
using Engine = twodog.Engine;

namespace InputForge.Tests;

public class InputForgeTestFixture : IDisposable
{
    private readonly Engine _engine;
    private readonly GodotInstance _godot;

    public SceneTree Tree => _engine.Tree;

    public InputForgeTestFixture()
    {
        var projectDir = Assembly
            .GetExecutingAssembly()
            .GetCustomAttributes<AssemblyMetadataAttribute>()
            .FirstOrDefault(a => a.Key == "GodotProjectDir")
            ?.Value ?? "./TestProject";

        _engine = new Engine(
            "InputForgeTests",
            projectDir,
            "--headless",
            "--rendering-driver", "dummy",
            "--audio-driver", "Dummy"
        );
        _godot = _engine.Start();
    }

    public void Dispose()
    {
        _godot.Dispose();
        _engine.Dispose();
    }
}
