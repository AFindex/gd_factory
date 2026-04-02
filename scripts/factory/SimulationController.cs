using Godot;
using System.Diagnostics;
using System.Collections.Generic;

public partial class SimulationController : Node
{
    private readonly List<FactoryStructure> _structures = new();
    private readonly List<IFactoryCombatSystem> _combatSystems = new();
    private readonly List<FactoryEnemyActor> _hostiles = new();
    private readonly HashSet<FactoryStructure> _destroyedStructures = new();
    private readonly HashSet<FactoryEnemyActor> _defeatedHostiles = new();
    private double _accumulator;
    private double _averageStepMilliseconds;
    private double _lastTopologyRebuildMilliseconds;
    private int _nextItemId = 1;
    private int _activeTransportItemCount;

    public GridManager? WorldGrid { get; private set; }

    public float TickAlpha => (float)(_accumulator / FactoryConstants.SimulationStepSeconds);
    public int RegisteredStructureCount => _structures.Count;
    public int ActiveTransportItemCount => _activeTransportItemCount;
    public double AverageStepMilliseconds => _averageStepMilliseconds;
    public double LastTopologyRebuildMilliseconds => _lastTopologyRebuildMilliseconds;
    public int ActiveEnemyCount => _hostiles.Count;
    public int DestroyedStructureCount { get; private set; }
    public int DefeatedEnemyCount { get; private set; }

    public void Configure(GridManager grid)
    {
        WorldGrid = grid;
    }

    public void RegisterStructure(FactoryStructure structure)
    {
        if (!_structures.Contains(structure))
        {
            _structures.Add(structure);
        }
    }

    public void UnregisterStructure(FactoryStructure structure)
    {
        _structures.Remove(structure);
    }

    public FactoryItem CreateItem(BuildPrototypeKind sourceKind, FactoryItemKind itemKind = FactoryItemKind.GenericCargo)
    {
        return new FactoryItem(_nextItemId++, sourceKind, itemKind);
    }

    public void RegisterCombatSystem(IFactoryCombatSystem combatSystem)
    {
        if (!_combatSystems.Contains(combatSystem))
        {
            _combatSystems.Add(combatSystem);
        }
    }

    public void UnregisterCombatSystem(IFactoryCombatSystem combatSystem)
    {
        _combatSystems.Remove(combatSystem);
    }

    public void RegisterEnemy(FactoryEnemyActor hostile)
    {
        if (!_hostiles.Contains(hostile))
        {
            _hostiles.Add(hostile);
        }
    }

    public void UnregisterEnemy(FactoryEnemyActor hostile)
    {
        _hostiles.Remove(hostile);
        _defeatedHostiles.Remove(hostile);
    }

    public void QueueStructureDestruction(FactoryStructure structure)
    {
        if (structure.IsDestroyed)
        {
            _destroyedStructures.Add(structure);
        }
    }

    public void QueueEnemyRemoval(FactoryEnemyActor hostile)
    {
        _defeatedHostiles.Add(hostile);
    }

    public FactoryEnemyActor? FindClosestEnemy(Vector3 worldPosition, float range)
    {
        FactoryEnemyActor? closest = null;
        var bestDistanceSquared = range * range;

        for (var i = 0; i < _hostiles.Count; i++)
        {
            var hostile = _hostiles[i];
            if (!GodotObject.IsInstanceValid(hostile) || hostile.IsDefeated)
            {
                continue;
            }

            var distanceSquared = worldPosition.DistanceSquaredTo(hostile.GlobalPosition);
            if (distanceSquared > bestDistanceSquared)
            {
                continue;
            }

            bestDistanceSquared = distanceSquared;
            closest = hostile;
        }

        return closest;
    }

    public FactoryStructure? FindNearestAttackableStructure(Vector3 worldPosition, float range, IReadOnlyCollection<BuildPrototypeKind>? preferredKinds = null)
    {
        FactoryStructure? preferred = null;
        FactoryStructure? fallback = null;
        var maxDistanceSquared = range * range;
        var bestPreferred = maxDistanceSquared;
        var bestFallback = maxDistanceSquared;

        for (var i = 0; i < _structures.Count; i++)
        {
            var structure = _structures[i];
            if (structure.IsDestroyed || structure.Site != WorldGrid)
            {
                continue;
            }

            var distanceSquared = worldPosition.DistanceSquaredTo(structure.GlobalPosition);
            if (distanceSquared > maxDistanceSquared)
            {
                continue;
            }

            var isPreferredKind = false;
            if (preferredKinds is not null)
            {
                foreach (var preferredKind in preferredKinds)
                {
                    if (preferredKind == structure.Kind)
                    {
                        isPreferredKind = true;
                        break;
                    }
                }
            }

            if (isPreferredKind)
            {
                if (distanceSquared < bestPreferred)
                {
                    bestPreferred = distanceSquared;
                    preferred = structure;
                }

                continue;
            }

            if (distanceSquared < bestFallback)
            {
                bestFallback = distanceSquared;
                fallback = structure;
            }
        }

        return preferred ?? fallback;
    }

    public bool TrySendItem(FactoryStructure source, Vector2I targetCell, FactoryItem item)
    {
        return source.Site.TrySendItem(source, targetCell, item, this);
    }

