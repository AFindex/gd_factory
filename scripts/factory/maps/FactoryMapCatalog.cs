using Godot;
using System;
using System.Collections.Generic;
using System.IO;

public enum FactoryMapCatalogSource
{
    Bundled,
    RuntimeSaved
}

public sealed class FactoryMapCatalogEntry
{
    public FactoryMapCatalogEntry(
        string path,
        string displayName,
        FactoryMapKind kind,
        FactoryMapCatalogSource source,
        string sourceLabel,
        string? detail = null)
    {
        Path = path;
        DisplayName = displayName;
        Kind = kind;
        Source = source;
        SourceLabel = sourceLabel;
        Detail = detail ?? string.Empty;
    }

    public string Path { get; }
    public string DisplayName { get; }
    public FactoryMapKind Kind { get; }
    public FactoryMapCatalogSource Source { get; }
    public string SourceLabel { get; }
    public string Detail { get; }

    public string BuildOptionText()
    {
        return $"{DisplayName} [{SourceLabel}]";
    }

    public string BuildSummaryText()
    {
        return string.IsNullOrWhiteSpace(Detail)
            ? $"{BuildOptionText()}\n{Path}"
            : $"{BuildOptionText()}\n{Detail}\n{Path}";
    }
}

public static class FactoryMapCatalog
{
    public static IReadOnlyList<FactoryMapCatalogEntry> GetWorldMaps()
    {
        var result = new List<FactoryMapCatalogEntry>();
        AddBundledEntry(result, FactoryMapPaths.StaticSandboxWorld, "Static Sandbox");
        AddBundledEntry(result, FactoryMapPaths.FocusedMobileWorld, "Mobile Focused World");
        AddRuntimeEntries(result, FactoryPersistencePaths.WorldMapDirectory);
        SortEntries(result);
        return result;
    }

    public static IReadOnlyList<FactoryMapCatalogEntry> GetInteriorMaps()
    {
        var result = new List<FactoryMapCatalogEntry>();
        AddBundledEntry(result, FactoryMapPaths.FocusedMobileInterior, "Mobile Focused Interior");
        AddRuntimeEntries(result, FactoryPersistencePaths.InteriorMapDirectory);
        SortEntries(result);
        return result;
    }

    private static void AddBundledEntry(List<FactoryMapCatalogEntry> destination, string path, string fallbackName)
    {
        if (TryLoadEntry(path, FactoryMapCatalogSource.Bundled, out var entry, fallbackName))
        {
            destination.Add(entry);
        }
    }

    private static void AddRuntimeEntries(List<FactoryMapCatalogEntry> destination, string directoryPath)
    {
        if (!FactoryPersistencePaths.IsPersistenceEnabled())
        {
            return;
        }

        var globalPath = ProjectSettings.GlobalizePath(directoryPath);
        if (!Directory.Exists(globalPath))
        {
            return;
        }

        var files = Directory.GetFiles(globalPath, "*.nfmap", SearchOption.TopDirectoryOnly);
        Array.Sort(files, StringComparer.OrdinalIgnoreCase);
        for (var index = 0; index < files.Length; index++)
        {
            var globalFilePath = files[index];
            var userPath = GlobalPathToUserPath(globalFilePath);
            if (string.IsNullOrWhiteSpace(userPath))
            {
                continue;
            }

            if (TryLoadEntry(userPath, FactoryMapCatalogSource.RuntimeSaved, out var entry, null))
            {
                destination.Add(entry);
            }
        }
    }

    private static bool TryLoadEntry(
        string path,
        FactoryMapCatalogSource source,
        out FactoryMapCatalogEntry entry,
        string? fallbackName)
    {
        try
        {
            var document = FactoryMapValidator.ValidateDocument(FactoryMapSerializer.LoadFromFile(path));
            var runtimeSource = document.Metadata.TryGetValue("runtime_saved_from", out var sourcePath)
                ? sourcePath
                : null;
            var displayName = !string.IsNullOrWhiteSpace(fallbackName)
                ? fallbackName!
                : BuildDisplayName(path, runtimeSource);
            var detail = runtimeSource is null ? null : $"来自 {Path.GetFileNameWithoutExtension(runtimeSource)}";
            entry = new FactoryMapCatalogEntry(
                path,
                displayName,
                document.Kind,
                source,
                source == FactoryMapCatalogSource.Bundled ? "工程内" : "运行时",
                detail);
            return true;
        }
        catch (Exception ex)
        {
            GD.PushWarning($"Skipping map catalog entry '{path}': {ex.Message}");
            entry = null!;
            return false;
        }
    }

    private static string BuildDisplayName(string path, string? runtimeSource)
    {
        var stem = Path.GetFileNameWithoutExtension(path);
        if (!string.IsNullOrWhiteSpace(runtimeSource))
        {
            return $"{HumanizeStem(stem)}";
        }

        return HumanizeStem(stem);
    }

    private static string HumanizeStem(string stem)
    {
        if (string.IsNullOrWhiteSpace(stem))
        {
            return "未命名地图";
        }

        return stem
            .Replace("-runtime", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace('-', ' ')
            .Replace('_', ' ')
            .Trim();
    }

    private static string? GlobalPathToUserPath(string globalPath)
    {
        var persistenceRoot = FactoryPersistencePaths.GetGlobalPath(FactoryPersistencePaths.PersistenceRootDirectory);
        if (!globalPath.StartsWith(persistenceRoot, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var suffix = globalPath.Substring(persistenceRoot.Length).Replace('\\', '/');
        if (!suffix.StartsWith("/", StringComparison.Ordinal))
        {
            suffix = "/" + suffix;
        }

        return FactoryPersistencePaths.PersistenceRootDirectory + suffix;
    }

    private static void SortEntries(List<FactoryMapCatalogEntry> entries)
    {
        entries.Sort((left, right) =>
        {
            var sourceCompare = left.Source.CompareTo(right.Source);
            if (sourceCompare != 0)
            {
                return sourceCompare;
            }

            return string.Compare(left.DisplayName, right.DisplayName, StringComparison.OrdinalIgnoreCase);
        });
    }
}
