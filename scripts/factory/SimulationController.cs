using Godot;
using System.Diagnostics;
using System.Collections.Generic;

public partial class SimulationController : Node
{
    private sealed class PowerNodeRuntime
    {
        public required FactoryStructure Structure { get; init; }
        public required IFactoryPowerNode Node { get; init; }
    }

    private sealed class PowerNetworkRuntime
    {
        public PowerNetworkRuntime(int id)
        {
            Id = id;
        }

        public int Id { get; }
        public List<PowerNodeRuntime> Nodes { get; } = new();
        public float Supply { get; set; }
        public float Demand { get; set; }
        public float Satisfaction { get; set; }
        public bool HasProducer { get; set; }
    }

    private readonly List<FactoryStructure> _structures = new();
    private readonly List<IFactoryCombatSystem> _combatSystems = new();
    private readonly List<FactoryEnemyActor> _hostiles = new();
    private readonly List<FactoryCombatProjectile> _projectiles = new();
    private readonly HashSet<FactoryStructure> _destroyedStructures = new();
    private readonly HashSet<FactoryEnemyActor> _defeatedHostiles = new();
    private readonly HashSet<FactoryCombatProjectile> _expiredProjectiles = new();
    private readonly List<PowerNetworkRuntime> _powerNetworks = new();
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
    public int ActiveProjectileCount => _projectiles.Count;
    public int TotalProjectileLaunchCount { get; private set; }
    public int DestroyedStructureCount { get; private set; }
    public int DefeatedEnemyCount { get; private set; }
    public int NextItemId => _nextItemId;

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
        return CreateItem(FactorySiteKind.World, sourceKind, itemKind);
    }

    public FactoryItem CreateItem(
        IFactorySite site,
        BuildPrototypeKind sourceKind,
        FactoryItemKind itemKind = FactoryItemKind.GenericCargo,
        FactoryCargoForm? cargoForm = null,
        string? bundleTemplateId = null,
        IReadOnlyDictionary<FactoryItemKind, int>? bundleContents = null)
    {
        return CreateItem(FactoryIndustrialStandards.ResolveSiteKind(site), sourceKind, itemKind, cargoForm, bundleTemplateId, bundleContents);
    }

    public FactoryItem CreateItem(
        FactorySiteKind siteKind,
        BuildPrototypeKind sourceKind,
        FactoryItemKind itemKind = FactoryItemKind.GenericCargo,
        FactoryCargoForm? cargoForm = null,
        string? bundleTemplateId = null,
        IReadOnlyDictionary<FactoryItemKind, int>? bundleContents = null)
    {
        return new FactoryItem(
            _nextItemId++,
            sourceKind,
            itemKind,
            FactoryCargoRules.ResolveProducedCargoForm(siteKind, sourceKind, itemKind, cargoForm),
            bundleTemplateId,
            bundleContents);
    }

    public FactoryItem CreateItemWithId(int id, BuildPrototypeKind sourceKind, FactoryItemKind itemKind = FactoryItemKind.GenericCargo)
    {
        return CreateItemWithId(id, sourceKind, itemKind, FactoryCargoRules.ResolveProducedCargoForm(FactorySiteKind.World, sourceKind, itemKind));
    }

    public FactoryItem CreateItemWithId(
        int id,
        BuildPrototypeKind sourceKind,
        FactoryItemKind itemKind,
        FactoryCargoForm cargoForm,
        string? bundleTemplateId = null,
        IReadOnlyDictionary<FactoryItemKind, int>? bundleContents = null)
    {
        EnsureNextItemId(id + 1);
        return new FactoryItem(id, sourceKind, itemKind, cargoForm, bundleTemplateId, bundleContents);
    }

    public void EnsureNextItemId(int nextId)
    {
        _nextItemId = Mathf.Max(_nextItemId, nextId);
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

    public IReadOnlyList<FactoryEnemyActor> SnapshotActiveEnemies()
    {
        return new List<FactoryEnemyActor>(_hostiles);
    }

    public void ClearCombatActors()
    {
        var hostiles = new List<FactoryEnemyActor>(_hostiles);
        _hostiles.Clear();
        _defeatedHostiles.Clear();
        for (var index = 0; index < hostiles.Count; index++)
        {
            if (GodotObject.IsInstanceValid(hostiles[index]))
            {
                hostiles[index].QueueFree();
            }
        }

        var projectiles = new List<FactoryCombatProjectile>(_projectiles);
        _projectiles.Clear();
        _expiredProjectiles.Clear();
        for (var index = 0; index < projectiles.Count; index++)
        {
            if (GodotObject.IsInstanceValid(projectiles[index]))
            {
                projectiles[index].QueueFree();
            }
        }
    }

    public void RestoreRuntimeCounters(int destroyedStructureCount, int defeatedEnemyCount, int totalProjectileLaunchCount)
    {
        DestroyedStructureCount = Mathf.Max(0, destroyedStructureCount);
        DefeatedEnemyCount = Mathf.Max(0, defeatedEnemyCount);
        TotalProjectileLaunchCount = Mathf.Max(0, totalProjectileLaunchCount);
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

    public void RegisterProjectile(FactoryCombatProjectile projectile)
    {
        if (!_projectiles.Contains(projectile))
        {
            AddChild(projectile);
            _projectiles.Add(projectile);
            TotalProjectileLaunchCount++;
        }
    }

    public void QueueProjectileRemoval(FactoryCombatProjectile projectile)
    {
        _expiredProjectiles.Add(projectile);
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
            if (structure.IsDestroyed || !structure.Site.IsVisible || !structure.Site.IsSimulationActive)
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
        return TrySendItem(source, source.Cell, targetCell, item);
    }

    public bool TrySendItem(FactoryStructure source, Vector2I sourceCell, Vector2I targetCell, FactoryItem item)
    {
        return source.Site.TrySendItem(source, sourceCell, targetCell, item, this);
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

        RebuildPowerTopology();
        _lastTopologyRebuildMilliseconds = Stopwatch.GetElapsedTime(startTicks).TotalMilliseconds;
    }

    public override void _PhysicsProcess(double delta)
    {
        _accumulator += delta;
        var stepsProcessed = 0;

        while (_accumulator >= FactoryConstants.SimulationStepSeconds
            && stepsProcessed < FactoryConstants.MaxSimulationStepsPerPhysicsFrame)
        {
            var stepStartTicks = Stopwatch.GetTimestamp();
            ApplyPowerStates();
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

            for (var i = 0; i < _projectiles.Count; i++)
            {
                if (!GodotObject.IsInstanceValid(_projectiles[i]) || _projectiles[i].IsExpired)
                {
                    continue;
                }

                _projectiles[i].SimulationStep(this, FactoryConstants.SimulationStepSeconds);
            }

            ProcessQueuedCombatCleanup();
            _accumulator -= FactoryConstants.SimulationStepSeconds;
            _activeTransportItemCount = CountTransitItems();
            _averageStepMilliseconds = SmoothMetric(_averageStepMilliseconds, Stopwatch.GetElapsedTime(stepStartTicks).TotalMilliseconds, 0.18);
            stepsProcessed++;
        }

        if (_accumulator > FactoryConstants.MaxSimulationAccumulatorSeconds)
        {
            _accumulator = FactoryConstants.MaxSimulationAccumulatorSeconds;
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
                if (structure.Site is MobileFactorySite mobileSite)
                {
                    mobileSite.Owner.HandleDestroyedInteriorStructure(structure, rebuildTopology: false);
                }

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

        if (_expiredProjectiles.Count > 0)
        {
            var projectileSnapshot = new List<FactoryCombatProjectile>(_expiredProjectiles);
            _expiredProjectiles.Clear();
            for (var i = 0; i < projectileSnapshot.Count; i++)
            {
                var projectile = projectileSnapshot[i];
                if (!GodotObject.IsInstanceValid(projectile))
                {
                    continue;
                }

                _projectiles.Remove(projectile);
                projectile.QueueFree();
            }
        }
    }

    private int CountTransitItems()
    {
        var total = 0;
        for (var i = 0; i < _structures.Count; i++)
        {
            if (_structures[i] is MobileFactoryBoundaryAttachmentStructure attachment)
            {
                total += attachment.StagedCargoCount;
                continue;
            }

            if (_structures[i] is FlowTransportStructure transport)
            {
                total += transport.TransitItemCount;
            }
        }

        return total;
    }

    private void RebuildPowerTopology()
    {
        _powerNetworks.Clear();

        var nodes = new List<PowerNodeRuntime>();
        for (var i = 0; i < _structures.Count; i++)
        {
            var structure = _structures[i];
            if (structure.IsDestroyed || !structure.Site.IsSimulationActive || structure is not IFactoryPowerNode powerNode || powerNode.PowerConnectionRangeCells <= 0)
            {
                continue;
            }

            nodes.Add(new PowerNodeRuntime
            {
                Structure = structure,
                Node = powerNode
            });
        }

        var visited = new bool[nodes.Count];
        var networkId = 1;
        for (var i = 0; i < nodes.Count; i++)
        {
            if (visited[i])
            {
                continue;
            }

            var network = new PowerNetworkRuntime(networkId++);
            var queue = new Queue<int>();
            queue.Enqueue(i);
            visited[i] = true;

            while (queue.Count > 0)
            {
                var currentIndex = queue.Dequeue();
                var current = nodes[currentIndex];
                network.Nodes.Add(current);

                for (var candidateIndex = 0; candidateIndex < nodes.Count; candidateIndex++)
                {
                    if (visited[candidateIndex] || !ArePowerNodesConnected(current, nodes[candidateIndex]))
                    {
                        continue;
                    }

                    visited[candidateIndex] = true;
                    queue.Enqueue(candidateIndex);
                }
            }

            _powerNetworks.Add(network);
        }
    }

    private void ApplyPowerStates()
    {
        for (var i = 0; i < _structures.Count; i++)
        {
            if (_structures[i].IsDestroyed || !_structures[i].Site.IsSimulationActive)
            {
                continue;
            }

            if (_structures[i] is IFactoryPowerConsumer disconnectedConsumer)
            {
                disconnectedConsumer.SetPowerState(FactoryPowerStatus.Disconnected, 0.0f, -1);
            }
        }

        for (var i = 0; i < _powerNetworks.Count; i++)
        {
            var network = _powerNetworks[i];
            network.Supply = 0.0f;
            network.Demand = 0.0f;
            network.HasProducer = false;

            for (var nodeIndex = 0; nodeIndex < network.Nodes.Count; nodeIndex++)
            {
                var runtime = network.Nodes[nodeIndex];
                if (runtime.Node is IFactoryPowerProducer producer)
                {
                    network.Supply += producer.GetAvailablePower(this);
                    network.HasProducer = true;
                }
            }
        }

        var networkConsumers = new Dictionary<int, List<IFactoryPowerConsumer>>();
        for (var i = 0; i < _structures.Count; i++)
        {
            var structure = _structures[i];
            if (structure.IsDestroyed || !structure.Site.IsSimulationActive || structure is not IFactoryPowerConsumer consumer)
            {
                continue;
            }

            var connectedNetwork = FindConsumerPowerNetwork(structure);
            if (connectedNetwork is null)
            {
                continue;
            }

            if (consumer.WantsPower(this))
            {
                connectedNetwork.Demand += consumer.GetRequestedPower(this);
            }

            if (!networkConsumers.TryGetValue(connectedNetwork.Id, out var consumers))
            {
                consumers = new List<IFactoryPowerConsumer>();
                networkConsumers[connectedNetwork.Id] = consumers;
            }

            consumers.Add(consumer);
        }

        for (var i = 0; i < _powerNetworks.Count; i++)
        {
            var network = _powerNetworks[i];

            network.Satisfaction = network.Demand <= 0.001f
                ? (network.Supply > 0.001f ? 1.0f : 0.0f)
                : Mathf.Clamp(network.Supply / network.Demand, 0.0f, 1.0f);

            if (!networkConsumers.TryGetValue(network.Id, out var consumers))
            {
                continue;
            }

            var status = !network.HasProducer
                ? FactoryPowerStatus.Disconnected
                : network.Satisfaction >= 0.999f
                    ? FactoryPowerStatus.Powered
                    : FactoryPowerStatus.Underpowered;
            for (var consumerIndex = 0; consumerIndex < consumers.Count; consumerIndex++)
            {
                consumers[consumerIndex].SetPowerState(status, network.Satisfaction, network.Id);
            }
        }
    }

    private PowerNetworkRuntime? FindConsumerPowerNetwork(FactoryStructure consumerStructure)
    {
        PowerNetworkRuntime? bestNetwork = null;
        var bestDistance = float.MaxValue;
        var consumerCell = new Vector2(consumerStructure.Cell.X, consumerStructure.Cell.Y);

        for (var networkIndex = 0; networkIndex < _powerNetworks.Count; networkIndex++)
        {
            var network = _powerNetworks[networkIndex];
            for (var nodeIndex = 0; nodeIndex < network.Nodes.Count; nodeIndex++)
            {
                var runtime = network.Nodes[nodeIndex];
                if (runtime.Structure.Site != consumerStructure.Site)
                {
                    continue;
                }

                var nodeCell = new Vector2(runtime.Structure.Cell.X, runtime.Structure.Cell.Y);
                var distance = consumerCell.DistanceTo(nodeCell);
                if (distance > runtime.Node.PowerConnectionRangeCells || distance >= bestDistance)
                {
                    continue;
                }

                bestDistance = distance;
                bestNetwork = network;
            }
        }

        return bestNetwork;
    }

    private static bool ArePowerNodesConnected(PowerNodeRuntime a, PowerNodeRuntime b)
    {
        var maxDistance = a.Node.PowerConnectionRangeCells + b.Node.PowerConnectionRangeCells;
        var aCell = new Vector2(a.Structure.Cell.X, a.Structure.Cell.Y);
        var bCell = new Vector2(b.Structure.Cell.X, b.Structure.Cell.Y);
        return aCell.DistanceTo(bCell) <= maxDistance;
    }

    private static double SmoothMetric(double current, double sample, double weight)
    {
        return current <= 0.0
            ? sample
            : current + ((sample - current) * weight);
    }
}
