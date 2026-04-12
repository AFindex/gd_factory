using Godot;
using System;
using System.IO;

public static class FactoryPersistencePaths
{
    public const string PersistenceRootDirectory = "user://factory-runtime";
    public const string WorldMapDirectory = PersistenceRootDirectory + "/maps/world";
    public const string InteriorMapDirectory = PersistenceRootDirectory + "/maps/interior";
    public const string BlueprintDirectory = PersistenceRootDirectory + "/blueprints";
    public const string BlueprintStateFilePath = BlueprintDirectory + "/_state.json";
    public const string BlueprintSourceDirectory = "res://data/factory/blueprints";
    public const string RuntimeSaveDirectory = PersistenceRootDirectory + "/saves";
    public const string RuntimeSaveIndexFilePath = RuntimeSaveDirectory + "/_index.json";

    public static bool IsPersistenceEnabled()
    {
        foreach (var arg in OS.GetCmdlineUserArgs())
        {
            if (arg.StartsWith("--factory-smoke-test", StringComparison.Ordinal)
                || arg.StartsWith("--mobile-factory-smoke-test", StringComparison.Ordinal)
                || arg.StartsWith("--mobile-factory-large-scenario-smoke-test", StringComparison.Ordinal)
                || arg.StartsWith("--factory-map-validate", StringComparison.Ordinal)
                || arg.StartsWith("--factory-map-validate-cell", StringComparison.Ordinal))
            {
                return false;
            }
        }

        return true;
    }

    public static void EnsureDirectory(string userDirectoryPath)
    {
        var globalDirectory = ProjectSettings.GlobalizePath(userDirectoryPath);
        Directory.CreateDirectory(globalDirectory);
    }

    public static string GetGlobalPath(string userPath)
    {
        return ProjectSettings.GlobalizePath(userPath);
    }

    public static string GetWorldMapDirectoryGlobalPath()
    {
        return GetGlobalPath(WorldMapDirectory);
    }

    public static string GetInteriorMapDirectoryGlobalPath()
    {
        return GetGlobalPath(InteriorMapDirectory);
    }

    public static string GetBlueprintDirectoryGlobalPath()
    {
        return GetGlobalPath(BlueprintDirectory);
    }

    public static string GetRuntimeSaveDirectoryGlobalPath()
    {
        return GetGlobalPath(RuntimeSaveDirectory);
    }

    public static string GetBlueprintSourceDirectoryGlobalPath()
    {
        return GetGlobalPath(BlueprintSourceDirectory);
    }

    public static string BuildBlueprintPersistenceHint()
    {
        if (!IsPersistenceEnabled())
        {
            return "当前运行处于 smoke/validate 模式，蓝图持久化已禁用。";
        }

        return "蓝图可保存到运行时或工程内版本，便于调试与回写。";
    }

    public static string BuildPersistenceSummary(bool includeInteriorMap)
    {
        if (!IsPersistenceEnabled())
        {
            return "当前运行处于 smoke/validate 模式，地图与蓝图持久化已禁用。";
        }

        return includeInteriorMap
            ? "世界地图、内部地图、蓝图和进度存档功能已启用。"
            : "世界地图、蓝图和进度存档功能已启用。";
    }

    public static string BuildRuntimeMapSavePath(string sourcePath, FactoryMapKind kind)
    {
        var directory = kind == FactoryMapKind.Interior ? InteriorMapDirectory : WorldMapDirectory;
        var stem = SanitizeFileStem(Path.GetFileNameWithoutExtension(sourcePath));
        return $"{directory}/{stem}-runtime.nfmap";
    }

    public static bool IsRuntimePersistencePath(string path)
    {
        return path.StartsWith(PersistenceRootDirectory, StringComparison.OrdinalIgnoreCase);
    }

    public static string BuildBlueprintFilePath(string blueprintId)
    {
        return $"{BlueprintDirectory}/{SanitizeFileStem(blueprintId)}.json";
    }

    public static string BuildBlueprintSourceFilePath(string blueprintId)
    {
        return $"{BlueprintSourceDirectory}/{SanitizeFileStem(blueprintId)}.json";
    }

    public static string BuildBlueprintFilePathFromDisplayName(string displayName)
    {
        return $"{BlueprintDirectory}/{SanitizeFileStem(displayName)}.json";
    }

    public static string BuildBlueprintSourceFilePathFromDisplayName(string displayName)
    {
        return $"{BlueprintSourceDirectory}/{SanitizeFileStem(displayName)}.json";
    }

    public static string BuildRuntimeSaveFilePath(string slotId)
    {
        return $"{RuntimeSaveDirectory}/{SanitizeFileStem(slotId)}.json";
    }

    public static string SanitizeBlueprintFileStem(string? rawValue)
    {
        return SanitizeFileStem(rawValue);
    }

    public static string SanitizeRuntimeSaveSlotId(string? rawValue)
    {
        return SanitizeFileStem(rawValue);
    }

    private static string SanitizeFileStem(string? rawValue)
    {
        if (string.IsNullOrWhiteSpace(rawValue))
        {
            return "untitled";
        }

        var invalidChars = Path.GetInvalidFileNameChars();
        var buffer = rawValue.Trim().ToCharArray();
        for (var index = 0; index < buffer.Length; index++)
        {
            if (Array.IndexOf(invalidChars, buffer[index]) >= 0 || char.IsWhiteSpace(buffer[index]))
            {
                buffer[index] = '-';
            }
        }

        var sanitized = new string(buffer)
            .Replace("--", "-", StringComparison.Ordinal)
            .Trim('-', '.');
        return string.IsNullOrWhiteSpace(sanitized) ? "untitled" : sanitized;
    }
}
