using Godot;
using System;
using System.Collections.Generic;
using System.IO;

public sealed class FactoryWorldMapLoadResult
{
    private readonly Dictionary<Vector2I, FactoryStructure> _structuresByAnchorCell;
    private readonly Dictionary<string, Vector2I> _anchors;

    public FactoryWorldMapLoadResult(
        FactoryMapDocument document,
        Dictionary<Vector2I, FactoryStructure> structuresByAnchorCell,
        IReadOnlyList<FactoryStructure> loadedStructures,
        IReadOnlyDictionary<string, Vector2I> anchors)
    {
        Document = document;
        _structuresByAnchorCell = structuresByAnchorCell;
        LoadedStructures = loadedStructures;
        _anchors = new Dictionary<string, Vector2I>(anchors, StringComparer.OrdinalIgnoreCase);
    }

    public FactoryMapDocument Document { get; }
    public IReadOnlyList<FactoryStructure> LoadedStructures { get; }

    public bool TryGetStructure(Vector2I cell, out FactoryStructure? structure)
    {
        return _structuresByAnchorCell.TryGetValue(cell, out structure);
    }

    public bool TryGetAnchor(string id, out Vector2I cell)
    {
        return _anchors.TryGetValue(id, out cell);
    }
}

public static class FactoryMapRuntimeLoader
{
    public static FactoryWorldMapLoadResult LoadWorldMap(
        string resourcePath,
        GridManager site,
        Node3D structureRoot,
        SimulationController simulation)
    {
        var document = FactoryMapValidator.ValidateDocument(FactoryMapSerializer.LoadFromFile(resourcePath));
        return LoadWorldMapDocument(resourcePath, document, site, structureRoot, simulation);
    }

    public static FactoryWorldMapLoadResult LoadWorldMapDocument(
        string resourcePath,
        FactoryMapDocument document,
        GridManager site,
        Node3D structureRoot,
        SimulationController simulation,
        bool applyDocumentRuntimeState = true)
    {
        foreach (var _ in site.GetStructures())
        {
            throw new InvalidOperationException($"World site '{site.SiteId}' must be empty before loading '{resourcePath}'.");
        }

        document = FactoryMapValidator.ValidateDocument(document);
        FactoryMapValidator.ValidateAgainstSiteBounds(document, site.MinCell, site.MaxCell, FactoryMapKind.World);
        LogDocument("Loaded world map document", resourcePath, document);

        var deposits = BuildDeposits(document.Deposits);
        site.SetResourceDeposits(deposits);

        var structuresByAnchorCell = new Dictionary<Vector2I, FactoryStructure>();
        var loadedStructures = new List<FactoryStructure>(document.Structures.Count);
        for (var i = 0; i < document.Structures.Count; i++)
        {
            var entry = document.Structures[i];
            if (!site.CanPlaceStructure(entry.Kind, entry.Cell, entry.Facing, out var reason))
            {
                throw new InvalidDataException(
                    $"World map structure '{entry.Kind}' at ({entry.Cell.X}, {entry.Cell.Y}) failed placement validation: {reason}");
            }

            var structure = FactoryStructureFactory.Create(entry.Kind, new FactoryStructurePlacement(site, entry.Cell, entry.Facing));
            structureRoot.AddChild(structure);
            site.PlaceStructure(structure);
            simulation.RegisterStructure(structure);
            structuresByAnchorCell[entry.Cell] = structure;
            loadedStructures.Add(structure);
        }

        if (applyDocumentRuntimeState)
        {
            ApplyStructureRuntimeState(document, simulation, structuresByAnchorCell);
        }

        var anchors = new Dictionary<string, Vector2I>(StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < document.Anchors.Count; i++)
        {
            anchors[document.Anchors[i].Id] = document.Anchors[i].Cell;
        }

        return new FactoryWorldMapLoadResult(document, structuresByAnchorCell, loadedStructures, anchors);
    }

