using Godot;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

public enum FactoryMapKind
{
    World,
    Interior
}

public sealed class FactoryMapSeedItemEntry
{
    public FactoryMapSeedItemEntry(FactoryItemKind itemKind, int count)
    {
        ItemKind = itemKind;
        Count = count;
    }

    public FactoryItemKind ItemKind { get; }
    public int Count { get; }
}

public sealed class FactoryMapDepositEntry
{
    public FactoryMapDepositEntry(string id, FactoryResourceKind resourceKind, Vector2I originCell, Vector2I size)
    {
        Id = id;
        ResourceKind = resourceKind;
        OriginCell = originCell;
        Size = size;
    }

    public string Id { get; }
    public FactoryResourceKind ResourceKind { get; }
    public Vector2I OriginCell { get; }
    public Vector2I Size { get; }

    public IReadOnlyList<Vector2I> BuildCells()
    {
        var cells = new List<Vector2I>(Size.X * Size.Y);
        for (var y = 0; y < Size.Y; y++)
        {
            for (var x = 0; x < Size.X; x++)
            {
                cells.Add(OriginCell + new Vector2I(x, y));
            }
        }

        return cells;
    }
}

public sealed class FactoryMapAnchorEntry
{
    public FactoryMapAnchorEntry(string id, Vector2I cell)
    {
        Id = id;
        Cell = cell;
    }

    public string Id { get; }
    public Vector2I Cell { get; }
}

public sealed class FactoryMapMarkerEntry
{
    public FactoryMapMarkerEntry(string id, string markerType, Vector2I cell)
    {
        Id = id;
        MarkerType = markerType;
        Cell = cell;
    }

    public string Id { get; }
    public string MarkerType { get; }
    public Vector2I Cell { get; }
}

public sealed class FactoryMapStructureEntry
{
    private readonly List<FactoryMapSeedItemEntry> _seedItems = new();

    public FactoryMapStructureEntry(BuildPrototypeKind kind, Vector2I cell, FacingDirection facing)
    {
        Kind = kind;
        Cell = cell;
        Facing = facing;
    }

    public BuildPrototypeKind Kind { get; }
    public Vector2I Cell { get; }
    public FacingDirection Facing { get; }
    public string? RecipeId { get; private set; }
    public IReadOnlyList<FactoryMapSeedItemEntry> SeedItems => _seedItems;

    public void SetRecipe(string recipeId)
    {
        RecipeId = recipeId;
    }

    public void AddSeedItem(FactoryMapSeedItemEntry entry)
    {
        _seedItems.Add(entry);
    }
}

public sealed class FactoryMapDocument
{
    public FactoryMapDocument(
        int version,
        FactoryMapKind kind,
        Vector2I minCell,
        Vector2I maxCell,
        IReadOnlyDictionary<string, string>? metadata = null,
        IReadOnlyList<FactoryMapDepositEntry>? deposits = null,
        IReadOnlyList<FactoryMapStructureEntry>? structures = null,
        IReadOnlyList<FactoryMapAnchorEntry>? anchors = null,
        IReadOnlyList<FactoryMapMarkerEntry>? markers = null)
    {
        Version = version;
        Kind = kind;
        MinCell = minCell;
        MaxCell = maxCell;
        Metadata = metadata is null
            ? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            : new Dictionary<string, string>(metadata, StringComparer.OrdinalIgnoreCase);
        Deposits = deposits is null ? new List<FactoryMapDepositEntry>() : new List<FactoryMapDepositEntry>(deposits);
        Structures = structures is null ? new List<FactoryMapStructureEntry>() : new List<FactoryMapStructureEntry>(structures);
        Anchors = anchors is null ? new List<FactoryMapAnchorEntry>() : new List<FactoryMapAnchorEntry>(anchors);
        Markers = markers is null ? new List<FactoryMapMarkerEntry>() : new List<FactoryMapMarkerEntry>(markers);
    }

    public int Version { get; }
    public FactoryMapKind Kind { get; }
    public Vector2I MinCell { get; }
    public Vector2I MaxCell { get; }
    public Dictionary<string, string> Metadata { get; }
    public List<FactoryMapDepositEntry> Deposits { get; }
    public List<FactoryMapStructureEntry> Structures { get; }
    public List<FactoryMapAnchorEntry> Anchors { get; }
    public List<FactoryMapMarkerEntry> Markers { get; }

