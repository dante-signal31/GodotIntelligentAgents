using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Godot;
using Godot.Collections;
using GodotGameAIbyExample.Scripts.Extensions;
using GodotGameAIbyExample.Scripts.SteeringBehaviors;
using Vector2 = Godot.Vector2;

namespace GodotGameAIbyExample.Scripts.Groups;

/// <summary>
/// <p>In many situations, you will want to distribute a fixed number of agents evenly
/// across a given rectangular area. This is seen in games like Total War when you reshape
/// a rectangular formation dragging the mouse. In other situations, you will want your
/// formation shape to change, keeping its proportions, when its number of agents is
/// modified. Both cases are scalable formations.</p>
/// <p>This class covers both cases of scalable formations.</p>
/// <remarks>For simplicity, this class only covers rectangular formations.</remarks>
/// </summary>
[Tool]
public partial class ScalableFormation: Node2D, IGizmos, IFormation
{
    /// <summary>
    /// Emitted when the formation dimensions are updated when quantity or density of
    /// formation members are changed.
    /// </summary>
    public event EventHandler<FormationDimensionsChangedArgs> FormationDimensionsChanged;
    
    /// <summary>
    /// <p>Defines how the formation is distributed.</p>
    /// <ul><b>QuantityAndDimensionsDefined</b>: Fixed number of agents that are
    /// distributed evenly across a given rectangular area.</ul>
    /// <ul><b>DensityAndQuantityDefined</b>: A fixed number of agents is distributed
    /// across an area, with a given density, and keeping formation former proportions.
    /// </ul>
    /// <ul><b>DensityAndDimensionsDefined</b>: With a given density, the formation is
    /// distributed across a given rectangular area.</ul> 
    /// </summary>
    private enum DistributionType
    {
        QuantityAndDimensionsDefined,
        DensityAndQuantityDefined,
        DensityAndDimensionsDefined
    }

    [ExportCategory("CONFIGURATION:")] 
    [Export] private PackedScene _memberScene;
    
    private DistributionType _distributionType;

    /// <summary>
    /// Specifies the distribution configuration type for the scalable formation.
    /// </summary>
    [Export] private DistributionType Distribution
    {
        get => _distributionType;
        set
        {
            _distributionType = value;
            if (_distributionType == DistributionType.DensityAndDimensionsDefined)
            {
                _formationFormerDimensions = FormationDimensions;
            }
            UpdateFormation();
        }
    }

    private float _memberRadius;
    /// <summary>
    /// Formation member radius.
    /// </summary>
    [Export] public float MemberRadius
    {
        get => _memberRadius;
        private set
        {
            _memberRadius = value;
            UpdateFormation();
        }
    }
    
    /// <summary>
    /// Represents the minimum allowable distance between agents in the formation
    /// to ensure proper spacing and avoid overlap.
    /// </summary>
    [Export] private Vector2 MinimumDistanceBetweenAgents { get; set; }
    
    private Vector2 _formationDimensions;
    /// <summary>
    /// <p>This field means different depending on the distribution type.</p>
    /// <p>For <b>QuantityDefined</b> and <b>DensityDefined</b> it is the given area to
    /// cover with this formation.</p>
    /// <p>For <b>DensityAndQuantityDefined</b> it is the formation proportions to
    /// keep.</p> 
    /// </summary>
    [Export] private Vector2 FormationDimensions
    {
        get => _formationDimensions;
        set
        {
            _formationDimensions = value;
            
            // This void is needed to avoid infinite calls when the dimensions are
            // corrected from CalculateMembersPositions.
            if (_correctingDimensions)
            {
                _correctingDimensions = false;
                return;
            }
            
            UpdateFormation();
        }
    }
    
    private Vector2 _density;

    /// <summary>
    /// <p>Agent density in the formation. Actually, it is the current separation between
    /// agents, horizontally and vertically.</p>
    /// <p> Only used for <b>DensityAndQuantityDefined</b> and
    /// <b>DensityAndDimensionsDefined</b> distribution types. </p>
    /// </summary>
    [Export]
    private Vector2 Density
    {
        get => _density;
        set
        {
            _density = value;
            
            // This void is needed to avoid infinite calls when the density is corrected
            // from CalculateMembersPositions.
            if (_correctingDensity)
            {
                _correctingDensity = false;
                return;
            }
            
            UpdateFormation();
        }
    }
    
