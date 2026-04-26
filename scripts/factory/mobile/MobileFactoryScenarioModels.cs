using Godot;
using System.Collections.Generic;

public readonly struct FactoryPlacementSpec
{
    public FactoryPlacementSpec(BuildPrototypeKind kind, Vector2I cell, FacingDirection facing, string? recipeId = null)
    {
        Kind = kind;
        Cell = cell;
        Facing = facing;
        RecipeId = recipeId;
    }

    public BuildPrototypeKind Kind { get; }
    public Vector2I Cell { get; }
    public FacingDirection Facing { get; }
    public string? RecipeId { get; }
}

public sealed class MobileFactoryInteriorPreset
{
    public MobileFactoryInteriorPreset(
        string id,
        string displayName,
        string description,
        string recoverySummary,
        IReadOnlyList<FactoryPlacementSpec> placements,
        IReadOnlyList<MobileFactoryAttachmentPlacementSpec>? attachmentPlacements = null)
    {
        Id = id;
        DisplayName = displayName;
        Description = description;
        RecoverySummary = recoverySummary;
        Placements = placements;
        AttachmentPlacements = attachmentPlacements ?? new List<MobileFactoryAttachmentPlacementSpec>();
    }

    public string Id { get; }
    public string DisplayName { get; }
    public string Description { get; }
    public string RecoverySummary { get; }
    public IReadOnlyList<FactoryPlacementSpec> Placements { get; }
    public IReadOnlyList<MobileFactoryAttachmentPlacementSpec> AttachmentPlacements { get; }
}

public sealed class MobileFactoryProfile
{
    public MobileFactoryProfile(
        string id,
        string displayName,
        Vector2I interiorMinCell,
        Vector2I interiorMaxCell,
        float interiorCellSize,
        float interiorFloorHeight,
        float interiorPlatformBorder,
        IReadOnlyList<Vector2I> footprintOffsetsEast,
        IReadOnlyList<Vector2I> portOffsetsEast,
        Vector2I outputBridgeCell,
        FacingDirection outputBridgeFacing,
        Vector3 transitParkingCenter,
        Color hullColor,
        Color cabColor,
        Color accentColor,
        Color portColor,
        IReadOnlyList<MobileFactoryAttachmentMount>? attachmentMounts = null)
    {
        Id = id;
        DisplayName = displayName;
        InteriorMinCell = interiorMinCell;
        InteriorMaxCell = interiorMaxCell;
        InteriorCellSize = interiorCellSize;
        InteriorFloorHeight = interiorFloorHeight;
        InteriorPlatformBorder = interiorPlatformBorder;
        FootprintOffsetsEast = footprintOffsetsEast;
        PortOffsetsEast = portOffsetsEast;
        OutputBridgeCell = outputBridgeCell;
        OutputBridgeFacing = outputBridgeFacing;
        TransitParkingCenter = transitParkingCenter;
        HullColor = hullColor;
        CabColor = cabColor;
        AccentColor = accentColor;
        PortColor = portColor;
        AttachmentMounts = attachmentMounts ?? new List<MobileFactoryAttachmentMount>();
    }

    public string Id { get; }
    public string DisplayName { get; }
    public Vector2I InteriorMinCell { get; }
    public Vector2I InteriorMaxCell { get; }
    public float InteriorCellSize { get; }
    public float InteriorFloorHeight { get; }
    public float InteriorPlatformBorder { get; }
    public IReadOnlyList<Vector2I> FootprintOffsetsEast { get; }
    public IReadOnlyList<Vector2I> PortOffsetsEast { get; }
    public Vector2I OutputBridgeCell { get; }
    public FacingDirection OutputBridgeFacing { get; }
    public Vector3 TransitParkingCenter { get; }
    public Color HullColor { get; }
    public Color CabColor { get; }
    public Color AccentColor { get; }
    public Color PortColor { get; }
    public IReadOnlyList<MobileFactoryAttachmentMount> AttachmentMounts { get; }

    public int InteriorWidth => InteriorMaxCell.X - InteriorMinCell.X + 1;
    public int InteriorHeight => InteriorMaxCell.Y - InteriorMinCell.Y + 1;

    public bool TryGetAttachmentMount(Vector2I cell, FacingDirection facing, BuildPrototypeKind kind, out MobileFactoryAttachmentMount? mount)
    {
        for (var i = 0; i < AttachmentMounts.Count; i++)
        {
            var candidate = AttachmentMounts[i];
            if (candidate.Cell == cell && candidate.Facing == facing && candidate.Allows(kind))
            {
                mount = candidate;
                return true;
            }
        }

        mount = null;
        return false;
    }

    public bool TryGetAttachmentMount(Vector2I cell, out MobileFactoryAttachmentMount? mount)
    {
        for (var i = 0; i < AttachmentMounts.Count; i++)
        {
            var candidate = AttachmentMounts[i];
            if (candidate.Cell == cell)
            {
                mount = candidate;
                return true;
            }
        }

        mount = null;
        return false;
    }
}

public sealed class MobileFactoryRoutePoint
{
    public MobileFactoryRoutePoint(Vector3 transitPosition, FacingDirection transitFacing, Vector2I deployAnchor, FacingDirection deployFacing, float transitHoldSeconds, float deployedHoldSeconds)
    {
        TransitPosition = transitPosition;
        TransitFacing = transitFacing;
        DeployAnchor = deployAnchor;
        DeployFacing = deployFacing;
        TransitHoldSeconds = transitHoldSeconds;
        DeployedHoldSeconds = deployedHoldSeconds;
    }

    public Vector3 TransitPosition { get; }
    public FacingDirection TransitFacing { get; }
    public Vector2I DeployAnchor { get; }
    public FacingDirection DeployFacing { get; }
    public float TransitHoldSeconds { get; }
    public float DeployedHoldSeconds { get; }
}

public sealed class MobileFactoryScenarioActorDefinition
{
    public MobileFactoryScenarioActorDefinition(
        string actorId,
        string displayLabel,
        MobileFactoryProfile profile,
        MobileFactoryInteriorPreset interiorPreset,
        bool isPlayerControlled,
        Vector3 transitPosition,
        FacingDirection transitFacing,
        Vector2I? initialDeployAnchor,
        FacingDirection initialDeployFacing,
        IReadOnlyList<MobileFactoryRoutePoint>? routePoints,
        Color labelColor)
    {
        ActorId = actorId;
        DisplayLabel = displayLabel;
        Profile = profile;
        InteriorPreset = interiorPreset;
        IsPlayerControlled = isPlayerControlled;
        TransitPosition = transitPosition;
        TransitFacing = transitFacing;
        InitialDeployAnchor = initialDeployAnchor;
        InitialDeployFacing = initialDeployFacing;
        RoutePoints = routePoints ?? new List<MobileFactoryRoutePoint>();
        LabelColor = labelColor;
    }

    public string ActorId { get; }
    public string DisplayLabel { get; }
    public MobileFactoryProfile Profile { get; }
    public MobileFactoryInteriorPreset InteriorPreset { get; }
    public bool IsPlayerControlled { get; }
    public Vector3 TransitPosition { get; }
    public FacingDirection TransitFacing { get; }
    public Vector2I? InitialDeployAnchor { get; }
    public FacingDirection InitialDeployFacing { get; }
    public IReadOnlyList<MobileFactoryRoutePoint> RoutePoints { get; }
    public Color LabelColor { get; }
}