    public bool TryGetStructure(Vector2I cell, out FactoryMapStructureEntry? entry)
    {
        for (var i = 0; i < Structures.Count; i++)
        {
            if (Structures[i].Cell == cell)
            {
                entry = Structures[i];
                return true;
            }
        }

        entry = null;
        return false;
    }
}

public static class FactoryMapPaths
{
    public const string StaticSandboxWorld = "res://data/factory/maps/static-sandbox-world.nfmap";
    public const string FocusedMobileWorld = "res://data/factory/maps/mobile-focused-world.nfmap";
    public const string FocusedMobileInterior = "res://data/factory/maps/mobile-focused-interior.nfmap";
    public const string DualStandardsMobileWorld = "res://data/factory/maps/mobile-dual-standards-world.nfmap";
    public const string DualStandardsMobileInterior = "res://data/factory/maps/mobile-dual-standards-interior.nfmap";
}

public static class FactoryMapSerializer
{
    public const int SupportedVersion = 1;

    public static FactoryMapDocument LoadFromFile(string resourcePath)
    {
        using var file = Godot.FileAccess.Open(resourcePath, Godot.FileAccess.ModeFlags.Read);
        if (file is null)
        {
            throw new InvalidDataException($"Unable to open factory map file '{resourcePath}'.");
        }

        return Deserialize(file.GetAsText(), resourcePath);
    }

    public static void SaveToFile(string resourcePath, FactoryMapDocument document)
    {
        var globalPath = ProjectSettings.GlobalizePath(resourcePath);
        var directoryPath = Path.GetDirectoryName(globalPath);
        if (!string.IsNullOrWhiteSpace(directoryPath))
        {
            Directory.CreateDirectory(directoryPath);
        }

        using var file = Godot.FileAccess.Open(resourcePath, Godot.FileAccess.ModeFlags.Write);
        if (file is null)
        {
            throw new InvalidDataException($"Unable to write factory map file '{resourcePath}'.");
        }

        file.StoreString(Serialize(document));
    }

    public static string Serialize(FactoryMapDocument document)
    {
        var builder = new StringBuilder();
        builder.AppendLine($"version|{document.Version}");
        builder.AppendLine($"kind|{document.Kind}");
        builder.AppendLine($"bounds|{document.MinCell.X}|{document.MinCell.Y}|{document.MaxCell.X}|{document.MaxCell.Y}");

        foreach (var key in new SortedSet<string>(document.Metadata.Keys, StringComparer.OrdinalIgnoreCase))
        {
            builder.AppendLine($"meta|{key}|{document.Metadata[key]}");
        }

        for (var i = 0; i < document.Anchors.Count; i++)
        {
            var anchor = document.Anchors[i];
            builder.AppendLine($"anchor|{anchor.Id}|{anchor.Cell.X}|{anchor.Cell.Y}");
        }

        for (var i = 0; i < document.Markers.Count; i++)
        {
            var marker = document.Markers[i];
            builder.AppendLine($"marker|{marker.Id}|{marker.MarkerType}|{marker.Cell.X}|{marker.Cell.Y}");
        }

        for (var i = 0; i < document.Deposits.Count; i++)
        {
            var deposit = document.Deposits[i];
            builder.AppendLine(
                $"deposit|{deposit.Id}|{deposit.ResourceKind}|{deposit.OriginCell.X}|{deposit.OriginCell.Y}|{deposit.Size.X}|{deposit.Size.Y}");
        }

        for (var i = 0; i < document.Structures.Count; i++)
        {
            var structure = document.Structures[i];
            builder.AppendLine($"structure|{structure.Kind}|{structure.Cell.X}|{structure.Cell.Y}|{structure.Facing}");
            if (!string.IsNullOrWhiteSpace(structure.RecipeId))
            {
                builder.AppendLine($"recipe|{structure.Cell.X}|{structure.Cell.Y}|{structure.RecipeId}");
            }

            for (var seedIndex = 0; seedIndex < structure.SeedItems.Count; seedIndex++)
            {
                var seed = structure.SeedItems[seedIndex];
                builder.AppendLine($"seed|{structure.Cell.X}|{structure.Cell.Y}|{seed.ItemKind}|{seed.Count}");
            }
        }

        return builder.ToString().TrimEnd() + "\n";
    }

