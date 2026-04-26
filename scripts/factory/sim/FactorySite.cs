using Godot;
using System.Collections.Generic;

public enum GridReservationKind
{
    StaticStructure,
    MobileFootprint,
    MobilePort
}

public sealed class GridReservation
{
    public GridReservation(string ownerId, GridReservationKind kind, FactoryStructure? structure = null)
    {
        OwnerId = ownerId;
        Kind = kind;
        Structure = structure;
    }

    public string OwnerId { get; }
    public GridReservationKind Kind { get; }
    public FactoryStructure? Structure { get; }
}

public readonly struct FactoryStructurePlacement
{
    public FactoryStructurePlacement(
        IFactorySite site,
        Vector2I cell,
        FacingDirection facing,
        FactoryStructureFootprint? footprint = null,
        IReadOnlyDictionary<string, string>? configuration = null,
        string? mapRecipeId = null)
    {
        Site = site;
        Cell = cell;
        Facing = facing;
        Footprint = footprint;
        Configuration = configuration;
        MapRecipeId = mapRecipeId;
    }

    public IFactorySite Site { get; }
    public Vector2I Cell { get; }
    public FacingDirection Facing { get; }
    public FactoryStructureFootprint? Footprint { get; }
    public IReadOnlyDictionary<string, string>? Configuration { get; }
    public string? MapRecipeId { get; }
}

public interface IFactorySite
{
    string SiteId { get; }
    float CellSize { get; }
    bool IsVisible { get; }
    bool IsSimulationActive { get; }
    float CombatOverlayScale { get; }
    float WorldRotationRadians { get; }

    bool IsInBounds(Vector2I cell);
    Vector3 CellToWorld(Vector2I cell);
    bool CanPlaceCells(IReadOnlyList<Vector2I> cells, string? ownerId = null);
    bool TryGetStructure(Vector2I cell, out FactoryStructure? structure);
    bool TrySendItem(FactoryStructure source, Vector2I sourceCell, Vector2I targetCell, FactoryItem item, SimulationController simulation);
    void RemoveStructure(FactoryStructure structure);
}

public interface IFactoryTopologyAware
{
    void RefreshTopology();
}
