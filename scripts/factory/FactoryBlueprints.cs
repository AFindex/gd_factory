using Godot;
using System;
using System.Collections.Generic;

public enum FactoryBlueprintSiteKind
{
    WorldGrid,
    MobileInterior
}

public sealed class FactoryBlueprintAttachmentRequirement
{
    public FactoryBlueprintAttachmentRequirement(BuildPrototypeKind kind, Vector2I localCell, FacingDirection facing)
    {
        Kind = kind;
        LocalCell = localCell;
        Facing = facing;
    }

    public BuildPrototypeKind Kind { get; }
    public Vector2I LocalCell { get; }
    public FacingDirection Facing { get; }
}

public sealed class FactoryBlueprintStructureEntry
{
    public FactoryBlueprintStructureEntry(
        BuildPrototypeKind kind,
        Vector2I localCell,
        FacingDirection facing,
        IReadOnlyDictionary<string, string>? configuration = null)
    {
        Kind = kind;
        LocalCell = localCell;
        Facing = facing;
        Configuration = configuration is null
            ? new Dictionary<string, string>()
            : new Dictionary<string, string>(configuration);
    }

    public BuildPrototypeKind Kind { get; }
    public Vector2I LocalCell { get; }
    public FacingDirection Facing { get; }
    public IReadOnlyDictionary<string, string> Configuration { get; }
}

public sealed class FactoryBlueprintRecord
{
    public FactoryBlueprintRecord(
        string id,
        string displayName,
        FactoryBlueprintSiteKind sourceSiteKind,
        Vector2I suggestedAnchorCell,
        Vector2I boundsSize,
        IReadOnlyList<FactoryBlueprintStructureEntry> entries,
        IReadOnlyList<FactoryBlueprintAttachmentRequirement>? requiredAttachments = null)
    {
        Id = id;
        DisplayName = displayName;
        SourceSiteKind = sourceSiteKind;
        SuggestedAnchorCell = suggestedAnchorCell;
        BoundsSize = boundsSize;
        Entries = entries;
        RequiredAttachments = requiredAttachments ?? Array.Empty<FactoryBlueprintAttachmentRequirement>();
    }

    public string Id { get; }
    public string DisplayName { get; }
    public FactoryBlueprintSiteKind SourceSiteKind { get; }
    public Vector2I SuggestedAnchorCell { get; }
    public Vector2I BoundsSize { get; }
    public IReadOnlyList<FactoryBlueprintStructureEntry> Entries { get; }
    public IReadOnlyList<FactoryBlueprintAttachmentRequirement> RequiredAttachments { get; }
    public int StructureCount => Entries.Count;

    public string GetSummaryText()
    {
        return $"{GetSiteKindLabel(SourceSiteKind)} | {StructureCount} 件 | {BoundsSize.X}x{BoundsSize.Y}";
    }

    public static string GetSiteKindLabel(FactoryBlueprintSiteKind siteKind)
    {
        return siteKind switch
        {
            FactoryBlueprintSiteKind.MobileInterior => "移动工厂内部",
            _ => "世界沙盒"
        };
    }
}

public sealed class FactoryBlueprintApplyIssue
{
    public FactoryBlueprintApplyIssue(string message, Vector2I? cell = null)
    {
        Message = message;
        Cell = cell;
    }

    public string Message { get; }
    public Vector2I? Cell { get; }
}

public sealed class FactoryBlueprintPlanEntry
{
    public FactoryBlueprintPlanEntry(
        FactoryBlueprintStructureEntry sourceEntry,
        Vector2I targetCell,
        FacingDirection targetFacing,
        bool isValid,
        string? issue)
    {
        SourceEntry = sourceEntry;
        TargetCell = targetCell;
        TargetFacing = targetFacing;
        IsValid = isValid;
        Issue = issue;
    }

    public FactoryBlueprintStructureEntry SourceEntry { get; }
    public Vector2I TargetCell { get; }
    public FacingDirection TargetFacing { get; }
    public bool IsValid { get; }
    public string? Issue { get; }
}

public sealed class FactoryBlueprintApplyPlan
{
    public FactoryBlueprintApplyPlan(
        FactoryBlueprintRecord blueprint,
        FactoryBlueprintSiteKind destinationSiteKind,
        Vector2I anchorCell,
        FacingDirection rotation,
        Vector2I footprintSize,
        IReadOnlyList<FactoryBlueprintPlanEntry> entries,
        IReadOnlyList<FactoryBlueprintApplyIssue> issues)
    {
        Blueprint = blueprint;
        DestinationSiteKind = destinationSiteKind;
        AnchorCell = anchorCell;
        Rotation = rotation;
        FootprintSize = footprintSize;
        Entries = entries;
        Issues = issues;
    }