    public static FactoryMapDocument Deserialize(string text, string sourceName = "<memory>")
    {
        var version = 0;
        var hasVersion = false;
        var hasKind = false;
        var hasBounds = false;
        var kind = FactoryMapKind.World;
        var minCell = Vector2I.Zero;
        var maxCell = Vector2I.Zero;
        var metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var anchors = new List<FactoryMapAnchorEntry>();
        var markers = new List<FactoryMapMarkerEntry>();
        var deposits = new List<FactoryMapDepositEntry>();
        var structures = new List<FactoryMapStructureEntry>();
        var structureByCell = new Dictionary<Vector2I, FactoryMapStructureEntry>();

        var lines = text.Replace("\r\n", "\n").Split('\n');
        for (var lineIndex = 0; lineIndex < lines.Length; lineIndex++)
        {
            var rawLine = lines[lineIndex].Trim();
            if (string.IsNullOrWhiteSpace(rawLine) || rawLine.StartsWith('#'))
            {
                continue;
            }

            var tokens = rawLine.Split('|');
            if (tokens.Length == 0)
            {
                continue;
            }

            switch (tokens[0])
            {
                case "version":
                    EnsureTokenCount(tokens, 2, sourceName, lineIndex);
                    version = ParseInt(tokens[1], "version", sourceName, lineIndex);
                    hasVersion = true;
                    break;
                case "kind":
                    EnsureTokenCount(tokens, 2, sourceName, lineIndex);
                    kind = ParseEnum<FactoryMapKind>(tokens[1], "kind", sourceName, lineIndex);
                    hasKind = true;
                    break;
                case "bounds":
                    EnsureTokenCount(tokens, 5, sourceName, lineIndex);
                    minCell = new Vector2I(
                        ParseInt(tokens[1], "min-x", sourceName, lineIndex),
                        ParseInt(tokens[2], "min-y", sourceName, lineIndex));
                    maxCell = new Vector2I(
                        ParseInt(tokens[3], "max-x", sourceName, lineIndex),
                        ParseInt(tokens[4], "max-y", sourceName, lineIndex));
                    hasBounds = true;
                    break;
                case "meta":
                    EnsureTokenCount(tokens, 3, sourceName, lineIndex);
                    metadata[tokens[1]] = string.Join("|", tokens, 2, tokens.Length - 2);
                    break;
                case "anchor":
                    EnsureTokenCount(tokens, 4, sourceName, lineIndex);
                    anchors.Add(new FactoryMapAnchorEntry(
                        tokens[1],
                        new Vector2I(
                            ParseInt(tokens[2], "anchor-x", sourceName, lineIndex),
                            ParseInt(tokens[3], "anchor-y", sourceName, lineIndex))));
                    break;
                case "marker":
                    EnsureTokenCount(tokens, 5, sourceName, lineIndex);
                    markers.Add(new FactoryMapMarkerEntry(
                        tokens[1],
                        tokens[2],
                        new Vector2I(
                            ParseInt(tokens[3], "marker-x", sourceName, lineIndex),
                            ParseInt(tokens[4], "marker-y", sourceName, lineIndex))));
                    break;
                case "deposit":
                    EnsureTokenCount(tokens, 7, sourceName, lineIndex);
                    deposits.Add(new FactoryMapDepositEntry(
                        tokens[1],
                        ParseEnum<FactoryResourceKind>(tokens[2], "resource-kind", sourceName, lineIndex),
                        new Vector2I(
                            ParseInt(tokens[3], "deposit-origin-x", sourceName, lineIndex),
                            ParseInt(tokens[4], "deposit-origin-y", sourceName, lineIndex)),
                        new Vector2I(
                            ParseInt(tokens[5], "deposit-width", sourceName, lineIndex),
                            ParseInt(tokens[6], "deposit-height", sourceName, lineIndex))));
                    break;
                case "structure":
                    EnsureTokenCount(tokens, 5, sourceName, lineIndex);
                    var structure = new FactoryMapStructureEntry(
                        ParseEnum<BuildPrototypeKind>(tokens[1], "structure-kind", sourceName, lineIndex),
                        new Vector2I(
                            ParseInt(tokens[2], "structure-x", sourceName, lineIndex),
                            ParseInt(tokens[3], "structure-y", sourceName, lineIndex)),
                        ParseEnum<FacingDirection>(tokens[4], "facing", sourceName, lineIndex));
                    structures.Add(structure);
                    structureByCell[structure.Cell] = structure;
                    break;
                case "recipe":
                    EnsureTokenCount(tokens, 4, sourceName, lineIndex);
                    ApplyStructureRecipe(structureByCell, tokens, sourceName, lineIndex);
                    break;
                case "seed":
                    EnsureTokenCount(tokens, 5, sourceName, lineIndex);
                    ApplyStructureSeed(structureByCell, tokens, sourceName, lineIndex);
                    break;
                default:
                    throw new InvalidDataException($"{sourceName}:{lineIndex + 1} Unsupported map directive '{tokens[0]}'.");
            }
        }

        if (!hasVersion)
        {
            throw new InvalidDataException($"{sourceName} Missing required 'version' directive.");
        }

        if (!hasKind)
        {
            throw new InvalidDataException($"{sourceName} Missing required 'kind' directive.");
        }

        if (!hasBounds)
        {
            throw new InvalidDataException($"{sourceName} Missing required 'bounds' directive.");
        }

        return new FactoryMapDocument(version, kind, minCell, maxCell, metadata, deposits, structures, anchors, markers);
    }

