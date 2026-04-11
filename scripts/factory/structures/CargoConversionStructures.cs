using Godot;
using System.Collections.Generic;

public abstract partial class FactoryCargoConverterStructure : FactoryStructure, IFactoryItemProvider, IFactoryItemReceiver
{
    private readonly FactoryItemBuffer _inputBuffer = new(4);
    private readonly FactoryItemBuffer _outputBuffer = new(4);
    private double _dispatchCooldown;
    private double _processProgress;
    private FactoryItem? _processingItem;

    protected abstract FactoryCargoForm OutputCargoForm { get; }
    protected virtual float ProcessSeconds => 0.9f;
    protected virtual float DispatchCooldownSeconds => 0.18f;

    public override bool CanReceiveFrom(Vector2I sourceCell)
    {
        return sourceCell == GetInputCell();
    }

    public override bool CanOutputTo(Vector2I targetCell)
    {
        return targetCell == GetOutputCell();
    }

    public bool TryPeekProvidedItem(Vector2I requesterCell, SimulationController simulation, out FactoryItem? item)
    {
        item = null;
        return CanOutputTo(requesterCell) && _outputBuffer.TryPeek(out item);
    }

    public bool TryTakeProvidedItem(Vector2I requesterCell, SimulationController simulation, out FactoryItem? item)
    {
        item = null;
        if (!CanOutputTo(requesterCell))
        {
            return false;
        }

        return _outputBuffer.TryDequeue(out item);
    }

    public bool CanReceiveProvidedItem(FactoryItem item, Vector2I sourceCell, SimulationController simulation)
    {
        return CanAcceptConversionInput(item, sourceCell);
    }

    public bool TryReceiveProvidedItem(FactoryItem item, Vector2I sourceCell, SimulationController simulation)
    {
        return CanAcceptConversionInput(item, sourceCell) && _inputBuffer.TryEnqueue(item);
    }

    public override bool CanAcceptItem(FactoryItem item, Vector2I sourceCell, SimulationController simulation)
    {
        return CanAcceptConversionInput(item, sourceCell);
    }

    public override bool TryAcceptItem(FactoryItem item, Vector2I sourceCell, SimulationController simulation)
    {
        return CanAcceptConversionInput(item, sourceCell) && _inputBuffer.TryEnqueue(item);
    }

    public override void SimulationStep(SimulationController simulation, double stepSeconds)
    {
        _dispatchCooldown = Mathf.Max(0.0, (float)(_dispatchCooldown - stepSeconds));
        TryDispatchBufferedOutput(simulation);

        if (_processingItem is not null)
        {
            _processProgress += stepSeconds;
            if (_processProgress >= ProcessSeconds && !_outputBuffer.IsFull)
            {
                var output = simulation.CreateItem(Site, Kind, _processingItem.ItemKind, OutputCargoForm);
                _outputBuffer.TryEnqueue(output);
                _processingItem = null;
                _processProgress = 0.0;
            }

            return;
        }

        if (_inputBuffer.TryDequeue(out var input) && input is not null)
        {
            _processingItem = input;
            _processProgress = 0.0;
        }
    }

    public override IEnumerable<string> GetInspectionLines()
    {
        foreach (var line in base.GetInspectionLines())
        {
            yield return line;
        }

        yield return $"输入缓存：{_inputBuffer.Count}/4";
        yield return $"输出缓存：{_outputBuffer.Count}/4";
        yield return _processingItem is null
            ? "状态：待机"
            : $"状态：转换中 {Mathf.Clamp((float)(_processProgress / ProcessSeconds), 0.0f, 1.0f) * 100.0f:0}%";
    }

    protected virtual bool CanAcceptConversionInput(FactoryItem item, Vector2I sourceCell)
    {
        return CanReceiveFrom(sourceCell)
            && !_inputBuffer.IsFull
            && FactoryCargoRules.StructureAcceptsItem(Kind, FactoryIndustrialStandards.ResolveSiteKind(Site), item);
    }

    protected virtual void TryDispatchBufferedOutput(SimulationController simulation)
    {
        if (_dispatchCooldown > 0.0 || !_outputBuffer.TryPeek(out var item) || item is null)
        {
            return;
        }

        if (simulation.TrySendItem(this, GetOutputCell(), item))
        {
            _outputBuffer.TryDequeue(out _);
            _dispatchCooldown = DispatchCooldownSeconds;
        }
    }
}

public partial class CargoUnpackerStructure : FactoryCargoConverterStructure
{
    public override BuildPrototypeKind Kind => BuildPrototypeKind.CargoUnpacker;
    public override string Description => "把世界散装/封装货物转成内部供料单元，供舱内模块继续处理。";
    protected override FactoryCargoForm OutputCargoForm => FactoryCargoForm.InteriorFeed;

