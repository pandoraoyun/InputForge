#if TOOLS
using Godot;

[Tool]
public partial class InputForgePlugin : EditorPlugin
{
    private const string AutoloadName = "EnhancedInputSystem";
    private const string AutoloadPath = "res://addons/input_forge/Scripts/Input/EnhancedInputSystem.cs";

    public override void _EnterTree()
    {
        if (!ProjectSettings.HasSetting($"autoload/{AutoloadName}"))
            AddAutoloadSingleton(AutoloadName, AutoloadPath);

        RegisterTypes();
    }

    public override void _ExitTree()
    {
        if (ProjectSettings.HasSetting($"autoload/{AutoloadName}"))
            RemoveAutoloadSingleton(AutoloadName);

        UnregisterTypes();
    }

    private void RegisterTypes()
    {
        var iconAction  = Load<Texture2D>("res://addons/input_forge/Icons/InputAction.svg");
        var iconKey     = Load<Texture2D>("res://addons/input_forge/Icons/InputKey.svg");
        var iconMapping = Load<Texture2D>("res://addons/input_forge/Icons/InputMapping.svg");
        var iconContext = Load<Texture2D>("res://addons/input_forge/Icons/InputMappingContext.svg");
        var iconMod     = Load<Texture2D>("res://addons/input_forge/Icons/InputModifier.svg");
        var iconTrig    = Load<Texture2D>("res://addons/input_forge/Icons/InputTrigger.svg");

        AddCustomType("InputAction",         "Resource", Load<Script>("res://addons/input_forge/Scripts/Input/InputAction.cs"),         iconAction);
        AddCustomType("InputKey",            "Resource", Load<Script>("res://addons/input_forge/Scripts/Input/Mappings/InputKey.cs"),   iconKey);
        AddCustomType("InputMapping",        "Resource", Load<Script>("res://addons/input_forge/Scripts/Input/InputMapping.cs"),        iconMapping);
        AddCustomType("InputMappingContext", "Resource", Load<Script>("res://addons/input_forge/Scripts/Input/InputMappingContext.cs"), iconContext);

        // Base modifier
        AddCustomType("InputModifier",       "Resource", Load<Script>("res://addons/input_forge/Scripts/Input/Modifiers/InputModifier.cs"), iconMod);
        AddCustomType("DeadzoneModifier",    "Resource", Load<Script>("res://addons/input_forge/Scripts/Input/Modifiers/DeadzoneModifier.cs"), iconMod);
        AddCustomType("InvertModifier",      "Resource", Load<Script>("res://addons/input_forge/Scripts/Input/Modifiers/InvertModifier.cs"),   iconMod);
        AddCustomType("NormalizeModifier",   "Resource", Load<Script>("res://addons/input_forge/Scripts/Input/Modifiers/NormalizeModifier.cs"), iconMod);
        AddCustomType("ScaleModifier",       "Resource", Load<Script>("res://addons/input_forge/Scripts/Input/Modifiers/ScaleModifier.cs"),    iconMod);
        AddCustomType("SwizzleModifier",     "Resource", Load<Script>("res://addons/input_forge/Scripts/Input/Modifiers/SwizzleModifier.cs"),  iconMod);

        // Base trigger
        AddCustomType("InputTrigger",        "Resource", Load<Script>("res://addons/input_forge/Scripts/Input/Triggers/InputTrigger.cs"),        iconTrig);
        AddCustomType("TriggerOnKeyDown",    "Resource", Load<Script>("res://addons/input_forge/Scripts/Input/Triggers/TriggerOnKeyDown.cs"),    iconTrig);
        AddCustomType("TriggerOnKeyUp",      "Resource", Load<Script>("res://addons/input_forge/Scripts/Input/Triggers/TriggerOnKeyUp.cs"),      iconTrig);
        AddCustomType("TriggerOnChange",     "Resource", Load<Script>("res://addons/input_forge/Scripts/Input/Triggers/TriggerOnChange.cs"),     iconTrig);
        AddCustomType("TriggerContinuous",   "Resource", Load<Script>("res://addons/input_forge/Scripts/Input/Triggers/TriggerContinuous.cs"),   iconTrig);
    }

    private void UnregisterTypes()
    {
        RemoveCustomType("InputAction");
        RemoveCustomType("InputKey");
        RemoveCustomType("InputMapping");
        RemoveCustomType("InputMappingContext");
        RemoveCustomType("InputModifier");
        RemoveCustomType("DeadzoneModifier");
        RemoveCustomType("InvertModifier");
        RemoveCustomType("NormalizeModifier");
        RemoveCustomType("ScaleModifier");
        RemoveCustomType("SwizzleModifier");
        RemoveCustomType("InputTrigger");
        RemoveCustomType("TriggerOnKeyDown");
        RemoveCustomType("TriggerOnKeyUp");
        RemoveCustomType("TriggerOnChange");
        RemoveCustomType("TriggerContinuous");
    }

    private static T Load<T>(string path) where T : Resource
        => ResourceLoader.Load<T>(path);
}
#endif