    private static void ApplyStructureRecipe(
        Dictionary<Vector2I, FactoryMapStructureEntry> structureByCell,
        string[] tokens,
        string sourceName,
        int lineIndex)
    {
        var cell = new Vector2I(
            ParseInt(tokens[1], "recipe-x", sourceName, lineIndex),
            ParseInt(tokens[2], "recipe-y", sourceName, lineIndex));
        if (!structureByCell.TryGetValue(cell, out var structure))
        {
            throw new InvalidDataException($"{sourceName}:{lineIndex + 1} Recipe references missing structure at ({cell.X}, {cell.Y}).");
        }

        structure.SetRecipe(tokens[3]);
    }

    private static void ApplyStructureSeed(
        Dictionary<Vector2I, FactoryMapStructureEntry> structureByCell,
        string[] tokens,
        string sourceName,
        int lineIndex)
    {
        var cell = new Vector2I(
            ParseInt(tokens[1], "seed-x", sourceName, lineIndex),
            ParseInt(tokens[2], "seed-y", sourceName, lineIndex));
        if (!structureByCell.TryGetValue(cell, out var structure))
        {
            throw new InvalidDataException($"{sourceName}:{lineIndex + 1} Seed references missing structure at ({cell.X}, {cell.Y}).");
        }

        structure.AddSeedItem(new FactoryMapSeedItemEntry(
            ParseEnum<FactoryItemKind>(tokens[3], "seed-item", sourceName, lineIndex),
            ParseInt(tokens[4], "seed-count", sourceName, lineIndex)));
    }

    private static void EnsureTokenCount(string[] tokens, int expectedCount, string sourceName, int lineIndex)
    {
        if (tokens.Length < expectedCount)
        {
            throw new InvalidDataException(
                $"{sourceName}:{lineIndex + 1} Expected at least {expectedCount} fields for '{tokens[0]}', got {tokens.Length}.");
        }
    }

    private static int ParseInt(string token, string fieldName, string sourceName, int lineIndex)
    {
        if (!int.TryParse(token, out var value))
        {
            throw new InvalidDataException($"{sourceName}:{lineIndex + 1} Invalid integer '{token}' for {fieldName}.");
        }

        return value;
    }

    private static TEnum ParseEnum<TEnum>(string token, string fieldName, string sourceName, int lineIndex) where TEnum : struct
    {
        if (!Enum.TryParse<TEnum>(token, ignoreCase: false, out var value))
        {
            throw new InvalidDataException($"{sourceName}:{lineIndex + 1} Invalid {fieldName} value '{token}'.");
        }

        return value;
    }
}

