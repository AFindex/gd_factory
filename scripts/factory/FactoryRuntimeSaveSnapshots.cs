using Godot;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;

public readonly record struct FactoryRuntimeInt2(int X, int Y)
{
    public Vector2I ToVector2I() => new(X, Y);
    public static FactoryRuntimeInt2 FromVector2I(Vector2I value) => new(value.X, value.Y);
}

public readonly record struct FactoryRuntimeVec3(float X, float Y, float Z)
{
    public Vector3 ToVector3() => new(X, Y, Z);
    public static FactoryRuntimeVec3 FromVector3(Vector3 value) => new(value.X, value.Y, value.Z);
}

public sealed class FactoryRuntimeSaveSlotMetadata
{
    public string SlotId { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string SavedAtUtc { get; set; } = string.Empty;
    public int SiteCount { get; set; }
}

public sealed class FactoryRuntimeSaveIndex
{
    public int Version { get; set; }
    public List<FactoryRuntimeSaveSlotMetadata> Slots { get; set; } = new();
}

public sealed class FactoryRuntimeItemSnapshot
{
    public int Id { get; set; }
    public BuildPrototypeKind SourceKind { get; set; }
    public FactoryItemKind ItemKind { get; set; }

    public FactoryItem ToItem(SimulationController simulation)
    {
        return simulation.CreateItemWithId(Id, SourceKind, ItemKind);
    }

    public static FactoryRuntimeItemSnapshot FromItem(FactoryItem item)
    {
        return new FactoryRuntimeItemSnapshot
        {
            Id = item.Id,
            SourceKind = item.SourceKind,
            ItemKind = item.ItemKind
        };
    }
}

public sealed class FactoryRuntimeInventoryStackSnapshot
{
    public FactoryRuntimeInt2 Slot { get; set; }
    public List<FactoryRuntimeItemSnapshot> Items { get; set; } = new();
}

public sealed class FactoryRuntimeInventorySnapshot
{
    public string InventoryId { get; set; } = string.Empty;
    public FactoryRuntimeInt2 GridSize { get; set; }
    public List<FactoryRuntimeInventoryStackSnapshot> Stacks { get; set; } = new();
}

public sealed class FactoryRuntimeTransitItemSnapshot
{
    public FactoryRuntimeItemSnapshot Item { get; set; } = new();
    public FactoryRuntimeInt2 SourceCell { get; set; }
    public FactoryRuntimeInt2 TargetCell { get; set; }
    public int LaneKey { get; set; }
    public float Position { get; set; }
    public float PreviousPosition { get; set; }
}

public sealed class FactoryStructureRuntimeSnapshot
{
    public string StructureKey { get; set; } = string.Empty;
    public string SiteId { get; set; } = string.Empty;
    public BuildPrototypeKind Kind { get; set; }
    public FactoryRuntimeInt2 Cell { get; set; }
    public FacingDirection Facing { get; set; }
    public float CurrentHealth { get; set; }
    public Dictionary<string, string> State { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public List<FactoryRuntimeInventorySnapshot> Inventories { get; set; } = new();
    public List<FactoryRuntimeTransitItemSnapshot> TransitItems { get; set; } = new();
}

public sealed class FactoryRuntimeSiteSnapshot
{
    public string SiteId { get; set; } = string.Empty;
    public FactoryMapKind Kind { get; set; }
    public string MapData { get; set; } = string.Empty;
    public List<FactoryStructureRuntimeSnapshot> Structures { get; set; } = new();
}

public sealed class FactoryPlayerRuntimeSnapshot
{
    public FactoryRuntimeVec3 Position { get; set; }
    public int ActiveHotbarIndex { get; set; }
    public bool IsHotbarPlacementArmed { get; set; }
    public string SelectedInventoryId { get; set; } = FactoryPlayerController.BackpackInventoryId;
    public FactoryRuntimeInt2 SelectedSlot { get; set; }
    public FactoryRuntimeInventorySnapshot Inventory { get; set; } = new();
}

public sealed class FactoryEnemyRuntimeSnapshot
{
    public string EnemyTypeId { get; set; } = string.Empty;
    public string EnemyId { get; set; } = string.Empty;
    public FactoryRuntimeVec3 Position { get; set; }
    public List<FactoryRuntimeVec3> PathPoints { get; set; } = new();
    public int NextPathIndex { get; set; }
    public float CurrentHealth { get; set; }
    public double AttackCooldown { get; set; }
}

public sealed class FactoryCombatLaneRuntimeSnapshot
{
    public string LaneId { get; set; } = string.Empty;
    public int SpawnIndex { get; set; }
    public float TimeUntilNextSpawn { get; set; }
}

public sealed class FactoryCombatDirectorRuntimeSnapshot
{
    public int SpawnCounter { get; set; }
    public int DestroyedStructureCount { get; set; }
    public int DefeatedEnemyCount { get; set; }
    public int TotalProjectileLaunchCount { get; set; }
    public List<FactoryCombatLaneRuntimeSnapshot> Lanes { get; set; } = new();
}

public sealed class FactoryMobileFactoryRuntimeSnapshot
{
    public string FactoryId { get; set; } = string.Empty;
    public MobileFactoryLifecycleState State { get; set; }
    public FactoryRuntimeVec3 HullPosition { get; set; }
    public FacingDirection TransitFacing { get; set; }
    public bool HasAnchorCell { get; set; }
    public FactoryRuntimeInt2 AnchorCell { get; set; }
    public FacingDirection DeploymentFacing { get; set; }
}

public sealed class FactoryRuntimeSaveSnapshotDocument
{
    public int Version { get; set; }
    public string SlotId { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string SavedAtUtc { get; set; } = string.Empty;
    public int MaxItemId { get; set; }
    public List<FactoryRuntimeSiteSnapshot> Sites { get; set; } = new();
    public FactoryPlayerRuntimeSnapshot? Player { get; set; }
    public FactoryCombatDirectorRuntimeSnapshot? CombatDirector { get; set; }
    public List<FactoryEnemyRuntimeSnapshot> Enemies { get; set; } = new();
    public FactoryMobileFactoryRuntimeSnapshot? MobileFactory { get; set; }
}

public sealed class FactoryRuntimeSaveResult
{
    public FactoryRuntimeSaveResult(FactoryRuntimeSaveSnapshotDocument document, string resourcePath)
    {
        Document = document;
        ResourcePath = resourcePath;
        GlobalPath = FactoryPersistencePaths.GetGlobalPath(resourcePath);
    }