    public static MobileFactoryInteriorPreset LoadInteriorPreset(
        string resourcePath,
        string presetId,
        string displayName,
        string description,
        string recoverySummary,
        MobileFactoryProfile? profile = null)
    {
        var document = FactoryMapValidator.ValidateDocument(FactoryMapSerializer.LoadFromFile(resourcePath));
        if (document.Kind != FactoryMapKind.Interior)
        {
            throw new InvalidDataException($"Map '{resourcePath}' is not an interior map.");
        }

        if (profile is not null)
        {
            ValidateInteriorDocumentAgainstProfile(document, profile, resourcePath);
        }

        LogDocument("Loaded interior preset document", resourcePath, document);

        var placements = new List<FactoryPlacementSpec>();
        var attachmentPlacements = new List<MobileFactoryAttachmentPlacementSpec>();
        for (var i = 0; i < document.Structures.Count; i++)
        {
            var entry = document.Structures[i];
            if (MobileFactoryBoundaryAttachmentCatalog.IsAttachmentKind(entry.Kind))
            {
                attachmentPlacements.Add(new MobileFactoryAttachmentPlacementSpec(entry.Kind, entry.Cell, entry.Facing));
            }
            else
            {
                placements.Add(new FactoryPlacementSpec(entry.Kind, entry.Cell, entry.Facing));
            }
        }

        return new MobileFactoryInteriorPreset(
            presetId,
            displayName,
            description,
            recoverySummary,
            placements,
            attachmentPlacements);
    }

    public static void ValidateInteriorDocumentAgainstProfile(
        FactoryMapDocument document,
        MobileFactoryProfile profile,
        string resourcePath)
    {
        FactoryMapValidator.ValidateAgainstSiteBounds(document, profile.InteriorMinCell, profile.InteriorMaxCell, FactoryMapKind.Interior);

        for (var i = 0; i < document.Structures.Count; i++)
        {
            var entry = document.Structures[i];
            if (!MobileFactoryBoundaryAttachmentCatalog.IsAttachmentKind(entry.Kind))
            {
                continue;
            }

            if (!profile.TryGetAttachmentMount(entry.Cell, entry.Facing, entry.Kind, out var mount) || mount is null)
            {
                throw new InvalidDataException(
                    $"Interior map '{resourcePath}' places attachment '{entry.Kind}' at ({entry.Cell.X}, {entry.Cell.Y}) facing {entry.Facing}, but the selected mobile factory profile '{profile.Id}' does not allow that mount.");
            }
        }
    }

    public static void ApplyInteriorRuntimeState(
        string resourcePath,
        MobileFactoryInstance factory,
        SimulationController simulation)
    {
        var document = FactoryMapValidator.ValidateDocument(FactoryMapSerializer.LoadFromFile(resourcePath));
        ApplyInteriorRuntimeDocument(resourcePath, document, factory, simulation);
    }

    public static void ApplyInteriorRuntimeDocument(
        string resourcePath,
        FactoryMapDocument document,
        MobileFactoryInstance factory,
        SimulationController simulation,
        bool applyDocumentRuntimeState = true)
    {
        FactoryMapValidator.ValidateAgainstSiteBounds(document, factory.InteriorMinCell, factory.InteriorMaxCell, FactoryMapKind.Interior);
        LogDocument("Applying interior runtime state", resourcePath, document);

        var structuresByAnchorCell = new Dictionary<Vector2I, FactoryStructure>();
        for (var i = 0; i < document.Structures.Count; i++)
        {
            var entry = document.Structures[i];
            if (!factory.TryGetInteriorStructure(entry.Cell, out var structure) || structure is null)
            {
                throw new InvalidDataException(
                    $"Interior map '{resourcePath}' expected a structure at ({entry.Cell.X}, {entry.Cell.Y}) after preset reconstruction.");
            }

            structuresByAnchorCell[entry.Cell] = structure;
        }

        if (applyDocumentRuntimeState)
        {
            ApplyStructureRuntimeState(document, simulation, structuresByAnchorCell);
        }
    }