    private int _quantity;
    /// <summary>
    /// Total number of agents in the formation.
    /// <p> Only used for <b>QuantityAndDimensionsDefined</b> and
    /// <b>DensityAndQuantityDefined</b> distribution types. </p>
    /// </summary>
    [Export] private int Quantity
    {
        get => _quantity;
        set
        {
            _quantity = value;
            
            // This void is needed to avoid infinite calls when the quantity is corrected
            // from CalculateMembersPositions.
            if (_correctingQuantity)
            {
                _correctingQuantity = false;
                return;
            }
            
            UpdateFormation();
        }
    }


    [ExportCategory("DEBUG:")]
    [Export] public bool ShowGizmos { get; set; }
    [Export] public Color GizmosColor { get; set; }
    
    public List<Node2D> Members { get; private set; } = new();
    public List<Vector2> MemberPositions { get; private set; } = new();
    
    private bool _correctingQuantity;
    private bool _correctingDimensions;
    private bool _correctingDensity;
    private int _originGizmoRadius = 10;
    private Vector2 _gizmoTextOffset = new(10, 10);
    private Vector2 _formationFormerDimensions;
    private MovingAgent _parentAgent;
    private UsherHingeAgent _rightHinge;
    private UsherHingeAgent _leftHinge;

    public override void _EnterTree()
    {
        _parentAgent = this.FindAncestor<MovingAgent>();
    }

    public override void _Ready()
    {
        GetHingeReferences();
        AlignWithAgentForward();
        UpdateFormation();
        if (Engine.IsEditorHint() || !CanProcess()) return;
        GenerateMembers();
    }

    private void GetHingeReferences()
    {
        List<UsherHingeAgent> usherHingeAgents = 
            this.FindChildren<UsherHingeAgent>(recursive:true);
        if (usherHingeAgents == null || usherHingeAgents.Count < 2) return;
        _rightHinge = usherHingeAgents.Find(hinge => hinge.Name == "RightHinge");
        _leftHinge = usherHingeAgents.Find(hinge => hinge.Name == "LeftHinge");
    }


    /// <summary>
    /// Aligns the formation's rotation to match the forward direction of its parent
    /// agent.
    /// </summary>
    private void AlignWithAgentForward()
    {
        if (_parentAgent == null) return;
        
        // Agent's forward is X axis, while scalable forward is -Y axis (Up). So we
        // must rotate the formation to match the agent's forward.
        float angle = Vector2.Up.AngleTo(_parentAgent.Forward);
        Rotation = angle;
    }

    /// <summary>
    /// Repositions the formation so that its central axis is aligned with the midpoint
    /// of its vertical dimension. This helps ensure that the formation is correctly
    /// centered relative to its agent origin.
    /// </summary>
    private void RecenterFormation()
    {
        Position = Position with { Y = -FormationDimensions.X / 2 };
    }

    private void UpdateHingesPosition()
    {
        if (_leftHinge == null || _rightHinge == null) return;
        
        // Place hinges at the front corners members.
        // The left corner member is always the first one.
        _leftHinge.Position = MemberPositions[0];
        _rightHinge.Position = GetRightFrontCornerMemberPosition();
    }

    private Vector2 GetRightFrontCornerMemberPosition()
    {
        // The right font corner member is the one with the highest X but lowest Y.
        return MemberPositions
            .OrderByDescending(position => position.X)
            .ThenBy(position => position.Y)
            .First();
    }

    private void UpdateFormation()
    {
        CalculateMembersPositions();
        RecenterFormation();
        UpdateHingesPosition();
        FormationDimensionsChanged?.Invoke(
            this, 
            new FormationDimensionsChangedArgs(
                MemberPositions.ToArray(), 
                MemberRadius));
    }

    private void CalculateMembersPositions()
    {
        switch (Distribution)
        {
            case DistributionType.QuantityAndDimensionsDefined:
                CalculateQuantityAndDimensionsDefinedFormation();
                break;
            case DistributionType.DensityAndQuantityDefined:
                CalculateDensityAndQuantityDefinedFormation();
                break;
            case DistributionType.DensityAndDimensionsDefined:
                CalculateDensityAndDimensionsDefinedFormation();
                break;
        }
    }