    protected override void BuildVisuals()
    {
        if (SiteKind == FactorySiteKind.Interior)
        {
            CreateInteriorModuleShell(this, "Unpacker", new Vector3(CellSize * 1.42f, 0.44f, CellSize * 0.94f), new Color("164E63"), new Color("7DD3FC"), new Vector3(0.0f, 0.30f, 0.0f));
            CreateInteriorTray(this, "UnpackerFeed", new Vector3(CellSize * 1.52f, 0.08f, CellSize * 0.18f), new Color("0369A1"), new Color("DBEAFE"), new Vector3(0.0f, 0.16f, 0.0f));
            CreateBox("UnpackerLatch", new Vector3(CellSize * 0.26f, 0.30f, CellSize * 0.42f), new Color("E0F2FE"), new Vector3(CellSize * 0.48f, 0.34f, 0.0f));
            CreateInteriorIndicatorLight(this, "UnpackerLamp", new Color("7DD3FC"), new Vector3(-CellSize * 0.46f, 0.50f, 0.0f), CellSize * 0.08f);
            return;
        }

        CreateBox("Deck", new Vector3(CellSize * 1.8f, 0.16f, CellSize * 1.2f), new Color("164E63"), new Vector3(0.0f, 0.08f, 0.0f));
        CreateBox("Chamber", new Vector3(CellSize * 1.1f, 0.48f, CellSize * 0.78f), new Color("38BDF8"), new Vector3(-CellSize * 0.18f, 0.32f, 0.0f));
        CreateBox("FeedRail", new Vector3(CellSize * 1.9f, 0.08f, CellSize * 0.18f), new Color("BAE6FD"), new Vector3(0.0f, 0.22f, 0.0f));
        CreateBox("ServicePanel", new Vector3(CellSize * 0.34f, 0.28f, CellSize * 0.54f), new Color("E0F2FE"), new Vector3(CellSize * 0.58f, 0.34f, 0.0f));
    }
}

public partial class CargoPackerStructure : FactoryCargoConverterStructure
{
    public override BuildPrototypeKind Kind => BuildPrototypeKind.CargoPacker;
    public override string Description => "把散装原料或内部供料重新压成世界标准封装货物，便于跨站点运输。";
    protected override FactoryCargoForm OutputCargoForm => FactoryCargoForm.WorldPacked;

    protected override void BuildVisuals()
    {
        if (SiteKind == FactorySiteKind.Interior)
        {
            CreateInteriorModuleShell(this, "Packer", new Vector3(CellSize * 1.42f, 0.44f, CellSize * 0.94f), new Color("7C2D12"), new Color("FDBA74"), new Vector3(0.0f, 0.30f, 0.0f));
            CreateInteriorTray(this, "PackerFeed", new Vector3(CellSize * 1.52f, 0.08f, CellSize * 0.18f), new Color("EA580C"), new Color("FED7AA"), new Vector3(0.0f, 0.16f, 0.0f));
            CreateBox("PackerClamp", new Vector3(CellSize * 0.24f, 0.30f, CellSize * 0.62f), new Color("FED7AA"), new Vector3(CellSize * 0.46f, 0.32f, 0.0f));
            CreateInteriorIndicatorLight(this, "PackerLamp", new Color("FB923C"), new Vector3(-CellSize * 0.46f, 0.50f, 0.0f), CellSize * 0.08f);
            return;
        }

        CreateBox("Deck", new Vector3(CellSize * 1.8f, 0.16f, CellSize * 1.2f), new Color("7C2D12"), new Vector3(0.0f, 0.08f, 0.0f));
        CreateBox("Compressor", new Vector3(CellSize * 1.0f, 0.46f, CellSize * 0.84f), new Color("F97316"), new Vector3(-CellSize * 0.10f, 0.32f, 0.0f));
        CreateBox("Clamp", new Vector3(CellSize * 0.28f, 0.34f, CellSize * 0.72f), new Color("FDBA74"), new Vector3(CellSize * 0.56f, 0.30f, 0.0f));
        CreateBox("FeedRail", new Vector3(CellSize * 1.9f, 0.08f, CellSize * 0.18f), new Color("FED7AA"), new Vector3(0.0f, 0.22f, 0.0f));
    }
}

public partial class TransferBufferStructure : FactoryStructure, IFactoryItemProvider, IFactoryItemReceiver
{
    private readonly FactoryItemBuffer _buffer = new(8);
    private double _dispatchCooldown;