    public FactoryBlueprintRecord Blueprint { get; }
    public FactoryBlueprintSiteKind DestinationSiteKind { get; }
    public Vector2I AnchorCell { get; }
    public FacingDirection Rotation { get; }
    public Vector2I FootprintSize { get; }
    public IReadOnlyList<FactoryBlueprintPlanEntry> Entries { get; }
    public IReadOnlyList<FactoryBlueprintApplyIssue> Issues { get; }
    public bool IsValid => Issues.Count == 0;

    public string GetIssueSummary()
    {
        if (Issues.Count == 0)
        {
            return "蓝图校验通过，可以应用。";
        }

        var lines = new List<string>();
        for (var index = 0; index < Issues.Count; index++)
        {
            var issue = Issues[index];
            if (issue.Cell is Vector2I cell)
            {
                lines.Add($"- ({cell.X}, {cell.Y}) {issue.Message}");
            }
            else
            {
                lines.Add($"- {issue.Message}");
            }
        }

        return string.Join("\n", lines);
    }
}

public sealed class FactoryBlueprintSiteAdapter
{
    private readonly Func<IEnumerable<FactoryStructure>> _enumerateStructures;
    private readonly Func<FactoryBlueprintStructureEntry, Vector2I, FacingDirection, string?> _validatePlacement;
    private readonly Func<BuildPrototypeKind, Vector2I, FacingDirection, FactoryStructure?> _placeStructure;
    private readonly Func<Vector2I, bool>? _removeStructureAtCell;
    private readonly Func<FactoryStructure, IReadOnlyDictionary<string, string>> _captureConfiguration;
    private readonly Func<FactoryBlueprintRecord, Vector2I> _defaultApplyAnchor;
    private readonly Func<FactoryBlueprintRecord, string?>? _validateCompatibility;

    public FactoryBlueprintSiteAdapter(
        FactoryBlueprintSiteKind siteKind,
        string siteId,
        string displayName,
        Vector2I minCell,
        Vector2I maxCell,
        Func<IEnumerable<FactoryStructure>> enumerateStructures,
        Func<FactoryBlueprintStructureEntry, Vector2I, FacingDirection, string?> validatePlacement,
        Func<BuildPrototypeKind, Vector2I, FacingDirection, FactoryStructure?> placeStructure,
        Func<Vector2I, bool>? removeStructureAtCell = null,
        Func<FactoryStructure, IReadOnlyDictionary<string, string>>? captureConfiguration = null,
        Func<FactoryBlueprintRecord, Vector2I>? defaultApplyAnchor = null,
        Func<FactoryBlueprintRecord, string?>? validateCompatibility = null)
    {
        SiteKind = siteKind;
        SiteId = siteId;
        DisplayName = displayName;
        MinCell = minCell;
        MaxCell = maxCell;
        _enumerateStructures = enumerateStructures;
        _validatePlacement = validatePlacement;
        _placeStructure = placeStructure;
        _removeStructureAtCell = removeStructureAtCell;
        _captureConfiguration = captureConfiguration ?? CaptureStructureConfiguration;
        _defaultApplyAnchor = defaultApplyAnchor ?? (record => record.SuggestedAnchorCell);
        _validateCompatibility = validateCompatibility;
    }

    public FactoryBlueprintSiteKind SiteKind { get; }
    public string SiteId { get; }
    public string DisplayName { get; }
    public Vector2I MinCell { get; }
    public Vector2I MaxCell { get; }

    public IEnumerable<FactoryStructure> EnumerateStructures()
    {
        return _enumerateStructures();
    }

    public string? ValidatePlacement(FactoryBlueprintStructureEntry entry, Vector2I targetCell, FacingDirection targetFacing)
    {
        return _validatePlacement(entry, targetCell, targetFacing);
    }

    public FactoryStructure? PlaceStructure(BuildPrototypeKind kind, Vector2I cell, FacingDirection facing)
    {
        return _placeStructure(kind, cell, facing);
    }

    public bool RemoveStructureAtCell(Vector2I cell)
    {
        return _removeStructureAtCell?.Invoke(cell) ?? false;
    }

    public IReadOnlyDictionary<string, string> CaptureConfiguration(FactoryStructure structure)
    {
        return _captureConfiguration(structure);
    }

