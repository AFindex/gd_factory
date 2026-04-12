using Godot;
using System;
using System.Collections.Generic;

public sealed class FactoryStructureDefinition
{
    public FactoryStructureDefinition(
        BuildPrototypeKind kind,
        Func<FactoryStructure> creator,
        bool allowWorldPlacement,
        bool allowMobileInterior,
        FactoryStructureFootprint? footprint = null)
    {
        Kind = kind;
        Creator = creator;
        AllowWorldPlacement = allowWorldPlacement;
        AllowMobileInterior = allowMobileInterior;
        Footprint = footprint ?? FactoryStructureFootprint.SingleCell;
    }

    public BuildPrototypeKind Kind { get; }
    public Func<FactoryStructure> Creator { get; }
    public bool AllowWorldPlacement { get; }
    public bool AllowMobileInterior { get; }
    public FactoryStructureFootprint Footprint { get; }
}

public static class FactoryStructureFactory
{
    private const string DefaultUnpackerTemplateId = "bulk-iron-ore-standard";
    private const string DefaultPackerTemplateId = "packed-gear-compact";

    private static readonly FactoryStructureFootprint MultiPortProcessingFootprint = new(
        new[]
        {
            Vector2I.Zero,
            Vector2I.Right,
            Vector2I.Right * 2,
            Vector2I.Down,
            Vector2I.Right + Vector2I.Down,
            (Vector2I.Right * 2) + Vector2I.Down
        },
        inputOffsetsEast: new[]
        {
            new Vector2I(0, -1),
            new Vector2I(1, -1),
            new Vector2I(2, -1)
        },
        outputOffsetsEast: new[]
        {
            new Vector2I(0, 2),
            new Vector2I(1, 2),
            new Vector2I(2, 2)
        });
    private static readonly FactoryStructureFootprint HeavyPortFootprint = new(
        new[] { Vector2I.Zero, Vector2I.Up },
        inputOffsetEast: Vector2I.Left,
        outputOffsetEast: Vector2I.Right);
    private static readonly FactoryStructureFootprint CompactConversionFootprint = new(
        new[] { Vector2I.Zero, Vector2I.Down },
        inputOffsetEast: Vector2I.Left,
        outputOffsetEast: Vector2I.Right);
    private static readonly FactoryStructureFootprint StandardConversionFootprint = new(
        new[] { Vector2I.Up, Vector2I.Zero, Vector2I.Down },
        inputOffsetEast: Vector2I.Left,
        outputOffsetEast: Vector2I.Right);
    private static readonly FactoryStructureFootprint WideConversionFootprint = new(
        new[] { Vector2I.Up, Vector2I.Zero, Vector2I.Down, Vector2I.Down * 2 },
        inputOffsetEast: Vector2I.Left,
        outputOffsetEast: Vector2I.Right);

