using Godot;
using System.Diagnostics;
using System.Collections.Generic;

public partial class SimulationController : Node
{
    private readonly List<FactoryStructure> _structures = new();
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

    public FactoryItem CreateItem(BuildPrototypeKind sourceKind)
    {
        return new FactoryItem(_nextItemId++, sourceKind);
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
                if (!_structures[i].Site.IsSimulationActive)
                {
                    continue;
                }

                _structures[i].SimulationStep(this, FactoryConstants.SimulationStepSeconds);
            }

            _accumulator -= FactoryConstants.SimulationStepSeconds;
            _activeTransportItemCount = CountTransitItems();
            _averageStepMilliseconds = SmoothMetric(_averageStepMilliseconds, Stopwatch.GetElapsedTime(stepStartTicks).TotalMilliseconds, 0.18);
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
