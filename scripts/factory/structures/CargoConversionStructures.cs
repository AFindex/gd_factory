using Godot;
using System.Collections.Generic;

public abstract partial class FactoryCargoConverterStructure : FactoryStructure, IFactoryItemProvider, IFactoryItemReceiver
{
    private readonly FactoryItemBuffer _inputBuffer = new(1);
    private readonly FactoryItemBuffer _outputBuffer = new(1);
    private double _dispatchCooldown;
    private double _processProgress;
    private FactoryItem? _processingItem;

    private Node3D? _stagingPayloadVisual;
    private string _stagingPayloadVisualKey = string.Empty;
    private Node3D? _processingPayloadVisual;
    private string _processingPayloadVisualKey = string.Empty;
    private Node3D? _dispatchPayloadVisual;
    private string _dispatchPayloadVisualKey = string.Empty;

    protected abstract FactoryCargoForm OutputCargoForm { get; }
    protected virtual float ProcessSeconds => 1.25f;
    protected virtual float DispatchCooldownSeconds => 0.20f;
    protected virtual int ChamberCapacity => 1;
    protected FactoryItem? ProcessingItem => _processingItem;
    protected float ProcessingRatio => Mathf.Clamp((float)(_processProgress / ProcessSeconds), 0.0f, 1.0f);
    protected bool HasBufferedOutput => !_outputBuffer.IsEmpty;

    protected virtual FactoryTransportVisualContext StagingPayloadContext => FactoryTransportVisualContext.InteriorStaging;
    protected virtual FactoryTransportVisualContext ProcessingPayloadContext => FactoryTransportVisualContext.InteriorConversion;
    protected virtual FactoryTransportVisualContext DispatchPayloadContext => FactoryTransportVisualContext.InteriorRail;

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

    public override void UpdateVisuals(float tickAlpha)
    {
        SyncPayloadVisual(
            "StagingPayloadAnchor",
            ref _stagingPayloadVisual,
            ref _stagingPayloadVisualKey,
            GetDisplayedStagingItem(),
            StagingPayloadContext);
        SyncPayloadVisual(
            "ProcessingPayloadAnchor",
            ref _processingPayloadVisual,
            ref _processingPayloadVisualKey,
            GetDisplayedProcessingItem(),
            ProcessingPayloadContext);
        SyncPayloadVisual(
            "DispatchPayloadAnchor",
            ref _dispatchPayloadVisual,
            ref _dispatchPayloadVisualKey,
            GetDisplayedDispatchItem(),
            DispatchPayloadContext);

        var processingAnchor = GetNodeOrNull<Node3D>("ProcessingPayloadAnchor");
        if (processingAnchor is not null)
        {
            var bob = Mathf.Sin((float)(Time.GetTicksMsec() * 0.006)) * CellSize * 0.015f;
            processingAnchor.Position = processingAnchor.GetMeta("base_position", processingAnchor.Position).AsVector3()
                + new Vector3(0.0f, bob + (ProcessingRatio * CellSize * 0.02f), 0.0f);
        }

        var dispatchAnchor = GetNodeOrNull<Node3D>("DispatchPayloadAnchor");
        if (dispatchAnchor is not null)
        {
            var pulse = 1.0f + (HasBufferedOutput ? Mathf.Sin((float)(Time.GetTicksMsec() * 0.008)) * 0.04f : 0.0f);
            dispatchAnchor.Scale = new Vector3(pulse, pulse, pulse);
        }
    }

    public override IEnumerable<string> GetInspectionLines()
    {
        foreach (var line in base.GetInspectionLines())
        {
            yield return line;
        }

        yield return $"输入舱位：{_inputBuffer.Count}/{ChamberCapacity}";
        yield return $"输出舱位：{_outputBuffer.Count}/{ChamberCapacity}";
        yield return _processingItem is null
            ? "状态：待装载单件世界货物"
            : $"状态：单件转换中 {ProcessingRatio * 100.0f:0}%";
    }