    private static readonly Dictionary<BuildPrototypeKind, FactoryStructureDefinition> Definitions = new()
    {
        [BuildPrototypeKind.Producer] = new FactoryStructureDefinition(BuildPrototypeKind.Producer, () => new ProducerStructure(), true, true),
        [BuildPrototypeKind.MiningDrill] = new FactoryStructureDefinition(BuildPrototypeKind.MiningDrill, () => new MiningDrillStructure(), true, false),
        [BuildPrototypeKind.Generator] = new FactoryStructureDefinition(BuildPrototypeKind.Generator, () => new GeneratorStructure(), true, true),
        [BuildPrototypeKind.PowerPole] = new FactoryStructureDefinition(BuildPrototypeKind.PowerPole, () => new PowerPoleStructure(), true, true),
        [BuildPrototypeKind.Smelter] = new FactoryStructureDefinition(BuildPrototypeKind.Smelter, () => new SmelterStructure(), true, true),
        [BuildPrototypeKind.Assembler] = new FactoryStructureDefinition(BuildPrototypeKind.Assembler, () => new AssemblerStructure(), true, true, MultiPortProcessingFootprint),
        [BuildPrototypeKind.Belt] = new FactoryStructureDefinition(BuildPrototypeKind.Belt, () => new BeltStructure(), true, true),
        [BuildPrototypeKind.Sink] = new FactoryStructureDefinition(BuildPrototypeKind.Sink, () => new SinkStructure(), true, true),
        [BuildPrototypeKind.Splitter] = new FactoryStructureDefinition(BuildPrototypeKind.Splitter, () => new SplitterStructure(), true, true),
        [BuildPrototypeKind.Merger] = new FactoryStructureDefinition(BuildPrototypeKind.Merger, () => new MergerStructure(), true, true),
        [BuildPrototypeKind.Bridge] = new FactoryStructureDefinition(BuildPrototypeKind.Bridge, () => new BridgeStructure(), true, true),
        [BuildPrototypeKind.Loader] = new FactoryStructureDefinition(BuildPrototypeKind.Loader, () => new LoaderStructure(), true, true),
        [BuildPrototypeKind.Unloader] = new FactoryStructureDefinition(BuildPrototypeKind.Unloader, () => new UnloaderStructure(), true, true),
        [BuildPrototypeKind.Storage] = new FactoryStructureDefinition(BuildPrototypeKind.Storage, () => new StorageStructure(), true, true),
        [BuildPrototypeKind.LargeStorageDepot] = new FactoryStructureDefinition(
            BuildPrototypeKind.LargeStorageDepot,
            () => new LargeStorageDepotStructure(),
            true,
            true,
            new FactoryStructureFootprint(
                new[] { Vector2I.Zero, Vector2I.Right, Vector2I.Down, Vector2I.Right + Vector2I.Down },
                inputOffsetEast: new Vector2I(-1, 0),
                outputOffsetEast: new Vector2I(2, 0))),
        [BuildPrototypeKind.CargoUnpacker] = new FactoryStructureDefinition(
            BuildPrototypeKind.CargoUnpacker,
            () => new CargoUnpackerStructure(),
            false,
            true,
            CompactConversionFootprint),
        [BuildPrototypeKind.CargoPacker] = new FactoryStructureDefinition(
            BuildPrototypeKind.CargoPacker,
            () => new CargoPackerStructure(),
            true,
            true,
            CompactConversionFootprint),
        [BuildPrototypeKind.TransferBuffer] = new FactoryStructureDefinition(
            BuildPrototypeKind.TransferBuffer,
            () => new TransferBufferStructure(),
            false,
            true),
        [BuildPrototypeKind.DebugOreSource] = new FactoryStructureDefinition(BuildPrototypeKind.DebugOreSource, () => new DebugOreSourceStructure(), true, true),
        [BuildPrototypeKind.DebugPartSource] = new FactoryStructureDefinition(BuildPrototypeKind.DebugPartSource, () => new DebugPartSourceStructure(), true, true),
        [BuildPrototypeKind.DebugCombatSource] = new FactoryStructureDefinition(BuildPrototypeKind.DebugCombatSource, () => new DebugCombatSourceStructure(), true, true),
        [BuildPrototypeKind.DebugPowerGenerator] = new FactoryStructureDefinition(BuildPrototypeKind.DebugPowerGenerator, () => new DebugPowerGeneratorStructure(), true, true),
        [BuildPrototypeKind.Inserter] = new FactoryStructureDefinition(BuildPrototypeKind.Inserter, () => new InserterStructure(), true, true),
        [BuildPrototypeKind.Wall] = new FactoryStructureDefinition(BuildPrototypeKind.Wall, () => new WallStructure(), true, true),
        [BuildPrototypeKind.AmmoAssembler] = new FactoryStructureDefinition(BuildPrototypeKind.AmmoAssembler, () => new AmmoAssemblerStructure(), true, true, MultiPortProcessingFootprint),
        [BuildPrototypeKind.GunTurret] = new FactoryStructureDefinition(BuildPrototypeKind.GunTurret, () => new GunTurretStructure(), true, true),
        [BuildPrototypeKind.HeavyGunTurret] = new FactoryStructureDefinition(
            BuildPrototypeKind.HeavyGunTurret,
            () => new HeavyGunTurretStructure(),
            true,
            true,
            new FactoryStructureFootprint(
                new[] { Vector2I.Zero, Vector2I.Right, Vector2I.Down, Vector2I.Right + Vector2I.Down })),
        [BuildPrototypeKind.OutputPort] = new FactoryStructureDefinition(BuildPrototypeKind.OutputPort, () => new MobileFactoryOutputPortStructure(), false, true),
        [BuildPrototypeKind.InputPort] = new FactoryStructureDefinition(BuildPrototypeKind.InputPort, () => new MobileFactoryInputPortStructure(), false, true),
        [BuildPrototypeKind.MiningInputPort] = new FactoryStructureDefinition(BuildPrototypeKind.MiningInputPort, () => new MobileFactoryMiningInputPortStructure(), false, true)
    };