public static class FactoryMapValidator
{
    public static FactoryMapDocument ValidateDocument(FactoryMapDocument document)
    {
        if (document.Version != FactoryMapSerializer.SupportedVersion)
        {
            throw new InvalidDataException($"Unsupported factory map version '{document.Version}'.");
        }

        if (document.MinCell.X > document.MaxCell.X || document.MinCell.Y > document.MaxCell.Y)
        {
            throw new InvalidDataException("Factory map bounds are inverted.");
        }

        ValidateAnchorsAndMarkers(document);
        ValidateDeposits(document);
        ValidateStructures(document);
        return document;
    }

    public static void ValidateAgainstSiteBounds(FactoryMapDocument document, Vector2I minCell, Vector2I maxCell, FactoryMapKind expectedKind)
    {
        if (document.Kind != expectedKind)
        {
            throw new InvalidDataException($"Expected a {expectedKind} map but got {document.Kind}.");
        }

        if (document.MinCell.X < minCell.X
            || document.MinCell.Y < minCell.Y
            || document.MaxCell.X > maxCell.X
            || document.MaxCell.Y > maxCell.Y)
        {
            throw new InvalidDataException(
                $"Map bounds ({document.MinCell.X}, {document.MinCell.Y})..({document.MaxCell.X}, {document.MaxCell.Y}) exceed site bounds ({minCell.X}, {minCell.Y})..({maxCell.X}, {maxCell.Y}).");
        }
    }

    private static void ValidateAnchorsAndMarkers(FactoryMapDocument document)
    {
        var anchorIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < document.Anchors.Count; i++)
        {
            var anchor = document.Anchors[i];
            if (string.IsNullOrWhiteSpace(anchor.Id))
            {
                throw new InvalidDataException("Anchor ids must not be empty.");
            }

            if (!anchorIds.Add(anchor.Id))
            {
                throw new InvalidDataException($"Duplicate anchor id '{anchor.Id}'.");
            }

            EnsureCellInBounds(document, anchor.Cell, $"anchor '{anchor.Id}'");
        }

