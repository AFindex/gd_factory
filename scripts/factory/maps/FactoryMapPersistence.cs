using Godot;
using System;
using System.Collections.Generic;
using System.IO;

public sealed class FactoryMapSaveResult
{
    public FactoryMapSaveResult(string resourcePath, FactoryMapDocument document)
    {
        ResourcePath = resourcePath;
        GlobalPath = ProjectSettings.GlobalizePath(resourcePath);
        Document = document;
    }

    public string ResourcePath { get; }
    public string UserPath => ResourcePath;
    public string GlobalPath { get; }
    public FactoryMapDocument Document { get; }
}

public static class FactoryMapPersistence
{
    public static FactoryMapSaveResult SaveWorldMap(string sourcePath, GridManager grid)
    {
        EnsurePersistenceAllowed();
        var savePath = FactoryPersistencePaths.BuildRuntimeMapSavePath(sourcePath, FactoryMapKind.World);
        return SaveWorldMapToPath(sourcePath, savePath, grid);
    }

    public static FactoryMapSaveResult SaveWorldMapToSource(string sourcePath, GridManager grid)
    {
        EnsurePersistenceAllowed();
        return SaveWorldMapToPath(sourcePath, sourcePath, grid);
    }

    public static FactoryMapSaveResult SaveInteriorMap(string sourcePath, MobileFactorySite site, string? profileId = null)
    {
        EnsurePersistenceAllowed();
        var savePath = FactoryPersistencePaths.BuildRuntimeMapSavePath(sourcePath, FactoryMapKind.Interior);
        return SaveInteriorMapToPath(sourcePath, savePath, site, profileId);
    }

    public static FactoryMapSaveResult SaveInteriorMapToSource(string sourcePath, MobileFactorySite site, string? profileId = null)
    {
        EnsurePersistenceAllowed();
        return SaveInteriorMapToPath(sourcePath, sourcePath, site, profileId);
    }

    private static FactoryMapSaveResult SaveWorldMapToPath(string templatePath, string targetPath, GridManager grid)
    {
        var document = CaptureWorldDocument(templatePath, targetPath, grid);
        FactoryMapSerializer.SaveToFile(targetPath, document);
        return new FactoryMapSaveResult(targetPath, document);
    }

    private static FactoryMapSaveResult SaveInteriorMapToPath(string templatePath, string targetPath, MobileFactorySite site, string? profileId)
    {
        var document = CaptureInteriorDocument(templatePath, targetPath, site, profileId);
        FactoryMapSerializer.SaveToFile(targetPath, document);
        return new FactoryMapSaveResult(targetPath, document);
    }

    private static FactoryMapDocument CaptureWorldDocument(string templatePath, string targetPath, GridManager grid)
    {
        var template = TryLoadTemplate(templatePath, FactoryMapKind.World);
        var document = CreateBaseDocument(templatePath, targetPath, FactoryMapKind.World, grid.MinCell, grid.MaxCell, template);
        var deposits = grid.GetResourceDeposits();
        for (var index = 0; index < deposits.Count; index++)
        {
            document.Deposits.Add(BuildDepositEntry(deposits[index]));
        }

        CaptureStructures(document.Structures, grid.GetStructures());
        return FactoryMapValidator.ValidateDocument(document);
    }

    private static FactoryMapDocument CaptureInteriorDocument(string templatePath, string targetPath, MobileFactorySite site, string? profileId)
    {
        var template = TryLoadTemplate(templatePath, FactoryMapKind.Interior);
        var document = CreateBaseDocument(templatePath, targetPath, FactoryMapKind.Interior, site.MinCell, site.MaxCell, template);
        if (!string.IsNullOrWhiteSpace(profileId))
        {
            document.Metadata["profile_id"] = profileId!;
        }

        CaptureStructures(document.Structures, site.GetStructures());
        return FactoryMapValidator.ValidateDocument(document);
    }

