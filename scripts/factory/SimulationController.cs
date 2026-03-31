using Godot;
using System.Collections.Generic;

public partial class SimulationController : Node
{
    private readonly List<FactoryStructure> _structures = new();
    private double _accumulator;
    private int _nextItemId = 1;

    public GridManager? Grid { get; private set; }

    public float TickAlpha => (float)(_accumulator / FactoryConstants.SimulationStepSeconds);

    public void Configure(GridManager grid)
    {
        Grid = grid;
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

    public bool TrySendItemToCell(Vector2I sourceCell, Vector2I targetCell, FactoryItem item)
    {
        if (Grid is null || !Grid.TryGetStructure(targetCell, out var structure) || structure is null)
        {
            return false;
        }

        return structure.TryAcceptItem(item, sourceCell, this);
    }

    public override void _PhysicsProcess(double delta)
    {
        _accumulator += delta;

        while (_accumulator >= FactoryConstants.SimulationStepSeconds)
        {
            for (var i = 0; i < _structures.Count; i++)
            {
                _structures[i].SimulationStep(this, FactoryConstants.SimulationStepSeconds);
            }

            _accumulator -= FactoryConstants.SimulationStepSeconds;
        }
    }
}
