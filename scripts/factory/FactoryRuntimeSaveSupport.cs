using Godot;
using System;
using System.Collections.Generic;

public static class FactoryRuntimeSaveSupport
{
    public static FactoryRuntimeSiteSnapshot BuildSiteSnapshot(
        string siteId,
        FactoryMapKind kind,
        FactoryMapDocument document,
        IEnumerable<FactoryStructure> structures)
    {
        var snapshot = new FactoryRuntimeSiteSnapshot
        {
            SiteId = siteId,
            Kind = kind,
            MapData = FactoryMapSerializer.Serialize(document)
        };

        foreach (var structure in structures)
        {
            snapshot.Structures.Add(structure.CaptureRuntimeSnapshot());
        }

        snapshot.Structures.Sort((left, right) => string.Compare(left.StructureKey, right.StructureKey, StringComparison.Ordinal));
        return snapshot;
    }

    public static FactoryMapDocument ParseSiteMap(FactoryRuntimeSiteSnapshot siteSnapshot, string sourceName)
    {
        var document = FactoryMapValidator.ValidateDocument(
            FactoryMapSerializer.Deserialize(siteSnapshot.MapData, sourceName));
        ValidateSiteStructureKeys(siteSnapshot, document);
        return document;
    }

    public static void ValidateSiteStructureKeys(FactoryRuntimeSiteSnapshot siteSnapshot, FactoryMapDocument document)
    {
        if (siteSnapshot.Structures.Count != document.Structures.Count)
        {
            throw new InvalidOperationException(
                $"Runtime site '{siteSnapshot.SiteId}' contains {siteSnapshot.Structures.Count} structure snapshots, but map data declares {document.Structures.Count} structures.");
        }

        var expectedKeys = new HashSet<string>(StringComparer.Ordinal);
        for (var index = 0; index < document.Structures.Count; index++)
        {
            var entry = document.Structures[index];
            expectedKeys.Add(FactoryStructure.BuildRuntimeStructureKey(entry.Kind, entry.Cell, entry.Facing));
        }

        for (var index = 0; index < siteSnapshot.Structures.Count; index++)
        {
            var key = siteSnapshot.Structures[index].StructureKey;
            if (!expectedKeys.Remove(key))
            {
                throw new InvalidOperationException(
                    $"Runtime site '{siteSnapshot.SiteId}' references unknown or duplicate structure key '{key}'.");
            }
        }

        if (expectedKeys.Count > 0)
        {
            throw new InvalidOperationException(
                $"Runtime site '{siteSnapshot.SiteId}' is missing {expectedKeys.Count} structure snapshots.");
        }
    }

    public static void ApplyStructureSnapshots(
        FactoryRuntimeSiteSnapshot siteSnapshot,
        IEnumerable<FactoryStructure> structures,
        SimulationController simulation)
    {
        var structuresByKey = BuildStructureIndex(structures);
        for (var index = 0; index < siteSnapshot.Structures.Count; index++)
        {
            var snapshot = siteSnapshot.Structures[index];
            if (!structuresByKey.TryGetValue(snapshot.StructureKey, out var structure))
            {
                throw new InvalidOperationException(
                    $"Could not find runtime structure '{snapshot.StructureKey}' in site '{siteSnapshot.SiteId}'.");
            }

            structure.ApplyRuntimeSnapshot(snapshot, simulation);
        }
    }

    public static void RestoreEnemies(
        Node3D enemyRoot,
        SimulationController simulation,
        IEnumerable<FactoryEnemyRuntimeSnapshot> snapshots)
    {
        foreach (var snapshot in snapshots)
        {
            if (!FactoryCombatDirector.IsKnownEnemyType(snapshot.EnemyTypeId))
            {
                throw new InvalidOperationException($"Unknown enemy type '{snapshot.EnemyTypeId}' in runtime snapshot.");
            }

            var enemy = FactoryCombatDirector.CreateEnemyActor(snapshot.EnemyTypeId);
            enemy.Name = snapshot.EnemyId;

            var path = new List<Vector3>(snapshot.PathPoints.Count);
            for (var index = 0; index < snapshot.PathPoints.Count; index++)
            {
                path.Add(snapshot.PathPoints[index].ToVector3());
            }

            enemy.Configure(snapshot.EnemyId, path);
            enemy.ApplyRuntimeSnapshot(snapshot);
            if (enemy.IsDefeated)
            {
                enemy.QueueFree();
                continue;
            }

            enemyRoot.AddChild(enemy);
            simulation.RegisterEnemy(enemy);
        }
    }

    public static void ValidateEnemySnapshots(IEnumerable<FactoryEnemyRuntimeSnapshot> snapshots)
    {
        foreach (var snapshot in snapshots)
        {
            if (!FactoryCombatDirector.IsKnownEnemyType(snapshot.EnemyTypeId))
            {
                throw new InvalidOperationException($"Unknown enemy type '{snapshot.EnemyTypeId}' in runtime snapshot.");
            }
        }
    }

    public static Dictionary<string, FactoryStructure> BuildStructureIndex(IEnumerable<FactoryStructure> structures)
    {
        var result = new Dictionary<string, FactoryStructure>(StringComparer.Ordinal);
        foreach (var structure in structures)
        {
            var key = structure.GetRuntimeStructureKey();
            result[key] = structure;
        }

        return result;
    }
}
