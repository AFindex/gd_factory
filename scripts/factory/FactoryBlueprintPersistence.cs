using Godot;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

public sealed class FactoryBlueprintPersistenceSnapshot
{
    public FactoryBlueprintPersistenceSnapshot(IReadOnlyList<FactoryBlueprintRecord>? records = null, string? activeBlueprintId = null)
    {
        Records = records ?? Array.Empty<FactoryBlueprintRecord>();
        ActiveBlueprintId = activeBlueprintId;
    }

    public IReadOnlyList<FactoryBlueprintRecord> Records { get; }
    public string? ActiveBlueprintId { get; }
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
        if (!FactoryPersistencePaths.IsPersistenceEnabled())
        {
            return new FactoryBlueprintPersistenceSnapshot();
        }

        FactoryPersistencePaths.EnsureDirectory(FactoryPersistencePaths.BlueprintDirectory);
        var records = new List<FactoryBlueprintRecord>();
        var directoryPath = FactoryPersistencePaths.GetGlobalPath(FactoryPersistencePaths.BlueprintDirectory);
        if (Directory.Exists(directoryPath))
        {
            var files = Directory.GetFiles(directoryPath, "*.json", SearchOption.TopDirectoryOnly);
            Array.Sort(files, StringComparer.OrdinalIgnoreCase);
            for (var index = 0; index < files.Length; index++)
            {
                var filePath = files[index];
                if (string.Equals(Path.GetFileName(filePath), Path.GetFileName(FactoryPersistencePaths.BlueprintStateFilePath), StringComparison.OrdinalIgnoreCase))
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
        }

        return new FactoryBlueprintPersistenceSnapshot(records, LoadActiveBlueprintId());
    }

    public static void Save(
        IReadOnlyList<FactoryBlueprintRecord> records,
        string? activeBlueprintId,
        Func<FactoryBlueprintRecord, bool> shouldPersist)
    {
        if (!FactoryPersistencePaths.IsPersistenceEnabled())
        {
            return;
        }

        FactoryPersistencePaths.EnsureDirectory(FactoryPersistencePaths.BlueprintDirectory);
        var directoryPath = FactoryPersistencePaths.GetGlobalPath(FactoryPersistencePaths.BlueprintDirectory);
        var expectedFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        for (var index = 0; index < records.Count; index++)
        {
            var record = records[index];
            if (!shouldPersist(record))
            {
                continue;
            }

            var userFilePath = FactoryPersistencePaths.BuildBlueprintFilePath(record.Id);
            var globalFilePath = FactoryPersistencePaths.GetGlobalPath(userFilePath);
            File.WriteAllText(globalFilePath, SerializeRecord(record));
            expectedFiles.Add(globalFilePath);
        }

        if (Directory.Exists(directoryPath))
        {
            var files = Directory.GetFiles(directoryPath, "*.json", SearchOption.TopDirectoryOnly);
            for (var index = 0; index < files.Length; index++)
            {
                var filePath = files[index];
                if (string.Equals(Path.GetFileName(filePath), Path.GetFileName(FactoryPersistencePaths.BlueprintStateFilePath), StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (!expectedFiles.Contains(filePath))
                {
                    File.Delete(filePath);
                }
            }
        }

        SaveActiveBlueprintId(activeBlueprintId);
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
