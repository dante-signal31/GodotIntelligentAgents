using System.Collections.Generic;
using Godot;

namespace GodotGameAIbyExample.Scripts.Sensors;

[Tool]
public partial class CircleSensor: VolumetricSensor
{
    [ExportCategory("CIRCLE SENSOR CONFIGURATION:")]
    private float _radius;

    [Export] public float Radius
    {
        get => _radius;
        set
        {
            _radius = value;
            if (_circleShape == null) return;
            _circleShape.Radius = value;
        }
    }
    
    private CircleShape2D _circleShape;

    public override void _EnterTree()
    {
        base._EnterTree();
        if (_circleShape != null) return;
        _circleShape = (CircleShape2D) CollisionShape.Shape;
    }
    
    public override string[] _GetConfigurationWarnings()
    {
        List<string> warnings = 
            new List<string>(base._GetConfigurationWarnings());
        
        CircleShape2D circleShape = CollisionShape.Shape as CircleShape2D;
        
        if (circleShape== null)
        {
            warnings.Add("This node needs a child Area2D node with a circle shape to work. ");
        }

        return warnings.ToArray();
    }
}