    public bool TryPeekProvidedItem(IFactorySite site, Vector2I providerCell, Vector2I requesterCell, out FactoryItem? item)
    {
        item = null;

        if (!site.TryGetStructure(providerCell, out var structure) || structure is not IFactoryItemProvider provider)
        {
            return false;
        }

        return provider.TryPeekProvidedItem(requesterCell, this, out item);
    }

    public bool TryTakeProvidedItem(IFactorySite site, Vector2I providerCell, Vector2I requesterCell, out FactoryItem? item)
    {
        item = null;

        if (!site.TryGetStructure(providerCell, out var structure) || structure is not IFactoryItemProvider provider)
        {
            return false;
        }

        return provider.TryTakeProvidedItem(requesterCell, this, out item);
    }

    public bool CanReceiveProvidedItem(FactoryStructure source, IFactorySite targetSite, Vector2I targetCell, FactoryItem item)
    {
        if (!targetSite.TryGetStructure(targetCell, out var structure) || structure is not IFactoryItemReceiver receiver)
        {
            return false;
        }

        return receiver.CanReceiveProvidedItem(item, source.Cell, this);
    }

    public bool TryReceiveProvidedItem(FactoryStructure source, IFactorySite targetSite, Vector2I targetCell, FactoryItem item)
    {
        if (!targetSite.TryGetStructure(targetCell, out var structure) || structure is not IFactoryItemReceiver receiver)
        {
            return false;
        }

        return receiver.TryReceiveProvidedItem(item, source.Cell, this);
    }

    public bool TrySendItemToSite(FactoryStructure source, Vector2I sourceCell, IFactorySite targetSite, Vector2I targetCell, FactoryItem item)
    {
        if (!targetSite.TryGetStructure(targetCell, out var structure) || structure is null)
        {
            return false;
        }

        return structure.TryAcceptItem(item, sourceCell, this);
    }

    public void RebuildTopology()
    {
        var startTicks = Stopwatch.GetTimestamp();
        for (var i = 0; i < _structures.Count; i++)
        {
            if (_structures[i] is IFactoryTopologyAware topologyAware)
            {
                topologyAware.RefreshTopology();
            }
        }

        _lastTopologyRebuildMilliseconds = Stopwatch.GetElapsedTime(startTicks).TotalMilliseconds;
    }

    public override void _PhysicsProcess(double delta)
    {
        _accumulator += delta;

        while (_accumulator >= FactoryConstants.SimulationStepSeconds)
        {
            var stepStartTicks = Stopwatch.GetTimestamp();
            for (var i = 0; i < _structures.Count; i++)
            {
                if (_structures[i].IsDestroyed || !_structures[i].Site.IsSimulationActive)
                {
                    continue;
                }

                _structures[i].AdvanceCombatState(FactoryConstants.SimulationStepSeconds);
                _structures[i].SimulationStep(this, FactoryConstants.SimulationStepSeconds);
            }

            for (var i = 0; i < _combatSystems.Count; i++)
            {
                _combatSystems[i].SimulationStep(this, FactoryConstants.SimulationStepSeconds);
            }

            for (var i = 0; i < _hostiles.Count; i++)
            {
                if (!GodotObject.IsInstanceValid(_hostiles[i]) || _hostiles[i].IsDefeated)
                {
                    continue;
                }

                _hostiles[i].SimulationStep(this, FactoryConstants.SimulationStepSeconds);
            }

            ProcessQueuedCombatCleanup();
            _accumulator -= FactoryConstants.SimulationStepSeconds;
            _activeTransportItemCount = CountTransitItems();
            _averageStepMilliseconds = SmoothMetric(_averageStepMilliseconds, Stopwatch.GetElapsedTime(stepStartTicks).TotalMilliseconds, 0.18);
        }
    }

    private void ProcessQueuedCombatCleanup()
    {
        if (_destroyedStructures.Count > 0)
        {
            var destroyedSnapshot = new List<FactoryStructure>(_destroyedStructures);
            _destroyedStructures.Clear();

            for (var i = 0; i < destroyedSnapshot.Count; i++)
            {
                var structure = destroyedSnapshot[i];
                if (!GodotObject.IsInstanceValid(structure))
                {
                    continue;
                }

                UnregisterStructure(structure);
                structure.Site.RemoveStructure(structure);
                structure.QueueFree();
                DestroyedStructureCount++;
            }

            RebuildTopology();
        }

        if (_defeatedHostiles.Count > 0)
        {
            var hostileSnapshot = new List<FactoryEnemyActor>(_defeatedHostiles);
            _defeatedHostiles.Clear();

            for (var i = 0; i < hostileSnapshot.Count; i++)
            {
                var hostile = hostileSnapshot[i];
                if (!GodotObject.IsInstanceValid(hostile))
                {
                    continue;
                }

                UnregisterEnemy(hostile);
                hostile.QueueFree();
                DefeatedEnemyCount++;
            }
        }
    }

    private int CountTransitItems()
    {
        var total = 0;
        for (var i = 0; i < _structures.Count; i++)
        {
            if (_structures[i] is FlowTransportStructure transport)
            {
                total += transport.TransitItemCount;
            }
        }

        return total;
    }

    private static double SmoothMetric(double current, double sample, double weight)
    {
        return current <= 0.0
            ? sample
            : current + ((sample - current) * weight);
    }
}
