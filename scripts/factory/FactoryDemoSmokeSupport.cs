using Godot;
using System;
using System.Collections.Generic;

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
}
