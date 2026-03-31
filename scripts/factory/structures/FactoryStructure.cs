using Godot;

public abstract partial class FactoryStructure : Node3D
{
    private bool _visualsBuilt;

    protected float CellSize { get; private set; } = FactoryConstants.CellSize;

    public Vector2I Cell { get; private set; }
    public FacingDirection Facing { get; protected set; }

    public abstract BuildPrototypeKind Kind { get; }
    public abstract string Description { get; }
    public virtual bool IsTransportNode => false;

    public void Configure(Vector2I cell, FacingDirection facing, Vector3 worldPosition, float cellSize)
    {
        Cell = cell;
        Facing = facing;
        CellSize = cellSize;
        Position = worldPosition;
        Rotation = new Vector3(0.0f, FactoryDirection.ToYRotationRadians(facing), 0.0f);
    }

    public override void _Ready()
    {
        if (_visualsBuilt)
        {
            return;
        }

        _visualsBuilt = true;
        BuildVisuals();
    }

    public virtual void SimulationStep(SimulationController simulation, double stepSeconds)
    {
    }

    public virtual void UpdateVisuals(float tickAlpha)
    {
    }

    public virtual bool TryAcceptItem(FactoryItem item, Vector2I sourceCell, SimulationController simulation)
    {
        return false;
    }

    public Vector2I GetOutputCell()
    {
        return Cell + FactoryDirection.ToCellOffset(Facing);
    }

    public Vector2I GetInputCell()
    {
        return Cell - FactoryDirection.ToCellOffset(Facing);
    }

    public bool AcceptsFrom(Vector2I sourceCell)
    {
        return sourceCell == GetInputCell();
    }

    public virtual bool CanReceiveFrom(Vector2I sourceCell)
    {
        return sourceCell == GetInputCell();
    }

    public virtual bool CanOutputTo(Vector2I targetCell)
    {
        return GetOutputCell() == targetCell;
    }

    protected abstract void BuildVisuals();

    protected MeshInstance3D CreateBox(string name, Vector3 size, Color color, Vector3? localPosition = null)
    {
        var mesh = new MeshInstance3D();
        mesh.Name = name;
        mesh.Mesh = new BoxMesh { Size = size };

        var material = new StandardMaterial3D();
        material.AlbedoColor = color;
        material.Roughness = 0.85f;
        mesh.MaterialOverride = material;

        if (localPosition is not null)
        {
            mesh.Position = localPosition.Value;
        }

        AddChild(mesh);
        return mesh;
    }
}
