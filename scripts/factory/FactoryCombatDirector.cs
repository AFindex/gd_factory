using Godot;
using System;
using System.Collections.Generic;

public sealed class FactoryEnemySpawnRule
{
    public FactoryEnemySpawnRule(string enemyTypeId, float intervalSeconds)
    {
        EnemyTypeId = enemyTypeId;
        IntervalSeconds = intervalSeconds;
    }

    public string EnemyTypeId { get; }
    public float IntervalSeconds { get; }
}

public sealed class FactoryEnemyLaneDefinition
{
    public FactoryEnemyLaneDefinition(string laneId, IReadOnlyList<Vector3> pathPoints, IReadOnlyList<FactoryEnemySpawnRule> spawnRules)
    {
        LaneId = laneId;
        PathPoints = pathPoints;
        SpawnRules = spawnRules;
    }

    public string LaneId { get; }
    public IReadOnlyList<Vector3> PathPoints { get; }
    public IReadOnlyList<FactoryEnemySpawnRule> SpawnRules { get; }
}

public partial class FactoryCombatDirector : Node, IFactoryCombatSystem
{
    private sealed class LaneRuntimeState
    {
        public LaneRuntimeState(FactoryEnemyLaneDefinition definition)
        {
            Definition = definition;
            TimeUntilNextSpawn = definition.SpawnRules.Count > 0 ? definition.SpawnRules[0].IntervalSeconds : 0.0f;
        }

        public FactoryEnemyLaneDefinition Definition { get; }
        public int SpawnIndex { get; set; }
        public float TimeUntilNextSpawn { get; set; }
    }

    private readonly List<LaneRuntimeState> _lanes = new();
    private Node3D? _enemyRoot;
    private int _spawnCounter;

    public void Configure(SimulationController simulation, Node3D enemyRoot)
    {
        _enemyRoot = enemyRoot;
        simulation.RegisterCombatSystem(this);
    }

    public void ClearLanes()
    {
        _lanes.Clear();
    }

    public void AddLane(FactoryEnemyLaneDefinition lane)
    {
        _lanes.Add(new LaneRuntimeState(lane));
    }

    public FactoryCombatDirectorRuntimeSnapshot CaptureRuntimeSnapshot(SimulationController simulation)
    {
        var snapshot = new FactoryCombatDirectorRuntimeSnapshot
        {
            SpawnCounter = _spawnCounter,
            DestroyedStructureCount = simulation.DestroyedStructureCount,
            DefeatedEnemyCount = simulation.DefeatedEnemyCount,
            TotalProjectileLaunchCount = simulation.TotalProjectileLaunchCount
        };

        for (var index = 0; index < _lanes.Count; index++)
        {
            var lane = _lanes[index];
            snapshot.Lanes.Add(new FactoryCombatLaneRuntimeSnapshot
            {
                LaneId = lane.Definition.LaneId,
                SpawnIndex = lane.SpawnIndex,
                TimeUntilNextSpawn = lane.TimeUntilNextSpawn
            });
        }

        return snapshot;
    }

    public void ApplyRuntimeSnapshot(FactoryCombatDirectorRuntimeSnapshot snapshot, SimulationController simulation)
    {
        ValidateRuntimeSnapshot(snapshot);
        _spawnCounter = Mathf.Max(0, snapshot.SpawnCounter);
        simulation.RestoreRuntimeCounters(
            snapshot.DestroyedStructureCount,
            snapshot.DefeatedEnemyCount,
            snapshot.TotalProjectileLaunchCount);

        for (var index = 0; index < snapshot.Lanes.Count; index++)
        {
            var laneSnapshot = snapshot.Lanes[index];
            var lane = FindLaneRuntime(laneSnapshot.LaneId)
                ?? throw new InvalidOperationException($"Combat lane '{laneSnapshot.LaneId}' was not found during restore.");

            lane.SpawnIndex = lane.Definition.SpawnRules.Count == 0
                ? 0
                : Mathf.Clamp(laneSnapshot.SpawnIndex, 0, lane.Definition.SpawnRules.Count - 1);
            lane.TimeUntilNextSpawn = Mathf.Max(0.0f, laneSnapshot.TimeUntilNextSpawn);
        }
    }

    public void ValidateRuntimeSnapshot(FactoryCombatDirectorRuntimeSnapshot snapshot)
    {
        for (var index = 0; index < snapshot.Lanes.Count; index++)
        {
            if (FindLaneRuntime(snapshot.Lanes[index].LaneId) is null)
            {
                throw new InvalidOperationException(
                    $"Combat lane '{snapshot.Lanes[index].LaneId}' was not found during validation.");
            }
        }
    }

    public static FactoryEnemyActor CreateEnemyActor(string enemyTypeId)
    {
        return enemyTypeId switch
        {
            "ranged" => new RangedRaiderEnemy(),
            "world-brute" => new WorldBruteEnemy(),
            "world-siege" => new WorldSiegeEnemy(),
            _ => new MeleeRaiderEnemy()
        };
    }

    public static bool IsKnownEnemyType(string enemyTypeId)
    {
        return enemyTypeId is "melee" or "ranged" or "world-brute" or "world-siege";
    }

    public void SimulationStep(SimulationController simulation, double stepSeconds)
    {
        if (_enemyRoot is null)
        {
            return;
        }

        for (var i = 0; i < _lanes.Count; i++)
        {
            var lane = _lanes[i];
            if (lane.Definition.SpawnRules.Count == 0 || lane.Definition.PathPoints.Count == 0)
            {
                continue;
            }

            lane.TimeUntilNextSpawn -= (float)stepSeconds;
            if (lane.TimeUntilNextSpawn > 0.0f)
            {
                continue;
            }

            var rule = lane.Definition.SpawnRules[lane.SpawnIndex];
            SpawnEnemy(simulation, lane.Definition, rule);
            lane.SpawnIndex = (lane.SpawnIndex + 1) % lane.Definition.SpawnRules.Count;
            lane.TimeUntilNextSpawn = lane.Definition.SpawnRules[lane.SpawnIndex].IntervalSeconds;
        }
    }

    private void SpawnEnemy(SimulationController simulation, FactoryEnemyLaneDefinition lane, FactoryEnemySpawnRule rule)
    {
        if (_enemyRoot is null)
        {
            return;
        }

        var enemy = CreateEnemyActor(rule.EnemyTypeId);

        enemy.Name = $"{lane.LaneId}_{rule.EnemyTypeId}_{_spawnCounter++}";
        enemy.Configure(enemy.Name, lane.PathPoints);
        _enemyRoot.AddChild(enemy);
        simulation.RegisterEnemy(enemy);
    }

    private LaneRuntimeState? FindLaneRuntime(string laneId)
    {
        for (var index = 0; index < _lanes.Count; index++)
        {
            if (string.Equals(_lanes[index].Definition.LaneId, laneId, StringComparison.OrdinalIgnoreCase))
            {
                return _lanes[index];
            }
        }

        return null;
    }
}