    public Vector2I GetDefaultApplyAnchor(FactoryBlueprintRecord record)
    {
        return _defaultApplyAnchor(record);
    }

    public string? ValidateCompatibility(FactoryBlueprintRecord record)
    {
        return _validateCompatibility?.Invoke(record);
    }

    private static IReadOnlyDictionary<string, string> CaptureStructureConfiguration(FactoryStructure structure)
    {
        return structure.CaptureBlueprintConfiguration();
    }
}

public static class FactoryBlueprintCaptureService
{
    public static FactoryBlueprintRecord? CaptureSelection(FactoryBlueprintSiteAdapter site, Rect2I selectionRect, string displayName)
    {
        var structures = GetStructuresInsideRect(site, selectionRect);
        return CreateRecordFromStructures(
            id: Guid.NewGuid().ToString("N"),
            displayName,
            site.SiteKind,
            structures,
            site.CaptureConfiguration);
    }

    public static FactoryBlueprintRecord? CaptureFullSite(FactoryBlueprintSiteAdapter site, string displayName)
    {
        var fullRect = new Rect2I(
            site.MinCell,
            new Vector2I(site.MaxCell.X - site.MinCell.X + 1, site.MaxCell.Y - site.MinCell.Y + 1));
        return CaptureSelection(site, fullRect, displayName);
    }

    public static FactoryBlueprintRecord CreateRecordFromPreset(
        string id,
        string displayName,
        MobileFactoryInteriorPreset preset)
    {
        var structures = new List<FactoryBlueprintStructureEntry>();
        var minCell = new Vector2I(int.MaxValue, int.MaxValue);
        var maxCell = new Vector2I(int.MinValue, int.MinValue);

        for (var index = 0; index < preset.Placements.Count; index++)
        {
            var placement = preset.Placements[index];
            minCell = new Vector2I(Math.Min(minCell.X, placement.Cell.X), Math.Min(minCell.Y, placement.Cell.Y));
            maxCell = new Vector2I(Math.Max(maxCell.X, placement.Cell.X), Math.Max(maxCell.Y, placement.Cell.Y));
        }

        for (var index = 0; index < preset.AttachmentPlacements.Count; index++)
        {
            var placement = preset.AttachmentPlacements[index];
            minCell = new Vector2I(Math.Min(minCell.X, placement.Cell.X), Math.Min(minCell.Y, placement.Cell.Y));
            maxCell = new Vector2I(Math.Max(maxCell.X, placement.Cell.X), Math.Max(maxCell.Y, placement.Cell.Y));
        }

        var requiredAttachments = new List<FactoryBlueprintAttachmentRequirement>();
        for (var index = 0; index < preset.Placements.Count; index++)
        {
            var placement = preset.Placements[index];
            structures.Add(new FactoryBlueprintStructureEntry(
                placement.Kind,
                placement.Cell - minCell,
                placement.Facing));
        }

        for (var index = 0; index < preset.AttachmentPlacements.Count; index++)
        {
            var placement = preset.AttachmentPlacements[index];
            var localCell = placement.Cell - minCell;
            structures.Add(new FactoryBlueprintStructureEntry(
                placement.Kind,
                localCell,
                placement.Facing));
            requiredAttachments.Add(new FactoryBlueprintAttachmentRequirement(placement.Kind, localCell, placement.Facing));
        }

        structures.Sort(CompareEntries);
        return new FactoryBlueprintRecord(
            id,
            displayName,
            FactoryBlueprintSiteKind.MobileInterior,
            minCell,
            new Vector2I(maxCell.X - minCell.X + 1, maxCell.Y - minCell.Y + 1),
            structures,
            requiredAttachments);
    }

    public static FactoryBlueprintRecord CreateRecordFromPlacements(
        string id,
        string displayName,
        FactoryBlueprintSiteKind siteKind,
        IReadOnlyList<FactoryPlacementSpec> placements)
    {
        var minCell = new Vector2I(int.MaxValue, int.MaxValue);
        var maxCell = new Vector2I(int.MinValue, int.MinValue);
        for (var index = 0; index < placements.Count; index++)
        {
            var placement = placements[index];
            minCell = new Vector2I(Math.Min(minCell.X, placement.Cell.X), Math.Min(minCell.Y, placement.Cell.Y));
            maxCell = new Vector2I(Math.Max(maxCell.X, placement.Cell.X), Math.Max(maxCell.Y, placement.Cell.Y));
        }

        var entries = new List<FactoryBlueprintStructureEntry>(placements.Count);
        for (var index = 0; index < placements.Count; index++)
        {
            var placement = placements[index];
            entries.Add(new FactoryBlueprintStructureEntry(
                placement.Kind,
                placement.Cell - minCell,
                placement.Facing));
        }

        entries.Sort(CompareEntries);
        return new FactoryBlueprintRecord(
            id,
            displayName,
            siteKind,
            minCell,
            new Vector2I(maxCell.X - minCell.X + 1, maxCell.Y - minCell.Y + 1),
            entries);
    }

