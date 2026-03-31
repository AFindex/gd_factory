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
            BuildPrototypeKind.Splitter => new SplitterStructure(),
            BuildPrototypeKind.Merger => new MergerStructure(),
            BuildPrototypeKind.Bridge => new BridgeStructure(),
            BuildPrototypeKind.Loader => new LoaderStructure(),
            BuildPrototypeKind.Unloader => new UnloaderStructure(),
            _ => new BeltStructure()
        };

        structure.Configure(cell, facing, grid.CellToWorld(cell), grid.CellSize);
        return structure;
    }
}