    public FactoryRuntimeSaveSnapshotDocument Document { get; }
    public string ResourcePath { get; }
    public string GlobalPath { get; }
}

public static class FactoryRuntimeSnapshotValues
{
    public static string FormatFloat(float value) => value.ToString("R", CultureInfo.InvariantCulture);
    public static string FormatDouble(double value) => value.ToString("R", CultureInfo.InvariantCulture);
    public static string FormatInt(int value) => value.ToString(CultureInfo.InvariantCulture);
    public static string FormatBool(bool value) => value ? "true" : "false";
    public static string BuildStructureKey(BuildPrototypeKind kind, Vector2I cell, FacingDirection facing)
        => $"{kind}|{cell.X}|{cell.Y}|{facing}";

    public static FactoryRuntimeInventorySnapshot CaptureInventory(string inventoryId, FactorySlottedItemInventory inventory)
    {
        var snapshot = new FactoryRuntimeInventorySnapshot
        {
            InventoryId = inventoryId,
            GridSize = FactoryRuntimeInt2.FromVector2I(inventory.GridSize)
        };

        var slots = inventory.Snapshot();
        for (var index = 0; index < slots.Length; index++)
        {
            var slot = slots[index];
            if (!slot.HasItem)
            {
                continue;
            }

            var items = inventory.SnapshotSlotItems(slot.Position);
            var stackSnapshot = new FactoryRuntimeInventoryStackSnapshot
            {
                Slot = FactoryRuntimeInt2.FromVector2I(slot.Position)
            };

            for (var itemIndex = 0; itemIndex < items.Count; itemIndex++)
            {
                stackSnapshot.Items.Add(FactoryRuntimeItemSnapshot.FromItem(items[itemIndex]));
            }

            snapshot.Stacks.Add(stackSnapshot);
        }

        return snapshot;
    }