    private static FactoryBlueprintRecord? CreateRecordFromStructures(
        string id,
        string displayName,
        FactoryBlueprintSiteKind siteKind,
        List<FactoryStructure> structures,
        Func<FactoryStructure, IReadOnlyDictionary<string, string>> captureConfiguration)
    {
        if (structures.Count == 0)
        {
            return null;
        }

        var minCell = new Vector2I(int.MaxValue, int.MaxValue);
        var maxCell = new Vector2I(int.MinValue, int.MinValue);
        for (var index = 0; index < structures.Count; index++)
        {
            foreach (var occupiedCell in structures[index].GetOccupiedCells())
            {
                minCell = new Vector2I(Math.Min(minCell.X, occupiedCell.X), Math.Min(minCell.Y, occupiedCell.Y));
                maxCell = new Vector2I(Math.Max(maxCell.X, occupiedCell.X), Math.Max(maxCell.Y, occupiedCell.Y));
            }
        }

        var entries = new List<FactoryBlueprintStructureEntry>(structures.Count);
        var requiredAttachments = new List<FactoryBlueprintAttachmentRequirement>();
        for (var index = 0; index < structures.Count; index++)
        {
            var structure = structures[index];
            var entry = new FactoryBlueprintStructureEntry(
                structure.Kind,
                structure.Cell - minCell,
                structure.Facing,
                captureConfiguration(structure));
            entries.Add(entry);

            if (MobileFactoryBoundaryAttachmentCatalog.IsAttachmentKind(structure.Kind))
            {
                requiredAttachments.Add(new FactoryBlueprintAttachmentRequirement(structure.Kind, entry.LocalCell, entry.Facing));
            }
        }

        entries.Sort(CompareEntries);
        return new FactoryBlueprintRecord(
            id,
            displayName,
            siteKind,
            minCell,
            new Vector2I(maxCell.X - minCell.X + 1, maxCell.Y - minCell.Y + 1),
            entries,
            requiredAttachments);
    }

    private static List<FactoryStructure> GetStructuresInsideRect(FactoryBlueprintSiteAdapter site, Rect2I selectionRect)
    {
        var selected = new List<FactoryStructure>();
        var seen = new HashSet<ulong>();
        foreach (var structure in site.EnumerateStructures())
        {
            foreach (var occupiedCell in structure.GetOccupiedCells())
            {
                if (!selectionRect.HasPoint(occupiedCell))
                {
                    continue;
                }

                if (seen.Add(structure.GetInstanceId()))
                {
                    selected.Add(structure);
                }
                break;
            }
        }

        return selected;
    }

    private static int CompareEntries(FactoryBlueprintStructureEntry a, FactoryBlueprintStructureEntry b)
    {
        var compareY = a.LocalCell.Y.CompareTo(b.LocalCell.Y);
        if (compareY != 0)
        {
            return compareY;
        }

        var compareX = a.LocalCell.X.CompareTo(b.LocalCell.X);
        if (compareX != 0)
        {
            return compareX;
        }

        return a.Kind.CompareTo(b.Kind);
    }
}