    protected virtual FactoryItem? GetDisplayedStagingItem()
    {
        return _processingItem is null && _inputBuffer.TryPeek(out var item) ? item : null;
    }

    protected virtual FactoryItem? GetDisplayedProcessingItem()
    {
        return _processingItem;
    }

    protected virtual FactoryItem? GetDisplayedDispatchItem()
    {
        return _outputBuffer.TryPeek(out var item) ? item : null;
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

    protected Node3D CreatePayloadAnchor(string name, Vector3 position)
    {
        var anchor = new Node3D
        {
            Name = name,
            Position = position
        };
        anchor.SetMeta("base_position", position);
        AddChild(anchor);
        return anchor;
    }

    private void SyncPayloadVisual(
        string anchorName,
        ref Node3D? payloadVisual,
        ref string payloadVisualKey,
        FactoryItem? item,
        FactoryTransportVisualContext visualContext)
    {
        var anchor = GetNodeOrNull<Node3D>(anchorName);
        if (anchor is null)
        {
            if (payloadVisual is not null)
            {
                payloadVisual.QueueFree();
                payloadVisual = null;
                payloadVisualKey = string.Empty;
            }

            return;
        }

        if (item is null)
        {
            if (payloadVisual is not null)
            {
                payloadVisual.Visible = false;
            }

            return;
        }

        var visualKey = $"{item.ItemKind}:{item.CargoForm}:{visualContext}";
        if (payloadVisual is null || !string.Equals(payloadVisualKey, visualKey, System.StringComparison.Ordinal))
        {
            payloadVisual?.QueueFree();
            payloadVisual = FactoryTransportVisualFactory.CreateVisual(item, CellSize, visualContext);
            payloadVisual.Name = $"{anchorName}_Visual";
            payloadVisual.Position = Vector3.Zero;
            anchor.AddChild(payloadVisual);
            payloadVisualKey = visualKey;
        }

        payloadVisual.Visible = true;
    }
}

public partial class CargoUnpackerStructure : FactoryCargoConverterStructure
{
    public override BuildPrototypeKind Kind => BuildPrototypeKind.CargoUnpacker;
    public override string Description => "单件解包处理舱。一次接收一个世界大件，并在舱内拆成可上料轨的小型标准载具。";
    protected override FactoryCargoForm OutputCargoForm => FactoryCargoForm.InteriorFeed;
    protected override FactoryTransportVisualContext DispatchPayloadContext => FactoryTransportVisualContext.InteriorRail;