    private static FactoryMapDocument CreateBaseDocument(
        string templatePath,
        string targetPath,
        FactoryMapKind kind,
        Vector2I minCell,
        Vector2I maxCell,
        FactoryMapDocument? template)
    {
        var metadata = template is null
            ? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            : new Dictionary<string, string>(template.Metadata, StringComparer.OrdinalIgnoreCase);
        if (FactoryPersistencePaths.IsRuntimePersistencePath(targetPath))
        {
            var runtimeSourcePath = metadata.TryGetValue("runtime_saved_from", out var existingSourcePath) && !string.IsNullOrWhiteSpace(existingSourcePath)
                ? existingSourcePath
                : templatePath;
            metadata["runtime_saved_from"] = runtimeSourcePath;
            metadata["runtime_saved_at_utc"] = DateTime.UtcNow.ToString("O", System.Globalization.CultureInfo.InvariantCulture);
        }
        else
        {
            metadata.Remove("runtime_saved_from");
            metadata.Remove("runtime_saved_at_utc");
        }

        return new FactoryMapDocument(
            FactoryMapSerializer.SupportedVersion,
            kind,
            minCell,
            maxCell,
            metadata,
            anchors: template?.Anchors,
            markers: template?.Markers);
    }

    private static FactoryMapDocument? TryLoadTemplate(string sourcePath, FactoryMapKind expectedKind)
    {
        try
        {
            var document = FactoryMapValidator.ValidateDocument(FactoryMapSerializer.LoadFromFile(sourcePath));
            return document.Kind == expectedKind ? document : null;
        }
        catch
        {
            return null;
        }
    }

    private static FactoryMapDepositEntry BuildDepositEntry(FactoryResourceDepositDefinition deposit)
    {
        var minX = int.MaxValue;
        var minY = int.MaxValue;
        var maxX = int.MinValue;
        var maxY = int.MinValue;
        for (var index = 0; index < deposit.Cells.Count; index++)
        {
            var cell = deposit.Cells[index];
            minX = Math.Min(minX, cell.X);
            minY = Math.Min(minY, cell.Y);
            maxX = Math.Max(maxX, cell.X);
            maxY = Math.Max(maxY, cell.Y);
        }

        return new FactoryMapDepositEntry(
            deposit.Id,
            deposit.ResourceKind,
            new Vector2I(minX, minY),
            new Vector2I(maxX - minX + 1, maxY - minY + 1));
    }

    private static void CaptureStructures(List<FactoryMapStructureEntry> destination, IEnumerable<FactoryStructure> structures)
    {
        var captured = new List<FactoryMapStructureEntry>();
        foreach (var structure in structures)
        {
            var entry = new FactoryMapStructureEntry(structure.Kind, structure.Cell, structure.Facing);
            var recipeId = structure.CaptureMapRecipeId();
            if (!string.IsNullOrWhiteSpace(recipeId))
            {
                entry.SetRecipe(recipeId!);
            }

            var seedItems = structure.CaptureMapSeedItems();
            for (var index = 0; index < seedItems.Count; index++)
            {
                if (seedItems[index].Count > 0)
                {
                    entry.AddSeedItem(seedItems[index]);
                }
            }

            captured.Add(entry);
        }

        captured.Sort(CompareStructureEntries);
        destination.AddRange(captured);
    }

    private static int CompareStructureEntries(FactoryMapStructureEntry left, FactoryMapStructureEntry right)
    {
        var compareY = left.Cell.Y.CompareTo(right.Cell.Y);
        if (compareY != 0)
        {
            return compareY;
        }

        var compareX = left.Cell.X.CompareTo(right.Cell.X);
        if (compareX != 0)
        {
            return compareX;
        }

        return left.Kind.CompareTo(right.Kind);
    }

    private static void EnsurePersistenceAllowed()
    {
        if (!FactoryPersistencePaths.IsPersistenceEnabled())
        {
            throw new InvalidOperationException("Runtime persistence is disabled while smoke tests or validation commands are running.");
        }
    }
}