public static class FactoryBlueprintPlanner
{
    public static FactoryBlueprintApplyPlan CreatePlan(
        FactoryBlueprintRecord blueprint,
        FactoryBlueprintSiteAdapter site,
        Vector2I anchorCell,
        FacingDirection rotation = FacingDirection.East)
    {
        var planEntries = new List<FactoryBlueprintPlanEntry>(blueprint.Entries.Count);
        var issues = new List<FactoryBlueprintApplyIssue>();
        var transformedBlueprint = CreateTransformedBlueprint(blueprint, rotation);

        if (blueprint.SourceSiteKind != site.SiteKind)
        {
            issues.Add(new FactoryBlueprintApplyIssue(
                $"该蓝图来自{FactoryBlueprintRecord.GetSiteKindLabel(blueprint.SourceSiteKind)}，不能应用到{site.DisplayName}。"));
            return new FactoryBlueprintApplyPlan(
                blueprint,
                site.SiteKind,
                anchorCell,
                rotation,
                transformedBlueprint.BoundsSize,
                planEntries,
                issues);
        }

        if (site.ValidateCompatibility(transformedBlueprint) is string compatibilityIssue)
        {
            issues.Add(new FactoryBlueprintApplyIssue(compatibilityIssue));
        }

        var plannedCells = new HashSet<Vector2I>();
        for (var index = 0; index < transformedBlueprint.Entries.Count; index++)
        {
            var entry = transformedBlueprint.Entries[index];
            var targetCell = anchorCell + entry.LocalCell;
            var targetFacing = entry.Facing;
            string? issue = null;

            if (!plannedCells.Add(targetCell))
            {
                issue = "蓝图内部存在冲突的目标格。";
            }
            else
            {
                issue = site.ValidatePlacement(entry, targetCell, targetFacing);
            }

            var isValid = string.IsNullOrWhiteSpace(issue);
            planEntries.Add(new FactoryBlueprintPlanEntry(entry, targetCell, targetFacing, isValid, issue));
            if (!isValid)
            {
                issues.Add(new FactoryBlueprintApplyIssue(issue!, targetCell));
            }
        }

        return new FactoryBlueprintApplyPlan(
            blueprint,
            site.SiteKind,
            anchorCell,
            rotation,
            transformedBlueprint.BoundsSize,
            planEntries,
            issues);
    }

    public static bool CommitPlan(FactoryBlueprintApplyPlan plan, FactoryBlueprintSiteAdapter site)
    {
        if (!plan.IsValid)
        {
            return false;
        }

        var placedCells = new List<Vector2I>(plan.Entries.Count);
        for (var index = 0; index < plan.Entries.Count; index++)
        {
            var entry = plan.Entries[index];
            var placed = site.PlaceStructure(entry.SourceEntry.Kind, entry.TargetCell, entry.TargetFacing);
            if (placed is null || !placed.ApplyBlueprintConfiguration(entry.SourceEntry.Configuration))
            {
                RollbackPlaced(site, placedCells);
                return false;
            }

            placedCells.Add(entry.TargetCell);
        }

        return true;
    }

    private static void RollbackPlaced(FactoryBlueprintSiteAdapter site, List<Vector2I> placedCells)
    {
        for (var index = placedCells.Count - 1; index >= 0; index--)
        {
            site.RemoveStructureAtCell(placedCells[index]);
        }
    }

    private static FactoryBlueprintRecord CreateTransformedBlueprint(FactoryBlueprintRecord blueprint, FacingDirection rotation)
    {
        if (rotation == FacingDirection.East || blueprint.Entries.Count == 0)
        {
            return blueprint;
        }

        var rotatedEntryLocals = new Vector2I[blueprint.Entries.Count];
        var minLocal = new Vector2I(int.MaxValue, int.MaxValue);
        var maxLocal = new Vector2I(int.MinValue, int.MinValue);
        for (var index = 0; index < blueprint.Entries.Count; index++)
        {
            var rotatedLocal = FactoryDirection.RotateOffset(blueprint.Entries[index].LocalCell, rotation);
            rotatedEntryLocals[index] = rotatedLocal;
            minLocal = new Vector2I(Math.Min(minLocal.X, rotatedLocal.X), Math.Min(minLocal.Y, rotatedLocal.Y));
            maxLocal = new Vector2I(Math.Max(maxLocal.X, rotatedLocal.X), Math.Max(maxLocal.Y, rotatedLocal.Y));
        }

        var transformedEntries = new List<FactoryBlueprintStructureEntry>(blueprint.Entries.Count);
        for (var index = 0; index < blueprint.Entries.Count; index++)
        {
            var entry = blueprint.Entries[index];
            transformedEntries.Add(new FactoryBlueprintStructureEntry(
                entry.Kind,
                rotatedEntryLocals[index] - minLocal,
                FactoryDirection.RotateBy(entry.Facing, rotation),
                entry.Configuration));
        }

        var transformedAttachments = new List<FactoryBlueprintAttachmentRequirement>(blueprint.RequiredAttachments.Count);
        for (var index = 0; index < blueprint.RequiredAttachments.Count; index++)
        {
            var attachment = blueprint.RequiredAttachments[index];
            transformedAttachments.Add(new FactoryBlueprintAttachmentRequirement(
                attachment.Kind,
                FactoryDirection.RotateOffset(attachment.LocalCell, rotation) - minLocal,
                FactoryDirection.RotateBy(attachment.Facing, rotation)));
        }

        return new FactoryBlueprintRecord(
            blueprint.Id,
            blueprint.DisplayName,
            blueprint.SourceSiteKind,
            blueprint.SuggestedAnchorCell,
            new Vector2I(maxLocal.X - minLocal.X + 1, maxLocal.Y - minLocal.Y + 1),
            transformedEntries,
            transformedAttachments);
    }
}

