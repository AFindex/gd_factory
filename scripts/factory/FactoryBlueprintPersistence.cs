using Godot;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

public sealed class FactoryBlueprintPersistenceSnapshot
{
    public FactoryBlueprintPersistenceSnapshot(
        IReadOnlyList<FactoryBlueprintRecord>? runtimeRecords = null,
        IReadOnlyList<FactoryBlueprintRecord>? sourceRecords = null,
        string? activeBlueprintId = null)
    {
        RuntimeRecords = runtimeRecords ?? Array.Empty<FactoryBlueprintRecord>();
        SourceRecords = sourceRecords ?? Array.Empty<FactoryBlueprintRecord>();
        ActiveBlueprintId = activeBlueprintId;
    }

    public IReadOnlyList<FactoryBlueprintRecord> RuntimeRecords { get; }
    public IReadOnlyList<FactoryBlueprintRecord> SourceRecords { get; }
    public string? ActiveBlueprintId { get; }
}

public enum FactoryBlueprintPersistenceTarget
{
    Runtime,
    Source
}

public static class FactoryBlueprintPersistence
{
    private const int FormatVersion = 1;
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true
    };

    public static FactoryBlueprintPersistenceSnapshot Load()
    {
        var runtimeRecords = FactoryPersistencePaths.IsPersistenceEnabled()
            ? LoadRecordsFromDirectory(
                FactoryPersistencePaths.BlueprintDirectory,
                skipFileName: Path.GetFileName(FactoryPersistencePaths.BlueprintStateFilePath))
            : Array.Empty<FactoryBlueprintRecord>();
        var sourceRecords = LoadRecordsFromDirectory(FactoryPersistencePaths.BlueprintSourceDirectory);
        return new FactoryBlueprintPersistenceSnapshot(runtimeRecords, sourceRecords, LoadActiveBlueprintId());
    }

    public static void SaveRecord(FactoryBlueprintRecord record, FactoryBlueprintPersistenceTarget target)
    {
        if (!FactoryPersistencePaths.IsPersistenceEnabled())
        {
            return;
        }

        var existingGlobalFilePath = FindExistingRecordFilePath(record.Id, target);
        var filePath = GetBlueprintFilePath(record, target);
        var globalFilePath = FactoryPersistencePaths.GetGlobalPath(filePath);
        var directoryPath = Path.GetDirectoryName(FactoryPersistencePaths.GetGlobalPath(filePath));
        if (!string.IsNullOrWhiteSpace(directoryPath))
        {
            Directory.CreateDirectory(directoryPath);
        }

        File.WriteAllText(globalFilePath, SerializeRecord(record));
        if (!string.IsNullOrWhiteSpace(existingGlobalFilePath)
            && !string.Equals(existingGlobalFilePath, globalFilePath, StringComparison.OrdinalIgnoreCase)
            && File.Exists(existingGlobalFilePath))
        {
            File.Delete(existingGlobalFilePath);
        }
    }

    public static void DeleteRecord(string blueprintId, FactoryBlueprintPersistenceTarget target)
    {
        var globalFilePath = FindExistingRecordFilePath(blueprintId, target);
        if (!string.IsNullOrWhiteSpace(globalFilePath) && File.Exists(globalFilePath))
        {
            File.Delete(globalFilePath);
        }
    }

    public static void SaveActiveBlueprintState(string? activeBlueprintId)
    {
        SaveActiveBlueprintId(activeBlueprintId);
    }

    public static string ResolveRecordGlobalPath(FactoryBlueprintRecord record, FactoryBlueprintPersistenceTarget target)
    {
        return FindExistingRecordFilePath(record.Id, target)
            ?? FactoryPersistencePaths.GetGlobalPath(GetBlueprintFilePath(record, target));
    }

    private static string SerializeRecord(FactoryBlueprintRecord record)
    {
        var dto = new BlueprintRecordDto
        {
            FormatVersion = FormatVersion,
            Id = record.Id,
            DisplayName = record.DisplayName,
            SourceSiteKind = record.SourceSiteKind.ToString(),
            SuggestedAnchorCell = new Vector2IDto(record.SuggestedAnchorCell),
            BoundsSize = new Vector2IDto(record.BoundsSize),
            Entries = BuildEntryDtos(record.Entries),
            RequiredAttachments = BuildAttachmentDtos(record.RequiredAttachments)
        };
        return JsonSerializer.Serialize(dto, SerializerOptions);
    }

    private static FactoryBlueprintRecord DeserializeRecord(string json, string sourcePath)
    {
        var dto = JsonSerializer.Deserialize<BlueprintRecordDto>(json, SerializerOptions)
            ?? throw new InvalidDataException($"Blueprint file '{sourcePath}' is empty.");
        if (dto.FormatVersion != FormatVersion)
        {
            throw new InvalidDataException($"Blueprint file '{sourcePath}' uses unsupported format version {dto.FormatVersion}.");
        }

        if (!Enum.TryParse(dto.SourceSiteKind, ignoreCase: true, out FactoryBlueprintSiteKind siteKind))
        {
            throw new InvalidDataException($"Blueprint file '{sourcePath}' contains unknown site kind '{dto.SourceSiteKind}'.");
        }

        var entries = new List<FactoryBlueprintStructureEntry>(dto.Entries?.Count ?? 0);
        if (dto.Entries is not null)
        {
            for (var index = 0; index < dto.Entries.Count; index++)
            {
                var entryDto = dto.Entries[index];
                if (!Enum.TryParse(entryDto.Kind, ignoreCase: true, out BuildPrototypeKind kind))
                {
                    throw new InvalidDataException($"Blueprint file '{sourcePath}' contains unknown structure kind '{entryDto.Kind}'.");
                }

                if (!Enum.TryParse(entryDto.Facing, ignoreCase: true, out FacingDirection facing))
                {
                    throw new InvalidDataException($"Blueprint file '{sourcePath}' contains unknown facing '{entryDto.Facing}'.");
                }

                entries.Add(new FactoryBlueprintStructureEntry(
                    kind,
                    entryDto.LocalCell?.ToVector2I() ?? Vector2I.Zero,
                    facing,
                    entryDto.Configuration ?? new Dictionary<string, string>(StringComparer.Ordinal)));
            }
        }

        var requiredAttachments = new List<FactoryBlueprintAttachmentRequirement>(dto.RequiredAttachments?.Count ?? 0);
        if (dto.RequiredAttachments is not null)
        {
            for (var index = 0; index < dto.RequiredAttachments.Count; index++)
            {
                var attachmentDto = dto.RequiredAttachments[index];
                if (!Enum.TryParse(attachmentDto.Kind, ignoreCase: true, out BuildPrototypeKind kind))
                {
                    throw new InvalidDataException($"Blueprint file '{sourcePath}' contains unknown attachment kind '{attachmentDto.Kind}'.");
                }

                if (!Enum.TryParse(attachmentDto.Facing, ignoreCase: true, out FacingDirection facing))
                {
                    throw new InvalidDataException($"Blueprint file '{sourcePath}' contains unknown attachment facing '{attachmentDto.Facing}'.");
                }

                requiredAttachments.Add(new FactoryBlueprintAttachmentRequirement(
                    kind,
                    attachmentDto.LocalCell?.ToVector2I() ?? Vector2I.Zero,
                    facing));
            }
        }

        return new FactoryBlueprintRecord(
            dto.Id ?? throw new InvalidDataException($"Blueprint file '{sourcePath}' is missing an id."),
            dto.DisplayName ?? "未命名蓝图",
            siteKind,
            dto.SuggestedAnchorCell?.ToVector2I() ?? Vector2I.Zero,
            dto.BoundsSize?.ToVector2I() ?? Vector2I.One,
            entries,
            requiredAttachments);
    }

    private static List<BlueprintEntryDto> BuildEntryDtos(IReadOnlyList<FactoryBlueprintStructureEntry> entries)
    {
        var result = new List<BlueprintEntryDto>(entries.Count);
        for (var index = 0; index < entries.Count; index++)
        {
            var entry = entries[index];
            result.Add(new BlueprintEntryDto
            {
                Kind = entry.Kind.ToString(),
                LocalCell = new Vector2IDto(entry.LocalCell),
                Facing = entry.Facing.ToString(),
                Configuration = entry.Configuration.Count == 0
                    ? null
                    : new Dictionary<string, string>(entry.Configuration, StringComparer.Ordinal)
            });
        }

        return result;
    }

    private static List<BlueprintAttachmentDto> BuildAttachmentDtos(IReadOnlyList<FactoryBlueprintAttachmentRequirement> attachments)
    {
        var result = new List<BlueprintAttachmentDto>(attachments.Count);
        for (var index = 0; index < attachments.Count; index++)
        {
            var attachment = attachments[index];
            result.Add(new BlueprintAttachmentDto
            {
                Kind = attachment.Kind.ToString(),
                LocalCell = new Vector2IDto(attachment.LocalCell),
                Facing = attachment.Facing.ToString()
            });
        }

        return result;
    }

    private static string? LoadActiveBlueprintId()
    {
        if (!FactoryPersistencePaths.IsPersistenceEnabled())
        {
            return null;
        }

        var statePath = FactoryPersistencePaths.GetGlobalPath(FactoryPersistencePaths.BlueprintStateFilePath);
        if (!File.Exists(statePath))
        {
            return null;
        }

        try
        {
            var json = File.ReadAllText(statePath);
            var dto = JsonSerializer.Deserialize<BlueprintStateDto>(json, SerializerOptions);
            return string.IsNullOrWhiteSpace(dto?.ActiveBlueprintId) ? null : dto.ActiveBlueprintId;
        }
        catch (Exception ex)
        {
            GD.PushWarning($"Failed to load blueprint state '{statePath}': {ex.Message}");
            return null;
        }
    }

    private static void SaveActiveBlueprintId(string? activeBlueprintId)
    {
        if (!FactoryPersistencePaths.IsPersistenceEnabled())
        {
            return;
        }

        FactoryPersistencePaths.EnsureDirectory(FactoryPersistencePaths.BlueprintDirectory);
        var statePath = FactoryPersistencePaths.GetGlobalPath(FactoryPersistencePaths.BlueprintStateFilePath);
        var dto = new BlueprintStateDto
        {
            FormatVersion = FormatVersion,
            ActiveBlueprintId = activeBlueprintId
        };
        File.WriteAllText(statePath, JsonSerializer.Serialize(dto, SerializerOptions));
    }

    private static IReadOnlyList<FactoryBlueprintRecord> LoadRecordsFromDirectory(string directoryPath, string? skipFileName = null)
    {
        var records = new List<FactoryBlueprintRecord>();
        var globalDirectoryPath = FactoryPersistencePaths.GetGlobalPath(directoryPath);
        if (!Directory.Exists(globalDirectoryPath))
        {
            return records;
        }

        var files = Directory.GetFiles(globalDirectoryPath, "*.json", SearchOption.TopDirectoryOnly);
        Array.Sort(files, StringComparer.OrdinalIgnoreCase);
        for (var index = 0; index < files.Length; index++)
        {
            var filePath = files[index];
            if (!string.IsNullOrWhiteSpace(skipFileName)
                && string.Equals(Path.GetFileName(filePath), skipFileName, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            try
            {
                var json = File.ReadAllText(filePath);
                records.Add(DeserializeRecord(json, filePath));
            }
            catch (Exception ex)
            {
                GD.PushWarning($"Failed to load persisted blueprint '{filePath}': {ex.Message}");
            }
        }

        return records;
    }

    private static string GetBlueprintFilePath(FactoryBlueprintRecord record, FactoryBlueprintPersistenceTarget target)
    {
        var directoryPath = GetBlueprintDirectoryPath(target);
        var preferredStem = FactoryPersistencePaths.SanitizeBlueprintFileStem(record.DisplayName);
        var existingGlobalFilePath = FindExistingRecordFilePath(record.Id, target);
        var preferredFilePath = CombineBlueprintFilePath(directoryPath, preferredStem);
        var preferredGlobalFilePath = FactoryPersistencePaths.GetGlobalPath(preferredFilePath);

        if (!File.Exists(preferredGlobalFilePath)
            || string.Equals(existingGlobalFilePath, preferredGlobalFilePath, StringComparison.OrdinalIgnoreCase))
        {
            return preferredFilePath;
        }

        var suffix = 2;
        while (true)
        {
            var candidateFilePath = CombineBlueprintFilePath(directoryPath, $"{preferredStem}-{suffix}");
            var candidateGlobalFilePath = FactoryPersistencePaths.GetGlobalPath(candidateFilePath);
            if (!File.Exists(candidateGlobalFilePath)
                || string.Equals(existingGlobalFilePath, candidateGlobalFilePath, StringComparison.OrdinalIgnoreCase))
            {
                return candidateFilePath;
            }

            suffix++;
        }
    }

    private static string? FindExistingRecordFilePath(string blueprintId, FactoryBlueprintPersistenceTarget target)
    {
        var directoryPath = FactoryPersistencePaths.GetGlobalPath(GetBlueprintDirectoryPath(target));
        if (!Directory.Exists(directoryPath))
        {
            return null;
        }

        var files = Directory.GetFiles(directoryPath, "*.json", SearchOption.TopDirectoryOnly);
        for (var index = 0; index < files.Length; index++)
        {
            var filePath = files[index];
            if (IsSkippedBlueprintFile(filePath, target))
            {
                continue;
            }

            if (TryReadBlueprintId(filePath, out var existingBlueprintId)
                && string.Equals(existingBlueprintId, blueprintId, StringComparison.Ordinal))
            {
                return filePath;
            }
        }

        return null;
    }

    private static bool TryReadBlueprintId(string filePath, out string blueprintId)
    {
        blueprintId = string.Empty;
        try
        {
            var json = File.ReadAllText(filePath);
            var dto = JsonSerializer.Deserialize<BlueprintRecordDto>(json, SerializerOptions);
            if (string.IsNullOrWhiteSpace(dto?.Id))
            {
                return false;
            }

            blueprintId = dto.Id;
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static bool IsSkippedBlueprintFile(string filePath, FactoryBlueprintPersistenceTarget target)
    {
        return target == FactoryBlueprintPersistenceTarget.Runtime
            && string.Equals(
                Path.GetFileName(filePath),
                Path.GetFileName(FactoryPersistencePaths.BlueprintStateFilePath),
                StringComparison.OrdinalIgnoreCase);
    }

    private static string GetBlueprintDirectoryPath(FactoryBlueprintPersistenceTarget target)
    {
        return target == FactoryBlueprintPersistenceTarget.Source
            ? FactoryPersistencePaths.BlueprintSourceDirectory
            : FactoryPersistencePaths.BlueprintDirectory;
    }

    private static string CombineBlueprintFilePath(string directoryPath, string fileStem)
    {
        return $"{directoryPath}/{fileStem}.json";
    }

    private sealed class BlueprintRecordDto
    {
        public int FormatVersion { get; set; }
        public string? Id { get; set; }
        public string? DisplayName { get; set; }
        public string? SourceSiteKind { get; set; }
        public Vector2IDto? SuggestedAnchorCell { get; set; }
        public Vector2IDto? BoundsSize { get; set; }
        public List<BlueprintEntryDto>? Entries { get; set; }
        public List<BlueprintAttachmentDto>? RequiredAttachments { get; set; }
    }

    private sealed class BlueprintEntryDto
    {
        public string? Kind { get; set; }
        public Vector2IDto? LocalCell { get; set; }
        public string? Facing { get; set; }
        public Dictionary<string, string>? Configuration { get; set; }
    }

    private sealed class BlueprintAttachmentDto
    {
        public string? Kind { get; set; }
        public Vector2IDto? LocalCell { get; set; }
        public string? Facing { get; set; }
    }

    private sealed class BlueprintStateDto
    {
        public int FormatVersion { get; set; }
        public string? ActiveBlueprintId { get; set; }
    }

    private sealed class Vector2IDto
    {
        public Vector2IDto()
        {
        }

        public Vector2IDto(Vector2I value)
        {
            X = value.X;
            Y = value.Y;
        }

        public int X { get; set; }
        public int Y { get; set; }

        public Vector2I ToVector2I()
        {
            return new Vector2I(X, Y);
        }
    }
}
