using Godot;

namespace GodotGameAIbyExample.addons.InteractiveRanges;

[Tool]
public partial class InteractiveRanges: EditorPlugin
{
    public override void _EnterTree()
    {
        RegisterCustomNode(
            "CircularRange",
            "Node2D",
            "res://addons/InteractiveRanges/CircularRange/CircularRange.cs",
            "res://addons/InteractiveRanges/CircularRange/CircularRangeIcon.svg");
        RegisterCustomNode(
            "ConeRange",
            "Node2D",
            "res://addons/InteractiveRanges/ConeRange/ConeRange.cs",
            "res://addons/InteractiveRanges/ConeRange/ConeRangeIcon.svg");
    }

    public override void _ExitTree()
    {
        RemoveCustomType("CircularRange");
        RemoveCustomType("ConeRange");
    }

    private void RegisterCustomNode(
        string name, 
        string baseNode, 
        string scriptPath,
        string texturePath)
    {
        Script script = GD.Load<Script>(scriptPath);
        Texture2D icon = GD.Load<Texture2D>(texturePath);
        AddCustomType(name, baseNode, script, icon);
    }
}