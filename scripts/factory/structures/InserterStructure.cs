using Godot;

public partial class InserterStructure : FactoryStructure
{
    private FactoryItem? _heldItem;
    private float _swingProgress;
    private bool _isReturning;
    private Node3D? _shoulderPivot;
    private Node3D? _elbowPivot;
    private MeshInstance3D? _claw;
    private MeshInstance3D? _heldVisual;

    public override BuildPrototypeKind Kind => BuildPrototypeKind.Inserter;
    public override string Description => "从后方抓取物品并向前方投送，适配传送带、仓储和其他缓冲结构。";

    public override void SimulationStep(SimulationController simulation, double stepSeconds)
    {
        var stepProgress = (float)(stepSeconds / FactoryConstants.InserterCycleSeconds);

        if (_heldItem is null && _isReturning)
        {
            _swingProgress = Mathf.Max(0.0f, _swingProgress - stepProgress);
            if (_swingProgress <= 0.001f)
            {
                _swingProgress = 0.0f;
                _isReturning = false;
            }

            return;
        }

        if (_heldItem is null)
        {
            if (!simulation.TryPeekProvidedItem(Site, GetInputCell(), Cell, out var previewItem)
                || previewItem is null
                || !simulation.CanReceiveProvidedItem(this, Site, GetOutputCell(), previewItem)
                || !simulation.TryTakeProvidedItem(Site, GetInputCell(), Cell, out var takenItem)
                || takenItem is null)
            {
                return;
            }

            _heldItem = takenItem;
            _swingProgress = 0.0f;
            _isReturning = false;
            if (_heldVisual is not null)
            {
                _heldVisual.Visible = true;
            }

            return;
        }

        _swingProgress = Mathf.Min(1.0f, _swingProgress + stepProgress);
        if (_swingProgress < 1.0f)
        {
            return;
        }

        if (!simulation.TryReceiveProvidedItem(this, Site, GetOutputCell(), _heldItem))
        {
            _swingProgress = 0.96f;
            return;
        }

        _heldItem = null;
        _isReturning = true;
        if (_heldVisual is not null)
        {
            _heldVisual.Visible = false;
        }
    }

    public override void UpdateVisuals(float tickAlpha)
    {
        UpdateArmPose(Mathf.Clamp(_swingProgress, 0.0f, 1.0f));

        if (_heldVisual is not null)
        {
            _heldVisual.Visible = _heldItem is not null;
        }
    }

    protected override void BuildVisuals()
    {
        CreateBox("Base", new Vector3(CellSize * 0.72f, 0.16f, CellSize * 0.72f), new Color("78350F"), new Vector3(0.0f, 0.08f, 0.0f));
        CreateBox("Column", new Vector3(CellSize * 0.18f, 0.72f, CellSize * 0.18f), new Color("A16207"), new Vector3(0.0f, 0.44f, 0.0f));
        CreateBox("InputMarker", new Vector3(CellSize * 0.16f, 0.05f, CellSize * 0.12f), new Color("FED7AA"), new Vector3(-CellSize * 0.28f, 0.16f, 0.0f));
        CreateBox("OutputMarker", new Vector3(CellSize * 0.16f, 0.05f, CellSize * 0.12f), new Color("FEF3C7"), new Vector3(CellSize * 0.28f, 0.16f, 0.0f));

        _shoulderPivot = new Node3D
        {
            Name = "ShoulderPivot",
            Position = new Vector3(0.0f, 0.82f, 0.0f)
        };
        AddChild(_shoulderPivot);

        CreateArmMesh(
            _shoulderPivot,
            "UpperArm",
            new Vector3(CellSize * 0.34f, 0.08f, 0.10f),
            new Color("D97706"),
            new Vector3(CellSize * 0.17f, 0.0f, 0.0f));

        _elbowPivot = new Node3D
        {
            Name = "ElbowPivot",
            Position = new Vector3(CellSize * 0.34f, 0.0f, 0.0f)
        };
        _shoulderPivot.AddChild(_elbowPivot);

        CreateArmMesh(
            _elbowPivot,
            "Forearm",
            new Vector3(CellSize * 0.30f, 0.08f, 0.09f),
            new Color("F59E0B"),
            new Vector3(CellSize * 0.15f, 0.0f, 0.0f));

        _claw = CreateArmMesh(
            _elbowPivot,
            "Claw",
            new Vector3(CellSize * 0.12f, 0.12f, 0.22f),
            new Color("FCD34D"),
            new Vector3(CellSize * 0.30f, 0.0f, 0.0f));

        _heldVisual = CreateArmMesh(
            _elbowPivot,
            "HeldItem",
            new Vector3(CellSize * 0.14f, CellSize * 0.14f, CellSize * 0.14f),
            new Color("FDE68A"),
            new Vector3(CellSize * 0.30f, CellSize * 0.10f, 0.0f));
        _heldVisual.Visible = false;

        UpdateArmPose(0.0f);
    }

    private void UpdateArmPose(float progress)
    {
        if (_shoulderPivot is null || _elbowPivot is null)
        {
            return;
        }

        var eased = Mathf.SmoothStep(0.0f, 1.0f, progress);
        var shoulderYaw = Mathf.Lerp(Mathf.Pi, 0.0f, eased);
        var elbowYaw = Mathf.Lerp(-0.85f, 0.85f, eased);
        var elbowLift = Mathf.Lerp(-0.32f, 0.18f, eased);

        _shoulderPivot.Rotation = new Vector3(0.0f, shoulderYaw, 0.0f);
        _elbowPivot.Rotation = new Vector3(elbowLift, elbowYaw, 0.0f);

        if (_claw is not null)
        {
            _claw.Rotation = new Vector3(0.0f, 0.0f, Mathf.Lerp(-0.35f, 0.2f, eased));
        }
    }

    private static MeshInstance3D CreateArmMesh(Node3D parent, string name, Vector3 size, Color color, Vector3 localPosition)
    {
        var mesh = new MeshInstance3D
        {
            Name = name,
            Mesh = new BoxMesh { Size = size },
            Position = localPosition,
            MaterialOverride = new StandardMaterial3D
            {
                AlbedoColor = color,
                Roughness = 0.82f
            }
        };
        parent.AddChild(mesh);
        return mesh;
    }
}