    /// <summary>
    /// With quantity and dimensions already defined, what we must calculate is the
    /// resulting formation density.
    /// </summary>
    private void CalculateQuantityAndDimensionsDefinedFormation()
    {
        // Get grid distribution with current quantity and proportions.
        (int columnsAmount, int rowsAmount) = GetGrid();

        // In this kind of formation, the user defines the dimensions unless
        // member separation is under MinimumDistanceBetweenAgents. In that case, 
        // MinimumDistanceBetweenAgents prevails and Quantity is corrected.
        float columnsSeparation = (FormationDimensions.X - 2 * MemberRadius) /
            (columnsAmount - 1);
        float rowsSeparation = (FormationDimensions.Y - 2 * MemberRadius) /
            (rowsAmount - 1);
        if ((columnsSeparation < MinimumDistanceBetweenAgents.X) ||
            (rowsSeparation < MinimumDistanceBetweenAgents.Y))
        {
            // In case of conflict with MinimumDistanceBetweenAgents, we correct the
            // quantity to place the maximum number of agents possible (without
            // going under MinimumDistanceBetweenAgents) in the current formation area.
            columnsSeparation = columnsSeparation < MinimumDistanceBetweenAgents.X ?
                MinimumDistanceBetweenAgents.X : columnsSeparation;
            rowsSeparation = rowsSeparation < MinimumDistanceBetweenAgents.Y ?
                MinimumDistanceBetweenAgents.Y : rowsSeparation;
            columnsAmount = Mathf.FloorToInt(FormationDimensions.X / columnsSeparation);
            rowsAmount = Mathf.FloorToInt(FormationDimensions.Y / rowsSeparation);
            _correctingQuantity = true;
            Quantity = Mathf.RoundToInt(columnsAmount * rowsAmount);
        }
        
        // Update formation density.
        _correctingDensity = true;
        Density = new Vector2(columnsSeparation, rowsSeparation);

        // Once the formation grid is defined, use it to calculate members' local positions.
        MemberPositions.Clear();
        for (int i = 0; i < Quantity; i++)
        {
            int column = i % columnsAmount;
            int row = i / columnsAmount;
            float x = MemberRadius + column * (columnsSeparation);
            float y = MemberRadius + row * (rowsSeparation);
            MemberPositions.Add(new Vector2(x, y));
        }
    }

    /// <summary>
    /// Calculates the number of columns and rows required to evenly distribute agents
    /// within the specified formation dimensions while maintaining proportionality
    /// between the formation's width and length. Ensures the formation accommodates
    /// the given agent quantity.
    /// </summary>
    /// <returns>
    /// A tuple containing the computed number of columns and rows in the grid.
    /// </returns>
    private (int columnsAmount, int rowsAmount) GetGrid(Vector2 formationRelation = new())
    {
        if (formationRelation == Vector2.Zero) formationRelation = FormationDimensions;
        
        // Our formation has two dimensions W (width, FormationsDimensions.X) and L
        // (length, FormationDimensions.Y). Their relation is W/L. We want to 
        // distribute agents evenly across the area, keeping the formation proportions.
        // That distribution will have a number of C columns and R rows. As they will
        // keep proportion, C/R will be approximately the same as W/L.
        // Along with that, our total agents quantity (N) will be approximately the same
        // as C*R. Or, what is the same, R is approximately N/C.
        // Substitute that value for R in the equivalence above between C/R and W/L, and 
        // you will get that C is approximately the square root of N*W/L
        int columnsAmount = Mathf.RoundToInt(
            Mathf.Sqrt(Quantity * formationRelation.X / formationRelation.Y));
        int rowsAmount = Mathf.CeilToInt(Quantity / (float)columnsAmount);
        return (columnsAmount, rowsAmount);
    }

    /// <summary>
    /// With density and quantity already defined, what we must calculate is the
    /// resulting formation dimensions.
    /// </summary>
    private void CalculateDensityAndQuantityDefinedFormation()
    {
        // Get grid distribution with current quantity and proportions.
        (int columnsAmount, int rowsAmount) = GetGrid(_formationFormerDimensions);

        // Once the formation grid is defined, use it to calculate members' local positions.
        MemberPositions.Clear();
        for (int i = 0; i < Quantity; i++)
        {
            int column = i % columnsAmount;
            int row = i / columnsAmount;
            float x = MemberRadius + column * (Density.X);
            float y = MemberRadius + row * (Density.Y);
            MemberPositions.Add(new Vector2(x, y));
        }
        
        // Update formation dimensions.
        _correctingDimensions = true;
        FormationDimensions = new Vector2(
            columnsAmount * Density.X + 2 * MemberRadius,
            rowsAmount * Density.Y + 2 * MemberRadius);
    }
    
