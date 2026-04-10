using Godot;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

public static class FactoryDemoSmokeSupport
{
    public static bool HasWorkspace(IReadOnlyList<string> workspaceIds, string workspaceId)
    {
        for (var index = 0; index < workspaceIds.Count; index++)
        {
            if (string.Equals(workspaceIds[index], workspaceId, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    public static bool ContainsAllWorkspaces(IReadOnlyList<string> workspaceIds, IReadOnlyList<string> requiredWorkspaceIds)
    {
        for (var index = 0; index < requiredWorkspaceIds.Count; index++)
        {
            if (!HasWorkspace(workspaceIds, requiredWorkspaceIds[index]))
            {
                return false;
            }
        }

        return true;
    }

    public static int CountMatchingPlacedEntries(
        IReadOnlyList<FactoryBlueprintPlanEntry> entries,
        Func<Vector2I, FactoryStructure?> resolveStructure)
    {
        var matched = 0;
        for (var index = 0; index < entries.Count; index++)
        {
            var entry = entries[index];
            var structure = resolveStructure(entry.TargetCell);
            if (structure is not null
                && structure.Kind == entry.SourceEntry.Kind
                && structure.Facing == entry.TargetFacing)
            {
                matched++;
            }
        }

        return matched;
    }

    public static FactoryRuntimeSiteSnapshot FindRequiredSite(
        FactoryRuntimeSaveSnapshotDocument document,
        string siteId,
        FactoryMapKind kind)
    {
        for (var index = 0; index < document.Sites.Count; index++)
        {
            var site = document.Sites[index];
            if (string.Equals(site.SiteId, siteId, StringComparison.OrdinalIgnoreCase) && site.Kind == kind)
            {
                return site;
            }
        }

        throw new InvalidOperationException($"Runtime smoke could not find site '{siteId}' ({kind}).");
    }

    public static FactoryStructureRuntimeSnapshot FindRequiredStructure(
        FactoryRuntimeSiteSnapshot site,
        BuildPrototypeKind kind,
        Vector2I cell)
    {
        for (var index = 0; index < site.Structures.Count; index++)
        {
            var structure = site.Structures[index];
            if (structure.Kind == kind && structure.Cell.ToVector2I() == cell)
            {
                return structure;
            }
        }

        throw new InvalidOperationException(
            $"Runtime smoke could not find structure '{kind}' at ({cell.X}, {cell.Y}) in site '{site.SiteId}'.");
    }

    public static FactoryStructureRuntimeSnapshot? FindFirstStructure(
        FactoryRuntimeSiteSnapshot site,
        Func<FactoryStructureRuntimeSnapshot, bool> predicate)
    {
        for (var index = 0; index < site.Structures.Count; index++)
        {
            if (predicate(site.Structures[index]))
            {
                return site.Structures[index];
            }
        }

        return null;
    }

    public static string SummarizePlayerSnapshot(FactoryPlayerRuntimeSnapshot? snapshot)
    {
        if (snapshot is null)
        {
            return "player:none";
        }

        var builder = new StringBuilder();
        builder.Append("player:")
            .Append(Format(snapshot.Position.X)).Append(',')
            .Append(Format(snapshot.Position.Y)).Append(',')
            .Append(Format(snapshot.Position.Z)).Append('|')
            .Append(snapshot.ActiveHotbarIndex).Append('|')
            .Append(snapshot.IsHotbarPlacementArmed).Append('|')
            .Append(snapshot.SelectedInventoryId).Append('|')
            .Append(snapshot.SelectedSlot.X).Append(',').Append(snapshot.SelectedSlot.Y).Append('|')
            .Append(SummarizeInventory(snapshot.Inventory));
        return builder.ToString();
    }

    public static string SummarizeCombatSnapshot(FactoryCombatDirectorRuntimeSnapshot? snapshot)
    {
        if (snapshot is null)
        {
            return "combat:none";
        }

        var builder = new StringBuilder();
        builder.Append("combat:")
            .Append(snapshot.SpawnCounter).Append('|')
            .Append(snapshot.DestroyedStructureCount).Append('|')
            .Append(snapshot.DefeatedEnemyCount).Append('|')
            .Append(snapshot.TotalProjectileLaunchCount);

        var lanes = new List<FactoryCombatLaneRuntimeSnapshot>(snapshot.Lanes);
        lanes.Sort((left, right) => string.Compare(left.LaneId, right.LaneId, StringComparison.Ordinal));
        for (var index = 0; index < lanes.Count; index++)
        {
            builder.Append('|')
                .Append(lanes[index].LaneId).Append(':')
                .Append(lanes[index].SpawnIndex).Append(':')
                .Append(Format(lanes[index].TimeUntilNextSpawn));
        }

        return builder.ToString();
    }

    public static string SummarizeEnemySnapshots(IEnumerable<FactoryEnemyRuntimeSnapshot> snapshots)
    {
        var enemies = new List<FactoryEnemyRuntimeSnapshot>(snapshots);
        enemies.Sort((left, right) =>
        {
            var typeCompare = string.Compare(left.EnemyTypeId, right.EnemyTypeId, StringComparison.Ordinal);
            return typeCompare != 0
                ? typeCompare
                : string.Compare(left.EnemyId, right.EnemyId, StringComparison.Ordinal);
        });

        var builder = new StringBuilder();
        builder.Append("enemies:");
        for (var index = 0; index < enemies.Count; index++)
        {
            var enemy = enemies[index];
            if (index > 0)
            {
                builder.Append('|');
            }

            builder.Append(enemy.EnemyTypeId).Append(':')
                .Append(enemy.EnemyId).Append(':')
                .Append(Format(enemy.Position.X)).Append(',')
                .Append(Format(enemy.Position.Y)).Append(',')
                .Append(Format(enemy.Position.Z)).Append(':')
                .Append(enemy.NextPathIndex).Append(':')
                .Append(Format(enemy.CurrentHealth)).Append(':')
                .Append(Format(enemy.AttackCooldown)).Append(':')
                .Append(enemy.PathPoints.Count);
        }

        return builder.ToString();
    }

    public static string SummarizeStructureSnapshot(FactoryStructureRuntimeSnapshot snapshot)
    {
        var builder = new StringBuilder();
        builder.Append(snapshot.StructureKey).Append('|')
            .Append(snapshot.SiteId).Append('|')
            .Append(snapshot.Kind).Append('|')
            .Append(snapshot.Cell.X).Append(',').Append(snapshot.Cell.Y).Append('|')
            .Append(snapshot.Facing).Append('|')
            .Append(Format(snapshot.CurrentHealth));

        if (snapshot.State.Count > 0)
        {
            var keys = new List<string>(snapshot.State.Keys);
            keys.Sort(StringComparer.Ordinal);
            builder.Append("|state=");
            for (var index = 0; index < keys.Count; index++)
            {
                if (index > 0)
                {
                    builder.Append(';');
                }

                builder.Append(keys[index]).Append('=').Append(snapshot.State[keys[index]]);
            }
        }

        if (snapshot.Inventories.Count > 0)
        {
            var inventories = new List<FactoryRuntimeInventorySnapshot>(snapshot.Inventories);
            inventories.Sort((left, right) => string.Compare(left.InventoryId, right.InventoryId, StringComparison.Ordinal));
            for (var index = 0; index < inventories.Count; index++)
            {
                builder.Append("|inv=").Append(SummarizeInventory(inventories[index]));
            }
        }

        if (snapshot.TransitItems.Count > 0)
        {
            var transitItems = new List<FactoryRuntimeTransitItemSnapshot>(snapshot.TransitItems);
            transitItems.Sort((left, right) =>
            {
                var laneCompare = left.LaneKey.CompareTo(right.LaneKey);
                return laneCompare != 0
                    ? laneCompare
                    : left.Item.Id.CompareTo(right.Item.Id);
            });

            for (var index = 0; index < transitItems.Count; index++)
            {
                var transit = transitItems[index];
                builder.Append("|transit=")
                    .Append(transit.Item.Id).Append(':')
                    .Append(transit.Item.ItemKind).Append(':')
                    .Append(transit.SourceCell.X).Append(',').Append(transit.SourceCell.Y).Append(':')
                    .Append(transit.TargetCell.X).Append(',').Append(transit.TargetCell.Y).Append(':')
                    .Append(transit.LaneKey).Append(':')
                    .Append(Format(transit.Position)).Append(':')
                    .Append(Format(transit.PreviousPosition));
            }
        }

        return builder.ToString();
    }

    public static string SummarizeMobileFactorySnapshot(FactoryMobileFactoryRuntimeSnapshot? snapshot)
    {
        if (snapshot is null)
        {
            return "mobile:none";
        }

        return string.Create(
            CultureInfo.InvariantCulture,
            $"mobile:{snapshot.FactoryId}|{snapshot.State}|{Format(snapshot.HullPosition.X)},{Format(snapshot.HullPosition.Y)},{Format(snapshot.HullPosition.Z)}|{snapshot.TransitFacing}|{snapshot.HasAnchorCell}|{snapshot.AnchorCell.X},{snapshot.AnchorCell.Y}|{snapshot.DeploymentFacing}");
    }

    private static string SummarizeInventory(FactoryRuntimeInventorySnapshot snapshot)
    {
        var builder = new StringBuilder();
        builder.Append(snapshot.InventoryId).Append('@')
            .Append(snapshot.GridSize.X).Append('x').Append(snapshot.GridSize.Y);

        var stacks = new List<FactoryRuntimeInventoryStackSnapshot>(snapshot.Stacks);
        stacks.Sort((left, right) =>
        {
            var xCompare = left.Slot.X.CompareTo(right.Slot.X);
            return xCompare != 0
                ? xCompare
                : left.Slot.Y.CompareTo(right.Slot.Y);
        });

        for (var index = 0; index < stacks.Count; index++)
        {
            var stack = stacks[index];
            builder.Append('|')
                .Append(stack.Slot.X).Append(',').Append(stack.Slot.Y).Append('=');
            for (var itemIndex = 0; itemIndex < stack.Items.Count; itemIndex++)
            {
                if (itemIndex > 0)
                {
                    builder.Append(',');
                }

                var item = stack.Items[itemIndex];
                builder.Append(item.Id).Append(':').Append(item.SourceKind).Append(':').Append(item.ItemKind);
            }
        }

        return builder.ToString();
    }

    private static string Format(float value)
    {
        return Math.Round(value, 3).ToString("0.###", CultureInfo.InvariantCulture);
    }

    private static string Format(double value)
    {
        return Math.Round(value, 3).ToString("0.###", CultureInfo.InvariantCulture);
    }
}
