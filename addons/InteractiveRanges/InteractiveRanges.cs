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
    }

    public override void _ExitTree()
    {
        RemoveCustomType("CircularRange");
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

    public override bool _ForwardCanvasGuiInput(InputEvent @event)
    {
        return base._ForwardCanvasGuiInput(@event);
    }
    
}