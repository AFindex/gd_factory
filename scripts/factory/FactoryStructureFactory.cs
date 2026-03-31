using Godot;

public static class FactoryStructureFactory
{
    public static FactoryStructure Create(BuildPrototypeKind kind, Vector2I cell, FacingDirection facing, GridManager grid)
    {
        FactoryStructure structure = kind switch
        {
            BuildPrototypeKind.Producer => new ProducerStructure(),
            BuildPrototypeKind.Belt => new BeltStructure(),
            BuildPrototypeKind.Sink => new SinkStructure(),
            _ => new BeltStructure()
        };

        structure.Configure(cell, facing, grid.CellToWorld(cell), grid.CellSize);
        return structure;
    }
}
