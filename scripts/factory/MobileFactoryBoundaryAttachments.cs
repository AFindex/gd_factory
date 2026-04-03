using Godot;
using System.Collections.Generic;

public sealed class MobileFactoryBoundaryAttachmentDefinition
{
    public MobileFactoryBoundaryAttachmentDefinition(
        BuildPrototypeKind kind,
        string displayName,
        string description,
        MobileFactoryAttachmentChannelType channelType,
        Color tint,
        Color connectorColor,
        IReadOnlyList<Vector2I> interiorStencil,
        IReadOnlyList<Vector2I> boundaryStencil,
        IReadOnlyList<Vector2I> exteriorStencil)
    {
        Kind = kind;
        DisplayName = displayName;
        Description = description;
        ChannelType = channelType;
        Tint = tint;
        ConnectorColor = connectorColor;
        InteriorStencil = interiorStencil;
        BoundaryStencil = boundaryStencil;
        ExteriorStencil = exteriorStencil;
    }

    public BuildPrototypeKind Kind { get; }
    public string DisplayName { get; }
    public string Description { get; }
    public MobileFactoryAttachmentChannelType ChannelType { get; }
    public Color Tint { get; }
    public Color ConnectorColor { get; }
    public IReadOnlyList<Vector2I> InteriorStencil { get; }
    public IReadOnlyList<Vector2I> BoundaryStencil { get; }
    public IReadOnlyList<Vector2I> ExteriorStencil { get; }
}

public sealed class MobileFactoryAttachmentMount
{
    public MobileFactoryAttachmentMount(
        string id,
        Vector2I cell,
        FacingDirection facing,
        Vector2I worldPortOffsetEast,
        IReadOnlyList<BuildPrototypeKind> allowedKinds)
    {
        Id = id;
        Cell = cell;
        Facing = facing;
        WorldPortOffsetEast = worldPortOffsetEast;
        AllowedKinds = allowedKinds;
    }

    public string Id { get; }
    public Vector2I Cell { get; }
    public FacingDirection Facing { get; }
    public Vector2I WorldPortOffsetEast { get; }
    public IReadOnlyList<BuildPrototypeKind> AllowedKinds { get; }

    public bool Allows(BuildPrototypeKind kind)
    {
        for (var i = 0; i < AllowedKinds.Count; i++)
        {
            if (AllowedKinds[i] == kind)
            {
                return true;
            }
        }

        return false;
    }
}

public readonly struct MobileFactoryAttachmentPlacementSpec
{
    public MobileFactoryAttachmentPlacementSpec(BuildPrototypeKind kind, Vector2I cell, FacingDirection facing)
    {
        Kind = kind;
        Cell = cell;
        Facing = facing;
    }

    public BuildPrototypeKind Kind { get; }
    public Vector2I Cell { get; }
    public FacingDirection Facing { get; }
}

public sealed class MobileFactoryAttachmentProjection
{
    public MobileFactoryAttachmentProjection(
        MobileFactoryBoundaryAttachmentStructure attachment,
        MobileFactoryAttachmentMount mount,
        FacingDirection deploymentFacing,
        IReadOnlyList<Vector2I> interiorCells,
        IReadOnlyList<Vector2I> boundaryCells,
        IReadOnlyList<Vector2I> exteriorStencilCells,
        IReadOnlyList<Vector2I> worldCells,
        Vector2I worldPortCell,
        FacingDirection worldFacing)
    {
        Attachment = attachment;
        Mount = mount;
        DeploymentFacing = deploymentFacing;
        InteriorCells = interiorCells;
        BoundaryCells = boundaryCells;
        ExteriorStencilCells = exteriorStencilCells;
        WorldCells = worldCells;
        WorldPortCell = worldPortCell;
        WorldFacing = worldFacing;
    }

    public MobileFactoryBoundaryAttachmentStructure Attachment { get; }
    public MobileFactoryAttachmentMount Mount { get; }
    public FacingDirection DeploymentFacing { get; }
    public IReadOnlyList<Vector2I> InteriorCells { get; }
    public IReadOnlyList<Vector2I> BoundaryCells { get; }
    public IReadOnlyList<Vector2I> ExteriorStencilCells { get; }
    public IReadOnlyList<Vector2I> WorldCells { get; }
    public Vector2I WorldPortCell { get; }
    public FacingDirection WorldFacing { get; }
    public Vector2I WorldAdjacentCell => WorldPortCell + FactoryDirection.ToCellOffset(WorldFacing);
}