    /// <summary>
    /// With density and dimensions already defined, what we must calculate is the
    /// resulting quantity formation members.
    /// </summary>
    private void CalculateDensityAndDimensionsDefinedFormation()
    {
        int columnsAmount = Mathf.FloorToInt(FormationDimensions.X / Density.X);
        int rowsAmount = Mathf.FloorToInt(FormationDimensions.Y / Density.Y);
        _correctingQuantity = true;
        Quantity = Mathf.RoundToInt(columnsAmount * rowsAmount);
        
        // Once the formation grid is defined, use it to calculate members' local
        // positions.
        MemberPositions.Clear();
        for (int i = 0; i < Quantity; i++)
        {
            int column = i % columnsAmount;
            int row = i / columnsAmount;
            float x = MemberRadius + column * (Density.X);
            float y = MemberRadius + row * (Density.Y);
            MemberPositions.Add(new Vector2(x, y));
        }
    }


    /// <summary>
    /// Instantiates and places members of the formation based on predefined positions.
    /// This method iterates through all calculated position offsets and creates
    /// individual formation members at those positions.
    /// </summary>
    private void GenerateMembers()
    {
        foreach (Vector2 positionOffset in MemberPositions)
        {
            GenerateMember(positionOffset);
        }
    }

    /// <summary>
    /// Creates and adds a new member to the formation at the specified local position.
    /// The instantiated member's process mode is disabled to prevent unwanted behavior,
    /// such as automatic movement, making it appropriate for static formation positioning.
    /// </summary>
    /// <param name="positionOffset">The local position at which the member is placed
    /// within the formation.</param>
    private void GenerateMember(Vector2 positionOffset)
    {
        Node2D member = _memberScene.Instantiate<Node2D>();
        member.Position = positionOffset;
        // For my tests I'm instantiating MovingAgents. The problem with then is that
        // they try to move, while they are expected here to be static. That is making
        // my formation movements look weird.
        // So, I'm stopping their movement disabling their process mode.
        // In a more realistic implementation, I would instantiate a specific scene
        // that would not move but would keep its process mode activated to be able
        // to animate or make whatever they are supposed to do.
        member.ProcessMode = ProcessModeEnum.Disabled;
        AddChild(member);
        Members.Add(member);
    }
    
    public override void _Process(double delta)
    {
        if (_leftHinge == null || _rightHinge == null) GetHingeReferences();
        
        DrawGizmos();
    }
    
    private void DrawGizmos()
    {
        QueueRedraw();
    }
    
    public override void _Draw()
    {
        if (!ShowGizmos) return;
        
        // Mark formation origin.
        DrawCircle(Vector2.Zero, _originGizmoRadius, GizmosColor, filled: true);
        DrawLine(Vector2.Zero, Vector2.Up * 50, GizmosColor, 2);
        
        if (MemberPositions == null) return;
        
        // Draw formation positions.
        for (int i=0; i < MemberPositions.Count; i++)
        {
            Vector2 gizmoBorder = new Vector2(MemberRadius, MemberRadius);
            Vector2 textPosition = MemberPositions[i] - gizmoBorder - _gizmoTextOffset;
            DrawString(ThemeDB.FallbackFont, textPosition, $"{i}");
            DrawCircle(
                MemberPositions[i], 
                MemberRadius, 
                GizmosColor, 
                filled: false);
        }
    }

    public override string[] _GetConfigurationWarnings()
    {
        List<string> warnings = new();

        List<UsherHingeAgent> usherHingeAgents = this.FindChildren<UsherHingeAgent>();
        
        if (usherHingeAgents == null || usherHingeAgents.Count < 2)
        {
            warnings.Add("This node needs at least 2 UsherHingeAgents children nodes " +
                         "to work. They need to be direct children.");
        }
        
        if (usherHingeAgents == null) return warnings.ToArray();
        
        bool hasRightHinge = usherHingeAgents.Exists(
            hinge => hinge.Name == "RightHinge");
        if (!hasRightHinge)
        {
            warnings.Add("This node needs a UsherHingeAgent child node named " +
                         "'RightHinge' to work.");
        }
        
        bool hasLeftHinge = usherHingeAgents.Exists(
            hinge => hinge.Name == "LeftHinge");
        if (!hasLeftHinge)
        {
            warnings.Add("This node needs a UsherHingeAgent child node named " +
                         "'LeftHinge' to work.");
        }
        
        return warnings.ToArray();
    }
}