#if TOOLS
using Godot;

namespace InputForge;

[Tool]
public partial class InputForgePlugin : EditorPlugin
{
    public override void _EnterTree()
    {
        GD.Print("InputForge plugin enabled.");
    }

    public override void _ExitTree()
    {
        GD.Print("InputForge plugin disabled.");
    }
}
#endif
