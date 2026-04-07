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

    public static string BuildBlueprintPersistenceHint()
    {
        if (!IsPersistenceEnabled())
        {
            return "当前运行处于 smoke/validate 模式，蓝图持久化已禁用。";
        }

        return $"蓝图会自动保存到 {GetBlueprintDirectoryGlobalPath()}。";
    }

    public static string BuildPersistenceSummary(bool includeInteriorMap)
    {
        if (!IsPersistenceEnabled())
        {
            return "当前运行处于 smoke/validate 模式，地图与蓝图持久化已禁用。";
        }

        return includeInteriorMap
            ? $"世界地图目录：{GetWorldMapDirectoryGlobalPath()}\n内部地图目录：{GetInteriorMapDirectoryGlobalPath()}\n蓝图目录：{GetBlueprintDirectoryGlobalPath()}"
            : $"世界地图目录：{GetWorldMapDirectoryGlobalPath()}\n蓝图目录：{GetBlueprintDirectoryGlobalPath()}";
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