        var markerIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < document.Markers.Count; i++)
        {
            var marker = document.Markers[i];
            if (string.IsNullOrWhiteSpace(marker.Id))
            {
                throw new InvalidDataException("Marker ids must not be empty.");
            }

            if (!markerIds.Add(marker.Id))
            {
                throw new InvalidDataException($"Duplicate marker id '{marker.Id}'.");
            }

            EnsureCellInBounds(document, marker.Cell, $"marker '{marker.Id}'");
        }
    }

    private static void ValidateDeposits(FactoryMapDocument document)
    {
        if (document.Kind == FactoryMapKind.Interior && document.Deposits.Count > 0)
        {
            throw new InvalidDataException("Interior maps cannot declare world resource deposits.");
        }

        var depositIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var occupiedDepositCells = new HashSet<Vector2I>();
        for (var i = 0; i < document.Deposits.Count; i++)
        {
            var deposit = document.Deposits[i];
            if (string.IsNullOrWhiteSpace(deposit.Id))
            {
                throw new InvalidDataException("Deposit ids must not be empty.");
            }

            if (!depositIds.Add(deposit.Id))
            {
                throw new InvalidDataException($"Duplicate deposit id '{deposit.Id}'.");
            }

            if (deposit.Size.X <= 0 || deposit.Size.Y <= 0)
            {
                throw new InvalidDataException($"Deposit '{deposit.Id}' has invalid size {deposit.Size}.");
            }

            var cells = deposit.BuildCells();
            for (var cellIndex = 0; cellIndex < cells.Count; cellIndex++)
            {
                var cell = cells[cellIndex];
                EnsureCellInBounds(document, cell, $"deposit '{deposit.Id}'");
                if (!occupiedDepositCells.Add(cell))
                {
                    throw new InvalidDataException($"Deposit '{deposit.Id}' overlaps another deposit at ({cell.X}, {cell.Y}).");
                }
            }
        }
    }

    private static void ValidateStructures(FactoryMapDocument document)
    {
        var depositCells = new Dictionary<Vector2I, FactoryMapDepositEntry>();
        for (var i = 0; i < document.Deposits.Count; i++)
        {
            var deposit = document.Deposits[i];
            var cells = deposit.BuildCells();
            for (var cellIndex = 0; cellIndex < cells.Count; cellIndex++)
            {
                depositCells[cells[cellIndex]] = deposit;
            }
        }

        var occupiedCells = new Dictionary<Vector2I, FactoryMapStructureEntry>();
        var primaryCells = new HashSet<Vector2I>();
        for (var i = 0; i < document.Structures.Count; i++)
        {
            var structure = document.Structures[i];
            if (!FactoryStructureFactory.TryGetDefinition(structure.Kind, out var definition) || definition is null)
            {
                throw new InvalidDataException($"Unsupported structure kind '{structure.Kind}'.");
            }

            var siteKind = document.Kind == FactoryMapKind.Interior
                ? FactorySiteKind.Interior
                : FactorySiteKind.World;
            if (!FactoryIndustrialStandards.IsStructureAllowed(structure.Kind, siteKind))
            {
                throw new InvalidDataException(FactoryIndustrialStandards.GetPlacementCompatibilityError(structure.Kind, siteKind));
            }

            if (!primaryCells.Add(structure.Cell))
            {
                throw new InvalidDataException($"Duplicate structure anchor at ({structure.Cell.X}, {structure.Cell.Y}).");
            }

            var footprintCells = FactoryPlacement.ResolveFootprintCells(
                structure.Kind,
                structure.Cell,
                structure.Facing,
                configuration: null,
                mapRecipeId: structure.RecipeId);
            var matchedDeposit = false;
            for (var cellIndex = 0; cellIndex < footprintCells.Count; cellIndex++)
            {
                var cell = footprintCells[cellIndex];
                EnsureCellInBounds(document, cell, $"{structure.Kind} at ({structure.Cell.X}, {structure.Cell.Y})");
                if (occupiedCells.ContainsKey(cell))
                {
                    throw new InvalidDataException($"Structure '{structure.Kind}' overlaps another structure at ({cell.X}, {cell.Y}).");
                }

                occupiedCells[cell] = structure;

                if (document.Kind != FactoryMapKind.World)
                {
                    continue;
                }

                if (!depositCells.TryGetValue(cell, out var deposit))
                {
                    continue;
                }

                if (structure.Kind != BuildPrototypeKind.MiningDrill)
                {
                    throw new InvalidDataException(
                        $"Structure '{structure.Kind}' cannot occupy resource deposit cell ({cell.X}, {cell.Y}) from '{deposit.Id}'.");
                }

                if (!FactoryResourceCatalog.SupportsExtractor(structure.Kind, deposit.ResourceKind))
                {
                    throw new InvalidDataException(
                        $"Structure '{structure.Kind}' cannot extract resource '{deposit.ResourceKind}' at ({cell.X}, {cell.Y}).");
                }

                matchedDeposit = true;
            }

            if (document.Kind == FactoryMapKind.World
                && structure.Kind == BuildPrototypeKind.MiningDrill
                && !matchedDeposit)
            {
                throw new InvalidDataException(
                    $"Mining drill at ({structure.Cell.X}, {structure.Cell.Y}) does not overlap any valid deposit cell.");
            }

            if (structure.SeedItems.Count == 0)
            {
                continue;
            }

            for (var seedIndex = 0; seedIndex < structure.SeedItems.Count; seedIndex++)
            {
                if (structure.SeedItems[seedIndex].Count <= 0)
                {
                    throw new InvalidDataException(
                        $"Structure '{structure.Kind}' at ({structure.Cell.X}, {structure.Cell.Y}) has a non-positive seed count.");
                }
            }
        }
    }

    private static void EnsureCellInBounds(FactoryMapDocument document, Vector2I cell, string label)
    {
        if (cell.X < document.MinCell.X
            || cell.X > document.MaxCell.X
            || cell.Y < document.MinCell.Y
            || cell.Y > document.MaxCell.Y)
        {
            throw new InvalidDataException(
                $"{label} references out-of-bounds cell ({cell.X}, {cell.Y}) for bounds ({document.MinCell.X}, {document.MinCell.Y})..({document.MaxCell.X}, {document.MaxCell.Y}).");
        }
    }
}
