using System.Collections.Generic;
using Godot;
using Godot.Collections;
using GodotGameAIbyExample.Scripts.Extensions;
using GodotGameAIbyExample.Scripts.SteeringBehaviors;

namespace GodotGameAIbyExample.Scripts.Groups;

[Tool]
public partial class FormationShapeManager : CollisionShape2D
{
    [ExportGroup("WIRING:")]
    [Export] private Node2D _formationNode;
    
    private RectangleShape2D _formationShape;
    private IFormation _formation;
    private bool _isAlreadyInitialized;
    
    public override void _EnterTree()
    {
        _formationShape = (RectangleShape2D) Shape;
        _formation = (IFormation) _formationNode;
        _formation.FormationDimensionsChanged += OnFormationDimensionsChanged;
    }

    public override void _ExitTree()
    {
        _formation.FormationDimensionsChanged -= OnFormationDimensionsChanged;
    }

    private void OnFormationDimensionsChanged(
        object sender, 
        FormationDimensionsChangedArgs e)
    {
        UpdateShape(e.MembersPositions, e.MemberRadius);
    }

    private void UpdateShape(Vector2[] membersPositions, float memberRadius)
    {
        float minimumXMemberPosition = membersPositions[0].X;
        float maximumXMemberPosition = membersPositions[0].X;
        float minimumYMemberPosition = membersPositions[0].Y;
        float maximumYMemberPosition = membersPositions[0].Y;

        
        foreach (Vector2 position in membersPositions)
        {
            if (position.X < minimumXMemberPosition)
            {
                minimumXMemberPosition = position.X;
            } 
            else if (position.X > maximumXMemberPosition)
            {
                maximumXMemberPosition = position.X;
            }
            
            if (position.Y < minimumYMemberPosition)
            {
                minimumYMemberPosition = position.Y;
            } 
            else if (position.Y > maximumYMemberPosition)
            {
                maximumYMemberPosition = position.Y;
            }
        }
        
        float positionWidth = maximumXMemberPosition - minimumXMemberPosition;
        float positionHeight = maximumYMemberPosition - minimumYMemberPosition;

        Vector2 formationDimensions = new(
            positionWidth + 2 * memberRadius, 
            positionHeight + 2 * memberRadius);
        
        _formationShape.Size = formationDimensions;
        
        GlobalPosition = new Vector2(
            _formationNode.GlobalPosition.X - formationDimensions.Y / 2.0f,  
            _formationNode.GlobalPosition.Y + formationDimensions.X / 2.0f);
        
        Rotation = _formationNode.Rotation;
    }

    public override void _Process(double delta)
    {
        if (!_isAlreadyInitialized)
        {
            if (_formation == null) return;
            UpdateShape(_formation.MemberPositions.ToArray(), _formation.MemberRadius);
            _isAlreadyInitialized = true;
        }
        // Once first initialized, any further change in the formation will be handled by
        // the OnFormationDimensionsChanged() method.
    }

    public override string[] _GetConfigurationWarnings()
    {
        List<string> warnings = new();

        IFormation formation = _formation;
        
        if (formation == null)
        {
            warnings.Add("Formation field value must implement IFormation interface.");
        }
        
        return warnings.ToArray();
    }
}