    public static void LoadInteriorMapDocument(
        string resourcePath,
        FactoryMapDocument document,
        MobileFactoryInstance factory,
        SimulationController simulation,
        bool applyDocumentRuntimeState = true)
    {
        document = FactoryMapValidator.ValidateDocument(document);
        FactoryMapValidator.ValidateAgainstSiteBounds(document, factory.InteriorMinCell, factory.InteriorMaxCell, FactoryMapKind.Interior);
        LogDocument("Loaded interior map document", resourcePath, document);
        factory.RebuildInteriorFromMapDocument(document, rebuildTopology: false);
        ApplyInteriorRuntimeDocument(resourcePath, document, factory, simulation, applyDocumentRuntimeState);
    }

    public static bool VerifyRoundTrip(string resourcePath)
    {
        try
        {
            var document = FactoryMapValidator.ValidateDocument(FactoryMapSerializer.LoadFromFile(resourcePath));
            var roundTrip = FactoryMapValidator.ValidateDocument(FactoryMapSerializer.Deserialize(FactoryMapSerializer.Serialize(document), $"{resourcePath}#roundtrip"));
            return document.Kind == roundTrip.Kind
                && document.Version == roundTrip.Version
                && document.MinCell == roundTrip.MinCell
                && document.MaxCell == roundTrip.MaxCell
                && document.Deposits.Count == roundTrip.Deposits.Count
                && document.Structures.Count == roundTrip.Structures.Count
                && document.Anchors.Count == roundTrip.Anchors.Count
                && document.Markers.Count == roundTrip.Markers.Count;
        }
        catch
        {
            return false;
        }
    }

    public static bool VerifyMalformedMapRejected()
    {
        const string malformedDocument = """
version|1
kind|world
bounds|0|0|2|2
structure|Belt|0|0|East
structure|Belt|0|0|South
""";

        try
        {
            FactoryMapValidator.ValidateDocument(FactoryMapSerializer.Deserialize(malformedDocument, "malformed-smoke"));
            return false;
        }
        catch (InvalidDataException)
        {
            return true;
        }
    }

    private static List<FactoryResourceDepositDefinition> BuildDeposits(IReadOnlyList<FactoryMapDepositEntry> entries)
    {
        var deposits = new List<FactoryResourceDepositDefinition>(entries.Count);
        for (var i = 0; i < entries.Count; i++)
        {
            var entry = entries[i];
            deposits.Add(new FactoryResourceDepositDefinition(
                entry.Id,
                entry.ResourceKind,
                FactoryResourceCatalog.GetDisplayName(entry.ResourceKind),
                FactoryResourceCatalog.GetTint(entry.ResourceKind),
                entry.BuildCells()));
        }

        return deposits;
    }

    private static void LogDocument(string phase, string resourcePath, FactoryMapDocument document)
    {
        GD.Print(
            $"[FactoryMap] {phase} path={resourcePath} kind={document.Kind} bounds=({document.MinCell.X},{document.MinCell.Y})..({document.MaxCell.X},{document.MaxCell.Y}) deposits={document.Deposits.Count} structures={document.Structures.Count} anchors={document.Anchors.Count} markers={document.Markers.Count}");
    }

    private static void ApplyStructureRuntimeState(
        FactoryMapDocument document,
        SimulationController simulation,
        IReadOnlyDictionary<Vector2I, FactoryStructure> structuresByAnchorCell)
    {
        for (var i = 0; i < document.Structures.Count; i++)
        {
            var entry = document.Structures[i];
            if (!structuresByAnchorCell.TryGetValue(entry.Cell, out var structure))
            {
                throw new InvalidDataException(
                    $"Runtime state application could not find structure at ({entry.Cell.X}, {entry.Cell.Y}).");
            }

            if (!string.IsNullOrWhiteSpace(entry.RecipeId) && !structure.TryApplyMapRecipe(entry.RecipeId))
            {
                throw new InvalidDataException(
                    $"Structure '{entry.Kind}' at ({entry.Cell.X}, {entry.Cell.Y}) rejected recipe '{entry.RecipeId}'.");
            }

            for (var seedIndex = 0; seedIndex < entry.SeedItems.Count; seedIndex++)
            {
                var seed = entry.SeedItems[seedIndex];
                for (var count = 0; count < seed.Count; count++)
                {
                    structure.TryAcceptItem(
                        simulation.CreateItem(entry.Kind, seed.ItemKind),
                        structure.GetInputCell(),
                        simulation);
                }
            }
        }
    }
}
