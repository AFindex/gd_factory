using Godot;
using System;
using System.Collections.Generic;

public sealed class FactoryStructureDefinition
{
    public FactoryStructureDefinition(
        BuildPrototypeKind kind,
        Func<FactoryStructure> creator,
        bool allowWorldPlacement,
        bool allowMobileInterior)
    {
        Kind = kind;
        Creator = creator;
        AllowWorldPlacement = allowWorldPlacement;
        AllowMobileInterior = allowMobileInterior;
    }

    public BuildPrototypeKind Kind { get; }
    public Func<FactoryStructure> Creator { get; }
    public bool AllowWorldPlacement { get; }
    public bool AllowMobileInterior { get; }
}

public static class FactoryStructureFactory
{
    private static readonly Dictionary<BuildPrototypeKind, FactoryStructureDefinition> Definitions = new()
    {
        [BuildPrototypeKind.Producer] = new FactoryStructureDefinition(BuildPrototypeKind.Producer, () => new ProducerStructure(), true, true),
        [BuildPrototypeKind.Belt] = new FactoryStructureDefinition(BuildPrototypeKind.Belt, () => new BeltStructure(), true, true),
        [BuildPrototypeKind.Sink] = new FactoryStructureDefinition(BuildPrototypeKind.Sink, () => new SinkStructure(), true, false),
        [BuildPrototypeKind.Splitter] = new FactoryStructureDefinition(BuildPrototypeKind.Splitter, () => new SplitterStructure(), true, true),
        [BuildPrototypeKind.Merger] = new FactoryStructureDefinition(BuildPrototypeKind.Merger, () => new MergerStructure(), true, true),
        [BuildPrototypeKind.Bridge] = new FactoryStructureDefinition(BuildPrototypeKind.Bridge, () => new BridgeStructure(), true, false),
        [BuildPrototypeKind.Loader] = new FactoryStructureDefinition(BuildPrototypeKind.Loader, () => new LoaderStructure(), true, false),
        [BuildPrototypeKind.Unloader] = new FactoryStructureDefinition(BuildPrototypeKind.Unloader, () => new UnloaderStructure(), true, false)
    };

    public static FactoryStructure Create(BuildPrototypeKind kind, FactoryStructurePlacement placement)
    {
        if (!Definitions.TryGetValue(kind, out var definition))
        {
            GD.PushWarning($"Unknown structure kind '{kind}', falling back to belt.");
            definition = Definitions[BuildPrototypeKind.Belt];
        }

        if (placement.Site is GridManager && !definition.AllowWorldPlacement)
        {
            throw new InvalidOperationException($"Structure kind '{kind}' is not configured for world placement.");
        }

        if (placement.Site is MobileFactorySite && !definition.AllowMobileInterior)
        {
            throw new InvalidOperationException($"Structure kind '{kind}' is not configured for mobile interior placement.");
        }

        var structure = definition.Creator();
        structure.Configure(placement.Site, placement.Cell, placement.Facing);
        return structure;
    }

    public static FactoryStructureDefinition GetDefinition(BuildPrototypeKind kind)
    {
        return Definitions[kind];
    }
}