public static class FactoryBlueprintLibrary
{
    private static readonly List<FactoryBlueprintRecord> Records = new();
    private static bool _seeded;

    public static string? ActiveBlueprintId { get; private set; }
    public static event Action? Changed;

    public static IReadOnlyList<FactoryBlueprintRecord> GetAll()
    {
        EnsureSeedBlueprints();
        return Records;
    }

    public static FactoryBlueprintRecord? GetActive()
    {
        EnsureSeedBlueprints();
        return string.IsNullOrWhiteSpace(ActiveBlueprintId)
            ? null
            : FindById(ActiveBlueprintId!);
    }

    public static FactoryBlueprintRecord? FindById(string blueprintId)
    {
        EnsureSeedBlueprints();
        for (var index = 0; index < Records.Count; index++)
        {
            if (Records[index].Id == blueprintId)
            {
                return Records[index];
            }
        }

        return null;
    }

    public static FactoryBlueprintRecord AddOrUpdate(FactoryBlueprintRecord blueprint)
    {
        EnsureSeedBlueprints();
        for (var index = 0; index < Records.Count; index++)
        {
            if (Records[index].Id != blueprint.Id)
            {
                continue;
            }

            Records[index] = blueprint;
            Changed?.Invoke();
            return blueprint;
        }

        Records.Add(blueprint);
        Changed?.Invoke();
        return blueprint;
    }

    public static bool Remove(string blueprintId)
    {
        EnsureSeedBlueprints();
        for (var index = 0; index < Records.Count; index++)
        {
            if (Records[index].Id != blueprintId)
            {
                continue;
            }

            Records.RemoveAt(index);
            if (ActiveBlueprintId == blueprintId)
            {
                ActiveBlueprintId = null;
            }

            Changed?.Invoke();
            return true;
        }

        return false;
    }

    public static bool SelectActive(string blueprintId)
    {
        EnsureSeedBlueprints();
        if (FindById(blueprintId) is null)
        {
            return false;
        }

        ActiveBlueprintId = blueprintId;
        Changed?.Invoke();
        return true;
    }

    public static void ClearActive()
    {
        EnsureSeedBlueprints();
        ActiveBlueprintId = null;
        Changed?.Invoke();
    }

    private static void EnsureSeedBlueprints()
    {
        if (_seeded)
        {
            return;
        }

        _seeded = true;
        AddSeed(FactoryBlueprintCaptureService.CreateRecordFromPlacements(
            "seed-world-storage-output-corridor",
            "仓储输出走廊",
            FactoryBlueprintSiteKind.WorldGrid,
            new[]
            {
                new FactoryPlacementSpec(BuildPrototypeKind.Producer, new Vector2I(-8, 2), FacingDirection.East),
                new FactoryPlacementSpec(BuildPrototypeKind.Belt, new Vector2I(-7, 2), FacingDirection.East),
                new FactoryPlacementSpec(BuildPrototypeKind.Storage, new Vector2I(-6, 2), FacingDirection.East),
                new FactoryPlacementSpec(BuildPrototypeKind.Inserter, new Vector2I(-5, 2), FacingDirection.East),
                new FactoryPlacementSpec(BuildPrototypeKind.Belt, new Vector2I(-4, 2), FacingDirection.East),
                new FactoryPlacementSpec(BuildPrototypeKind.Belt, new Vector2I(-3, 2), FacingDirection.East),
                new FactoryPlacementSpec(BuildPrototypeKind.Belt, new Vector2I(-2, 2), FacingDirection.East),
                new FactoryPlacementSpec(BuildPrototypeKind.Sink, new Vector2I(-1, 2), FacingDirection.East)
            }));

        AddSeed(FactoryBlueprintCaptureService.CreateRecordFromPreset(
            "seed-mobile-focused-demo-layout",
            "双线物流样板蓝图",
            MobileFactoryScenarioLibrary.CreateFocusedDemoPreset()));
    }

    private static void AddSeed(FactoryBlueprintRecord blueprint)
    {
        Records.Add(blueprint);
    }
}
