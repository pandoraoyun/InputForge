using Godot;
using InputForge;

namespace InputForge.Tests;

/// <summary>
/// Test-only helpers for deterministic node/singleton teardown in the shared-engine
/// (2dog) collection. Because no frames are processed during a test run, Node.QueueFree()
/// never actually frees — leftover EnhancedInputSystem nodes stay parented to the tree
/// and keep the static singleton (_instance) populated across tests. These helpers free
/// synchronously instead, which also triggers _ExitTree so the singleton is cleared.
/// </summary>
public static class InputForgeTestExtensions
{
    /// <summary>
    /// Synchronously removes a node from the tree and frees it immediately (no deferred
    /// QueueFree). Use in a test's finally block instead of QueueFree() to keep the shared
    /// engine clean between tests. Safe to call with an already-freed/invalid instance.
    /// </summary>
    public static void DestroyImmediate(this Node node)
    {
        if (node is null || !GodotObject.IsInstanceValid(node)) return;
        node.GetParent()?.RemoveChild(node);
        node.Free();
    }

    /// <summary>
    /// Evicts whatever EnhancedInputSystem is currently registered as the singleton,
    /// freeing it synchronously so GetInstance() returns null afterwards. Use at the start
    /// of any test that asserts "no instance exists" to make it order-independent.
    /// </summary>
    public static void DropEnhancedInputSystemInstance()
    {
        EnhancedInputSystem.GetInstance().DestroyImmediate();
    }
}
