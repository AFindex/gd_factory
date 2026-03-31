using Godot;
using System.Collections.Generic;

public partial class SimulationController : Node
{
    private readonly List<FactoryStructure> _structures = new();
    private double _accumulator;
    private int _nextItemId = 1;

    public GridManager? WorldGrid { get; private set; }

    public float TickAlpha => (float)(_accumulator / FactoryConstants.SimulationStepSeconds);

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
        for (var i = 0; i < _structures.Count; i++)
        {
            if (_structures[i] is IFactoryTopologyAware topologyAware)
            {
                topologyAware.RefreshTopology();
            }
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        _accumulator += delta;

        while (_accumulator >= FactoryConstants.SimulationStepSeconds)
        {
            for (var i = 0; i < _structures.Count; i++)
            {
                if (!_structures[i].Site.IsSimulationActive)
                {
                    continue;
                }

                _structures[i].SimulationStep(this, FactoryConstants.SimulationStepSeconds);
            }

            _accumulator -= FactoryConstants.SimulationStepSeconds;
        }
    }
}