    public static FactoryStructure Create(BuildPrototypeKind kind, FactoryStructurePlacement placement)
    {
        if (!Definitions.TryGetValue(kind, out var definition))
        {
            GD.PushWarning($"Unknown structure kind '{kind}', falling back to belt.");
            definition = Definitions[BuildPrototypeKind.Belt];
        }

        var siteKind = FactoryIndustrialStandards.ResolveSiteKind(placement.Site);
        if (!FactoryIndustrialStandards.IsStructureAllowed(kind, siteKind))
        {
            throw new InvalidOperationException(FactoryIndustrialStandards.GetPlacementCompatibilityError(kind, siteKind));
        }

        var resolvedFootprint = placement.Footprint
            ?? GetFootprint(kind, placement.Configuration, placement.MapRecipeId);
        var structure = definition.Creator();
        structure.Configure(placement.Site, placement.Cell, placement.Facing, footprint: resolvedFootprint);
        return structure;
    }

    public static FactoryStructureDefinition GetDefinition(BuildPrototypeKind kind)
    {
        return Definitions[kind];
    }

    public static bool TryGetDefinition(BuildPrototypeKind kind, out FactoryStructureDefinition? definition)
    {
        return Definitions.TryGetValue(kind, out definition);
    }

    public static FactoryStructure CreateGhostPreview(
        BuildPrototypeKind kind,
        FactoryStructurePlacement placement)
    {
        var structure = Create(kind, placement);
        return structure;
    }

    public static FactoryStructureFootprint GetFootprint(BuildPrototypeKind kind)
    {
        return GetFootprint(kind, configuration: null, mapRecipeId: null);
    }

    public static FactoryStructureFootprint GetFootprint(
        BuildPrototypeKind kind,
        IReadOnlyDictionary<string, string>? configuration,
        string? mapRecipeId = null)
    {
        return kind switch
        {
            BuildPrototypeKind.CargoUnpacker => ResolveConversionFootprint(
                ResolveBundleTemplateId(configuration, mapRecipeId, DefaultUnpackerTemplateId)),
            BuildPrototypeKind.CargoPacker => ResolveConversionFootprint(
                ResolveBundleTemplateId(configuration, mapRecipeId, DefaultPackerTemplateId)),
            BuildPrototypeKind.InputPort or BuildPrototypeKind.OutputPort => HeavyPortFootprint,
            _ => GetDefinition(kind).Footprint
        };
    }

    private static string ResolveBundleTemplateId(
        IReadOnlyDictionary<string, string>? configuration,
        string? mapRecipeId,
        string fallbackTemplateId)
    {
        if (configuration is not null
            && configuration.TryGetValue("bundle_template_id", out var configuredTemplateId)
            && FactoryBundleCatalog.TryGet(configuredTemplateId, out _))
        {
            return configuredTemplateId;
        }

        if (!string.IsNullOrWhiteSpace(mapRecipeId) && FactoryBundleCatalog.TryGet(mapRecipeId, out _))
        {
            return mapRecipeId;
        }

        return fallbackTemplateId;
    }

    private static FactoryStructureFootprint ResolveConversionFootprint(string templateId)
    {
        if (!FactoryBundleCatalog.TryGet(templateId, out var template) || template is null)
        {
            return CompactConversionFootprint;
        }

        return template.SizeTier switch
        {
            FactoryBundleSizeTier.Wide => WideConversionFootprint,
            FactoryBundleSizeTier.Standard => StandardConversionFootprint,
            _ => CompactConversionFootprint
        };
    }
}