public static class MobileFactoryBoundaryAttachmentCatalog
{
    private static readonly Dictionary<BuildPrototypeKind, MobileFactoryBoundaryAttachmentDefinition> Definitions = new()
    {
        [BuildPrototypeKind.OutputPort] = new MobileFactoryBoundaryAttachmentDefinition(
            BuildPrototypeKind.OutputPort,
            "输出端口",
            "把内部物流推出工厂边界并接入世界网格。",
            MobileFactoryAttachmentChannelType.ItemOutput,
            new Color("FB923C"),
            new Color("FED7AA"),
            new[] { Vector2I.Zero },
            new[] { Vector2I.Right },
            new[] { new Vector2I(2, 0) }),
        [BuildPrototypeKind.InputPort] = new MobileFactoryBoundaryAttachmentDefinition(
            BuildPrototypeKind.InputPort,
            "输入端口",
            "从外部世界吸入物流并送入工厂内部。",
            MobileFactoryAttachmentChannelType.ItemInput,
            new Color("60A5FA"),
            new Color("BFDBFE"),
            new[] { Vector2I.Zero },
            new[] { Vector2I.Right },
            new[] { new Vector2I(2, 0) }),
        [BuildPrototypeKind.MiningInputPort] = new MobileFactoryBoundaryAttachmentDefinition(
            BuildPrototypeKind.MiningInputPort,
            "采矿输入端口",
            "在部署后把世界侧矿区直接接成移动工厂的采矿入口。",
            MobileFactoryAttachmentChannelType.ItemInput,
            new Color("34D399"),
            new Color("A7F3D0"),
            new[] { Vector2I.Zero },
            new[] { Vector2I.Right },
            new[]
            {
                new Vector2I(2, 1),
                new Vector2I(3, 1),
                new Vector2I(2, 2),
                new Vector2I(3, 2)
            })
    };

    public static bool IsAttachmentKind(BuildPrototypeKind kind)
    {
        return Definitions.ContainsKey(kind);
    }

    public static MobileFactoryBoundaryAttachmentDefinition Get(BuildPrototypeKind kind)
    {
        return Definitions[kind];
    }

    public static IEnumerable<MobileFactoryBoundaryAttachmentDefinition> GetAll()
    {
        return Definitions.Values;
    }
}

public static class MobileFactoryBoundaryAttachmentGeometry
{
    public static List<Vector2I> GetInteriorCells(MobileFactoryBoundaryAttachmentDefinition definition, Vector2I anchorCell, FacingDirection facing)
    {
        return ResolveLocalCells(definition.InteriorStencil, anchorCell, facing);
    }

    public static List<Vector2I> GetBoundaryCells(MobileFactoryBoundaryAttachmentDefinition definition, Vector2I anchorCell, FacingDirection facing)
    {
        return ResolveLocalCells(definition.BoundaryStencil, anchorCell, facing);
    }

    public static List<Vector2I> GetExteriorStencilCells(MobileFactoryBoundaryAttachmentDefinition definition, Vector2I anchorCell, FacingDirection facing)
    {
        return ResolveLocalCells(definition.ExteriorStencil, anchorCell, facing);
    }

    public static MobileFactoryAttachmentProjection CreateProjection(
        MobileFactoryBoundaryAttachmentStructure attachment,
        MobileFactoryAttachmentMount mount,
        Vector2I factoryAnchorCell,
        FacingDirection deploymentFacing)
    {
        var definition = attachment.AttachmentDefinition;
        var interiorCells = GetInteriorCells(definition, attachment.Cell, attachment.Facing);
        var boundaryCells = GetBoundaryCells(definition, attachment.Cell, attachment.Facing);
        var exteriorStencilCells = GetExteriorStencilCells(definition, attachment.Cell, attachment.Facing);

        var worldPortCell = factoryAnchorCell + FactoryDirection.RotateOffset(mount.WorldPortOffsetEast, deploymentFacing);
        var worldFacing = FactoryDirection.RotateBy(mount.Facing, deploymentFacing);
        var worldCells = new List<Vector2I>(definition.ExteriorStencil.Count);
        for (var i = 0; i < definition.ExteriorStencil.Count; i++)
        {
            var local = definition.ExteriorStencil[i] - new Vector2I(2, 0);
            worldCells.Add(worldPortCell + FactoryDirection.RotateOffset(local, worldFacing));
        }

        if (worldCells.Count == 0)
        {
            worldCells.Add(worldPortCell);
        }

        return new MobileFactoryAttachmentProjection(
            attachment,
            mount,
            deploymentFacing,
            interiorCells,
            boundaryCells,
            exteriorStencilCells,
            worldCells,
            worldPortCell,
            worldFacing);
    }

    private static List<Vector2I> ResolveLocalCells(IReadOnlyList<Vector2I> stencil, Vector2I anchorCell, FacingDirection facing)
    {
        var cells = new List<Vector2I>(stencil.Count);
        for (var i = 0; i < stencil.Count; i++)
        {
            cells.Add(anchorCell + FactoryDirection.RotateOffset(stencil[i], facing));
        }

        return cells;
    }
}