    public static bool TryRestoreInventory(
        FactorySlottedItemInventory inventory,
        FactoryRuntimeInventorySnapshot snapshot,
        SimulationController simulation)
    {
        if (snapshot.GridSize.ToVector2I() != inventory.GridSize)
        {
            return false;
        }

        var stacks = new List<(Vector2I Slot, IReadOnlyList<FactoryItem> Items)>(snapshot.Stacks.Count);
        for (var index = 0; index < snapshot.Stacks.Count; index++)
        {
            var stackSnapshot = snapshot.Stacks[index];
            var items = new List<FactoryItem>(stackSnapshot.Items.Count);
            for (var itemIndex = 0; itemIndex < stackSnapshot.Items.Count; itemIndex++)
            {
                items.Add(stackSnapshot.Items[itemIndex].ToItem(simulation));
            }

            stacks.Add((stackSnapshot.Slot.ToVector2I(), items));
        }

        return inventory.TryRestoreSnapshot(stacks);
    }

    public static bool TryGetFloat(IReadOnlyDictionary<string, string> values, string key, out float result)
    {
        if (values.TryGetValue(key, out var raw)
            && float.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out result))
        {
            return true;
        }

        result = 0.0f;
        return false;
    }

    public static bool TryGetDouble(IReadOnlyDictionary<string, string> values, string key, out double result)
    {
        if (values.TryGetValue(key, out var raw)
            && double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out result))
        {
            return true;
        }

        result = 0.0;
        return false;
    }

    public static bool TryGetInt(IReadOnlyDictionary<string, string> values, string key, out int result)
    {
        if (values.TryGetValue(key, out var raw)
            && int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out result))
        {
            return true;
        }

        result = 0;
        return false;
    }

    public static bool TryGetBool(IReadOnlyDictionary<string, string> values, string key, out bool result)
    {
        if (values.TryGetValue(key, out var raw)
            && bool.TryParse(raw, out result))
        {
            return true;
        }

        result = false;
        return false;
    }
}

