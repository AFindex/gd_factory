using Godot;
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

        FactoryEnemyActor enemy = rule.EnemyTypeId switch
        {
            "ranged" => new RangedRaiderEnemy(),
            "world-brute" => new WorldBruteEnemy(),
            "world-siege" => new WorldSiegeEnemy(),
            _ => new MeleeRaiderEnemy()
        };

        enemy.Name = $"{lane.LaneId}_{rule.EnemyTypeId}_{_spawnCounter++}";
        enemy.Configure(enemy.Name, lane.PathPoints);
        _enemyRoot.AddChild(enemy);
        simulation.RegisterEnemy(enemy);
    }
}