    public override BuildPrototypeKind Kind => BuildPrototypeKind.TransferBuffer;
    public override string Description => "舱内中转缓冲槽，负责在维护通路旁暂存标准化货物。";

    public override bool CanReceiveFrom(Vector2I sourceCell)
    {
        return sourceCell == GetInputCell();
    }

    public override bool CanOutputTo(Vector2I targetCell)
    {
        return targetCell == GetOutputCell();
    }

    public bool TryPeekProvidedItem(Vector2I requesterCell, SimulationController simulation, out FactoryItem? item)
    {
        item = null;
        return CanOutputTo(requesterCell) && _buffer.TryPeek(out item);
    }

    public bool TryTakeProvidedItem(Vector2I requesterCell, SimulationController simulation, out FactoryItem? item)
    {
        item = null;
        return CanOutputTo(requesterCell) && _buffer.TryDequeue(out item);
    }

    public bool CanReceiveProvidedItem(FactoryItem item, Vector2I sourceCell, SimulationController simulation)
    {
        return CanAcceptBufferedItem(item, sourceCell);
    }

    public bool TryReceiveProvidedItem(FactoryItem item, Vector2I sourceCell, SimulationController simulation)
    {
        return CanAcceptBufferedItem(item, sourceCell) && _buffer.TryEnqueue(item);
    }

    public override bool CanAcceptItem(FactoryItem item, Vector2I sourceCell, SimulationController simulation)
    {
        return CanAcceptBufferedItem(item, sourceCell);
    }

    public override bool TryAcceptItem(FactoryItem item, Vector2I sourceCell, SimulationController simulation)
    {
        return CanAcceptBufferedItem(item, sourceCell) && _buffer.TryEnqueue(item);
    }

    public override void SimulationStep(SimulationController simulation, double stepSeconds)
    {
        _dispatchCooldown = Mathf.Max(0.0, (float)(_dispatchCooldown - stepSeconds));
        if (_dispatchCooldown > 0.0 || !_buffer.TryPeek(out var item) || item is null)
        {
            return;
        }

        if (simulation.TrySendItem(this, GetOutputCell(), item))
        {
            _buffer.TryDequeue(out _);
            _dispatchCooldown = 0.16f;
        }
    }

    public override IEnumerable<string> GetInspectionLines()
    {
        foreach (var line in base.GetInspectionLines())
        {
            yield return line;
        }

        yield return $"缓冲量：{_buffer.Count}/8";
    }

    protected override void BuildVisuals()
    {
        if (SiteKind == FactorySiteKind.Interior)
        {
            CreateBox("BufferWell", new Vector3(CellSize * 0.84f, 0.12f, CellSize * 0.84f), new Color("042F2E"), new Vector3(0.0f, 0.06f, 0.0f));
            CreateInteriorTray(this, "BufferDrawer", new Vector3(CellSize * 0.56f, 0.16f, CellSize * 0.50f), new Color("0F766E"), new Color("99F6E4"), new Vector3(0.0f, 0.18f, 0.0f));
            CreateBox("BufferHandle", new Vector3(CellSize * 0.10f, 0.08f, CellSize * 0.46f), new Color("CCFBF1"), new Vector3(CellSize * 0.24f, 0.24f, 0.0f));
            CreateInteriorIndicatorLight(this, "BufferLamp", new Color("5EEAD4"), new Vector3(-CellSize * 0.24f, 0.26f, 0.0f), CellSize * 0.07f);
            return;
        }

        CreateBox("Trench", new Vector3(CellSize * 0.84f, 0.12f, CellSize * 0.84f), new Color("0F766E"), new Vector3(0.0f, 0.06f, 0.0f));
        CreateBox("Tray", new Vector3(CellSize * 0.56f, 0.14f, CellSize * 0.56f), new Color("14B8A6"), new Vector3(0.0f, 0.18f, 0.0f));
        CreateBox("PanelNorth", new Vector3(CellSize * 0.64f, 0.10f, CellSize * 0.10f), new Color("99F6E4"), new Vector3(0.0f, 0.26f, -CellSize * 0.24f));
        CreateBox("PanelSouth", new Vector3(CellSize * 0.64f, 0.10f, CellSize * 0.10f), new Color("99F6E4"), new Vector3(0.0f, 0.26f, CellSize * 0.24f));
    }

    private bool CanAcceptBufferedItem(FactoryItem item, Vector2I sourceCell)
    {
        return CanReceiveFrom(sourceCell)
            && !_buffer.IsFull
            && FactoryCargoRules.StructureAcceptsItem(Kind, FactoryIndustrialStandards.ResolveSiteKind(Site), item);
    }
}