public static class FactoryRuntimeSavePersistence
{
    public const int SupportedVersion = 1;

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true
    };

    public static FactoryRuntimeSaveResult Save(FactoryRuntimeSaveSnapshotDocument document)
    {
        EnsurePersistenceAllowed();
        var validated = ValidateDocument(document);
        var slotId = FactoryPersistencePaths.SanitizeRuntimeSaveSlotId(validated.SlotId);
        validated.SlotId = slotId;
        var resourcePath = FactoryPersistencePaths.BuildRuntimeSaveFilePath(slotId);
        var globalPath = FactoryPersistencePaths.GetGlobalPath(resourcePath);
        var directoryPath = Path.GetDirectoryName(globalPath);
        if (!string.IsNullOrWhiteSpace(directoryPath))
        {
            Directory.CreateDirectory(directoryPath);
        }

        File.WriteAllText(globalPath, JsonSerializer.Serialize(validated, SerializerOptions));
        SaveIndexRecord(validated);
        return new FactoryRuntimeSaveResult(validated, resourcePath);
    }

    public static FactoryRuntimeSaveSnapshotDocument Load(string slotId)
    {
        EnsurePersistenceAllowed();
        var sanitized = FactoryPersistencePaths.SanitizeRuntimeSaveSlotId(slotId);
        var resourcePath = FactoryPersistencePaths.BuildRuntimeSaveFilePath(sanitized);
        var globalPath = FactoryPersistencePaths.GetGlobalPath(resourcePath);
        if (!File.Exists(globalPath))
        {
            throw new FileNotFoundException($"Runtime save slot '{slotId}' was not found.", globalPath);
        }

        var json = File.ReadAllText(globalPath);
        var document = JsonSerializer.Deserialize<FactoryRuntimeSaveSnapshotDocument>(json, SerializerOptions)
            ?? throw new InvalidDataException($"Runtime save slot '{slotId}' could not be deserialized.");
        return ValidateDocument(document);
    }

    public static FactoryRuntimeSaveIndex LoadIndex()
    {
        EnsurePersistenceAllowed();
        var globalPath = FactoryPersistencePaths.GetGlobalPath(FactoryPersistencePaths.RuntimeSaveIndexFilePath);
        if (!File.Exists(globalPath))
        {
            return new FactoryRuntimeSaveIndex
            {
                Version = SupportedVersion
            };
        }

        var json = File.ReadAllText(globalPath);
        var index = JsonSerializer.Deserialize<FactoryRuntimeSaveIndex>(json, SerializerOptions)
            ?? new FactoryRuntimeSaveIndex();
        index.Version = SupportedVersion;
        index.Slots ??= new List<FactoryRuntimeSaveSlotMetadata>();
        return index;
    }

    public static FactoryRuntimeSaveSnapshotDocument ValidateDocument(FactoryRuntimeSaveSnapshotDocument document)
    {
        if (document.Version != SupportedVersion)
        {
            throw new InvalidDataException($"Runtime save snapshot version '{document.Version}' is unsupported.");
        }

        if (string.IsNullOrWhiteSpace(document.SlotId))
        {
            throw new InvalidDataException("Runtime save snapshot is missing a slot id.");
        }

        if (document.Sites.Count == 0)
        {
            throw new InvalidDataException("Runtime save snapshot does not contain any site snapshots.");
        }

        var seenSiteIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        for (var index = 0; index < document.Sites.Count; index++)
        {
            var site = document.Sites[index];
            if (string.IsNullOrWhiteSpace(site.SiteId))
            {
                throw new InvalidDataException($"Runtime save snapshot site #{index} is missing a site id.");
            }

            if (!seenSiteIds.Add(site.SiteId))
            {
                throw new InvalidDataException($"Runtime save snapshot contains duplicate site id '{site.SiteId}'.");
            }

            if (string.IsNullOrWhiteSpace(site.MapData))
            {
                throw new InvalidDataException($"Runtime save snapshot site '{site.SiteId}' is missing embedded map data.");
            }

            var seenStructureKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (var structureIndex = 0; structureIndex < site.Structures.Count; structureIndex++)
            {
                var structure = site.Structures[structureIndex];
                if (string.IsNullOrWhiteSpace(structure.StructureKey))
                {
                    throw new InvalidDataException($"Runtime save structure #{structureIndex} in site '{site.SiteId}' is missing its structure key.");
                }

                if (!seenStructureKeys.Add(structure.StructureKey))
                {
                    throw new InvalidDataException($"Runtime save site '{site.SiteId}' contains duplicate structure key '{structure.StructureKey}'.");
                }
            }
        }

        document.SlotId = FactoryPersistencePaths.SanitizeRuntimeSaveSlotId(document.SlotId);
        if (string.IsNullOrWhiteSpace(document.DisplayName))
        {
            document.DisplayName = document.SlotId;
        }

        if (string.IsNullOrWhiteSpace(document.SavedAtUtc))
        {
            document.SavedAtUtc = DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture);
        }

        return document;
    }

    private static void SaveIndexRecord(FactoryRuntimeSaveSnapshotDocument document)
    {
        var index = LoadIndex();
        var existing = index.Slots.FindIndex(slot => string.Equals(slot.SlotId, document.SlotId, StringComparison.OrdinalIgnoreCase));
        var record = new FactoryRuntimeSaveSlotMetadata
        {
            SlotId = document.SlotId,
            DisplayName = document.DisplayName,
            SavedAtUtc = document.SavedAtUtc,
            SiteCount = document.Sites.Count
        };

        if (existing >= 0)
        {
            index.Slots[existing] = record;
        }
        else
        {
            index.Slots.Add(record);
        }

        index.Slots.Sort((left, right) => string.Compare(right.SavedAtUtc, left.SavedAtUtc, StringComparison.Ordinal));

        var globalPath = FactoryPersistencePaths.GetGlobalPath(FactoryPersistencePaths.RuntimeSaveIndexFilePath);
        var directoryPath = Path.GetDirectoryName(globalPath);
        if (!string.IsNullOrWhiteSpace(directoryPath))
        {
            Directory.CreateDirectory(directoryPath);
        }

        File.WriteAllText(globalPath, JsonSerializer.Serialize(index, SerializerOptions));
    }

    private static void EnsurePersistenceAllowed()
    {
        if (!FactoryPersistencePaths.IsPersistenceEnabled())
        {
            throw new InvalidOperationException("Runtime persistence is disabled while smoke tests or validation commands are running.");
        }
    }
}