    protected override void BuildVisuals()
    {
        if (SiteKind == FactorySiteKind.Interior)
        {
            CreateBox("UnpackerBaseSkid", new Vector3(CellSize * 1.86f, 0.16f, CellSize * 1.26f), new Color("082F49"), new Vector3(0.0f, 0.08f, 0.0f));
            CreateInteriorModuleShell(this, "UnpackerChamber", new Vector3(CellSize * 1.68f, 0.64f, CellSize * 1.08f), new Color("164E63"), new Color("7DD3FC"), new Vector3(0.0f, 0.38f, 0.0f));
            CreateBox("UnpackerMouthFrame", new Vector3(CellSize * 0.28f, 0.40f, CellSize * 0.86f), new Color("BAE6FD"), new Vector3(-CellSize * 0.58f, 0.34f, 0.0f));
            CreateBox("UnpackerCradle", new Vector3(CellSize * 0.92f, 0.14f, CellSize * 0.72f), new Color("0EA5E9"), new Vector3(0.0f, 0.18f, 0.0f));
            CreateInteriorTray(this, "UnpackerOutfeed", new Vector3(CellSize * 0.54f, 0.10f, CellSize * 0.18f), new Color("0369A1"), new Color("DBEAFE"), new Vector3(CellSize * 0.62f, 0.16f, 0.0f));
            CreateBox("UnpackerClampTop", new Vector3(CellSize * 0.68f, 0.06f, CellSize * 0.10f), new Color("E0F2FE"), new Vector3(0.0f, 0.58f, -CellSize * 0.34f));
            CreateBox("UnpackerClampBottom", new Vector3(CellSize * 0.68f, 0.06f, CellSize * 0.10f), new Color("E0F2FE"), new Vector3(0.0f, 0.58f, CellSize * 0.34f));
            CreateInteriorIndicatorLight(this, "UnpackerLamp", new Color("7DD3FC"), new Vector3(-CellSize * 0.72f, 0.56f, 0.0f), CellSize * 0.10f);
            CreatePayloadAnchor("StagingPayloadAnchor", new Vector3(-CellSize * 0.60f, 0.26f, 0.0f));
            CreatePayloadAnchor("ProcessingPayloadAnchor", new Vector3(0.0f, 0.30f, 0.0f));
            CreatePayloadAnchor("DispatchPayloadAnchor", new Vector3(CellSize * 0.66f, 0.26f, 0.0f));
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
    public override string Description => "单件封包处理舱。把舱内小型载具重新组合成一个可出舱的世界标准大件货物。";
    protected override FactoryCargoForm OutputCargoForm => FactoryCargoForm.WorldPacked;
    protected override FactoryTransportVisualContext StagingPayloadContext => FactoryTransportVisualContext.InteriorRail;
    protected override FactoryTransportVisualContext DispatchPayloadContext => FactoryTransportVisualContext.InteriorStaging;

    protected override FactoryItem? GetDisplayedProcessingItem()
    {
        return ProcessingItem is null
            ? null
            : ProcessingItem.WithCargoForm(Kind, FactoryCargoForm.WorldPacked);
    }

    protected override void BuildVisuals()
    {
        if (SiteKind == FactorySiteKind.Interior)
        {
            CreateBox("PackerBaseSkid", new Vector3(CellSize * 1.86f, 0.16f, CellSize * 1.26f), new Color("431407"), new Vector3(0.0f, 0.08f, 0.0f));
            CreateInteriorModuleShell(this, "PackerChamber", new Vector3(CellSize * 1.68f, 0.64f, CellSize * 1.08f), new Color("7C2D12"), new Color("FDBA74"), new Vector3(0.0f, 0.38f, 0.0f));
            CreateInteriorTray(this, "PackerInfeed", new Vector3(CellSize * 0.54f, 0.10f, CellSize * 0.18f), new Color("EA580C"), new Color("FED7AA"), new Vector3(-CellSize * 0.62f, 0.16f, 0.0f));
            CreateBox("PackerCompressionDeck", new Vector3(CellSize * 0.92f, 0.14f, CellSize * 0.72f), new Color("C2410C"), new Vector3(0.0f, 0.18f, 0.0f));
            CreateBox("PackerExportCradle", new Vector3(CellSize * 0.86f, 0.12f, CellSize * 0.66f), new Color("FB923C"), new Vector3(CellSize * 0.56f, 0.18f, 0.0f));
            CreateBox("PackerClampTop", new Vector3(CellSize * 0.74f, 0.06f, CellSize * 0.10f), new Color("FED7AA"), new Vector3(0.0f, 0.58f, -CellSize * 0.34f));
            CreateBox("PackerClampBottom", new Vector3(CellSize * 0.74f, 0.06f, CellSize * 0.10f), new Color("FED7AA"), new Vector3(0.0f, 0.58f, CellSize * 0.34f));
            CreateInteriorIndicatorLight(this, "PackerLamp", new Color("FB923C"), new Vector3(CellSize * 0.72f, 0.56f, 0.0f), CellSize * 0.10f);
            CreatePayloadAnchor("StagingPayloadAnchor", new Vector3(-CellSize * 0.64f, 0.24f, 0.0f));
            CreatePayloadAnchor("ProcessingPayloadAnchor", new Vector3(0.0f, 0.30f, 0.0f));
            CreatePayloadAnchor("DispatchPayloadAnchor", new Vector3(CellSize * 0.58f, 0.24f, 0.0f));
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
    private readonly FactoryItemBuffer _buffer = new(4);
    private double _dispatchCooldown;
    private Node3D? _bufferedPayloadVisual;
    private string _bufferedPayloadVisualKey = string.Empty;

    public override BuildPrototypeKind Kind => BuildPrototypeKind.TransferBuffer;
    public override string Description => "大件转换节拍缓冲架。围绕解包/封包舱整理待处理或待出舱的载荷，不作为普通小型加工机使用。";

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
            _dispatchCooldown = 0.18f;
        }
    }

    public override void UpdateVisuals(float tickAlpha)
    {
        var anchor = GetNodeOrNull<Node3D>("BufferPayloadAnchor");
        var item = _buffer.TryPeek(out var buffered) ? buffered : null;
        if (anchor is null)
        {
            return;
        }

        if (item is null)
        {
            if (_bufferedPayloadVisual is not null)
            {
                _bufferedPayloadVisual.Visible = false;
            }

            return;
        }

        var visualContext = item.CargoForm == FactoryCargoForm.InteriorFeed
            ? FactoryTransportVisualContext.InteriorRail
            : FactoryTransportVisualContext.InteriorStaging;
        var visualKey = $"{item.ItemKind}:{item.CargoForm}:{visualContext}";
        if (_bufferedPayloadVisual is null || !string.Equals(_bufferedPayloadVisualKey, visualKey, System.StringComparison.Ordinal))
        {
            _bufferedPayloadVisual?.QueueFree();
            _bufferedPayloadVisual = FactoryTransportVisualFactory.CreateVisual(item, CellSize, visualContext);
            _bufferedPayloadVisual.Name = "BufferPayloadVisual";
            anchor.AddChild(_bufferedPayloadVisual);
            _bufferedPayloadVisualKey = visualKey;
        }

        _bufferedPayloadVisual.Visible = true;
        var pulse = 1.0f + Mathf.Sin((float)(Time.GetTicksMsec() * 0.007f)) * 0.03f;
        anchor.Scale = new Vector3(pulse, pulse, pulse);
    }

    public override IEnumerable<string> GetInspectionLines()
    {
        foreach (var line in base.GetInspectionLines())
        {
            yield return line;
        }

        yield return $"缓冲位：{_buffer.Count}/4";
    }

    protected override void BuildVisuals()
    {
        if (SiteKind == FactorySiteKind.Interior)
        {
            CreateBox("BufferDeck", new Vector3(CellSize * 1.36f, 0.12f, CellSize * 1.08f), new Color("042F2E"), new Vector3(0.0f, 0.06f, 0.0f));
            CreateBox("BufferCradle", new Vector3(CellSize * 0.96f, 0.12f, CellSize * 0.72f), new Color("0F766E"), new Vector3(0.0f, 0.18f, 0.0f));
            CreateBox("BufferGuideNorth", new Vector3(CellSize * 0.82f, 0.08f, CellSize * 0.06f), new Color("99F6E4"), new Vector3(0.0f, 0.26f, -CellSize * 0.30f));
            CreateBox("BufferGuideSouth", new Vector3(CellSize * 0.82f, 0.08f, CellSize * 0.06f), new Color("99F6E4"), new Vector3(0.0f, 0.26f, CellSize * 0.30f));
            CreateBox("BufferRackBack", new Vector3(CellSize * 0.12f, 0.44f, CellSize * 0.74f), new Color("134E4A"), new Vector3(-CellSize * 0.42f, 0.34f, 0.0f));
            CreateInteriorTray(this, "BufferOutfeed", new Vector3(CellSize * 0.44f, 0.08f, CellSize * 0.16f), new Color("115E59"), new Color("CCFBF1"), new Vector3(CellSize * 0.44f, 0.14f, 0.0f));
            CreateInteriorIndicatorLight(this, "BufferLamp", new Color("5EEAD4"), new Vector3(-CellSize * 0.50f, 0.46f, 0.0f), CellSize * 0.08f);
            var anchor = new Node3D
            {
                Name = "BufferPayloadAnchor",
                Position = new Vector3(0.0f, 0.24f, 0.0f)
            };
            AddChild(anchor);
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
