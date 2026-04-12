using Godot;
using System;
using System.Collections.Generic;

public abstract partial class FactoryCargoConverterStructure : FactoryStructure, IFactoryItemProvider, IFactoryItemReceiver
{
    private readonly FactoryItemBuffer _inputBuffer;
    private readonly FactoryItemBuffer _outputBuffer;
    private double _dispatchCooldown;

    private Node3D? _stagingPayloadVisual;
    private string _stagingPayloadVisualKey = string.Empty;
    private Node3D? _processingPayloadVisual;
    private string _processingPayloadVisualKey = string.Empty;
    private Node3D? _dispatchPayloadVisual;
    private string _dispatchPayloadVisualKey = string.Empty;

    protected FactoryCargoConverterStructure(int inputCapacity = 1, int outputCapacity = 1)
    {
        _inputBuffer = new FactoryItemBuffer(Mathf.Max(1, inputCapacity));
        _outputBuffer = new FactoryItemBuffer(Mathf.Max(1, outputCapacity));
    }

    protected virtual float DispatchCooldownSeconds => 0.20f;
    protected virtual int ChamberCapacity => 1;
    protected virtual FactoryTransportVisualContext StagingPayloadContext => FactoryTransportVisualContext.InteriorStaging;
    protected virtual FactoryTransportVisualContext ProcessingPayloadContext => FactoryTransportVisualContext.InteriorConversion;
    protected virtual FactoryTransportVisualContext DispatchPayloadContext => FactoryTransportVisualContext.InteriorRail;
    protected int InputCount => _inputBuffer.Count;
    protected int OutputCount => _outputBuffer.Count;
    protected bool IsOutputFull => _outputBuffer.IsFull;
    protected bool HasBufferedOutput => !_outputBuffer.IsEmpty;

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
        _dispatchCooldown = Mathf.Max(0.0f, (float)(_dispatchCooldown - stepSeconds));
        TryDispatchBufferedOutput(simulation);
        AdvanceConverter(simulation, stepSeconds);
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

        ApplyMechanicsVisuals();
    }

    public override IEnumerable<string> GetInspectionLines()
    {
        foreach (var line in base.GetInspectionLines())
        {
            yield return line;
        }

        yield return $"输入舱位：{InputCount}/{ChamberCapacity}";
        yield return $"输出舱位：{OutputCount}/{ChamberCapacity}";
        foreach (var line in DescribeConversionState())
        {
            yield return line;
        }
    }

    protected virtual IEnumerable<string> DescribeConversionState()
    {
        yield return "状态：待机";
    }

    protected virtual void ApplyMechanicsVisuals()
    {
    }

    protected virtual FactoryItem? GetDisplayedStagingItem()
    {
        return TryPeekInput(out var item) ? item : null;
    }

    protected virtual FactoryItem? GetDisplayedProcessingItem()
    {
        return null;
    }

    protected virtual FactoryItem? GetDisplayedDispatchItem()
    {
        return TryPeekOutput(out var item) ? item : null;
    }

    protected virtual bool CanAcceptConversionInput(FactoryItem item, Vector2I sourceCell)
    {
        return CanReceiveFrom(sourceCell)
            && !_inputBuffer.IsFull
            && FactoryCargoRules.StructureAcceptsItem(Kind, FactoryIndustrialStandards.ResolveSiteKind(Site), item);
    }

    protected abstract void AdvanceConverter(SimulationController simulation, double stepSeconds);

    protected bool TryPeekInput(out FactoryItem? item)
    {
        return _inputBuffer.TryPeek(out item);
    }

    protected bool TryTakeInput(out FactoryItem? item)
    {
        return _inputBuffer.TryDequeue(out item);
    }

    protected bool TryPeekOutput(out FactoryItem? item)
    {
        return _outputBuffer.TryPeek(out item);
    }

    protected bool TryBufferOutput(FactoryItem item)
    {
        return _outputBuffer.TryEnqueue(item);
    }

    protected virtual void TryDispatchBufferedOutput(SimulationController simulation)
    {
        if (_dispatchCooldown > 0.0 || !_outputBuffer.TryPeek(out var item) || item is null)
        {
            return;
        }

        var outputCells = GetOutputCells();
        for (var index = 0; index < outputCells.Count; index++)
        {
            var targetCell = outputCells[index];
            var sourceCell = GetTransferOutputCell(targetCell);
            if (!simulation.TrySendItem(this, sourceCell, targetCell, item))
            {
                continue;
            }

            _outputBuffer.TryDequeue(out _);
            _dispatchCooldown = DispatchCooldownSeconds;
            break;
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

    protected void CreateOpenHeavyChamber(
        string prefix,
        Vector3 baseSize,
        float frameHeight,
        float chamberDepth,
        Color frameColor,
        Color accentColor)
    {
        var frameSpan = Mathf.Max(CellSize * 0.68f, baseSize.X * 0.58f);
        var railSpan = Mathf.Max(CellSize * 0.62f, baseSize.X * 0.76f);
        var braceSpan = Mathf.Max(CellSize * 0.72f, baseSize.X * 0.84f);
        CreateBox($"{prefix}BaseSkid", baseSize, frameColor.Darkened(0.24f), new Vector3(0.0f, baseSize.Y * 0.5f, 0.0f));
        CreateBox(
            $"{prefix}DeckPlate",
            new Vector3(baseSize.X * 0.72f, Mathf.Max(CellSize * 0.04f, baseSize.Y * 0.42f), chamberDepth * 0.38f),
            frameColor.Lightened(0.08f),
            new Vector3(0.0f, baseSize.Y + 0.02f, 0.0f));
        CreateBox(
            $"{prefix}FrameWest",
            new Vector3(CellSize * 0.10f, frameHeight, chamberDepth * 0.78f),
            frameColor,
            new Vector3(-frameSpan * 0.5f, frameHeight * 0.5f, 0.0f));
        CreateBox(
            $"{prefix}FrameEast",
            new Vector3(CellSize * 0.10f, frameHeight, chamberDepth * 0.78f),
            frameColor,
            new Vector3(frameSpan * 0.5f, frameHeight * 0.5f, 0.0f));
        CreateBox(
            $"{prefix}ClampRailNorth",
            new Vector3(railSpan, CellSize * 0.04f, CellSize * 0.08f),
            accentColor.Lightened(0.06f),
            new Vector3(0.0f, frameHeight * 0.78f, -chamberDepth * 0.28f));
        CreateBox(
            $"{prefix}ClampRailSouth",
            new Vector3(railSpan, CellSize * 0.04f, CellSize * 0.08f),
            accentColor.Lightened(0.06f),
            new Vector3(0.0f, frameHeight * 0.78f, chamberDepth * 0.28f));
        CreateBox(
            $"{prefix}RearBrace",
            new Vector3(braceSpan, CellSize * 0.06f, CellSize * 0.08f),
            accentColor,
            new Vector3(0.0f, frameHeight * 0.58f, -chamberDepth * 0.36f));
        CreateBox(
            $"{prefix}FrontBrace",
            new Vector3(braceSpan, CellSize * 0.05f, CellSize * 0.06f),
            accentColor.Darkened(0.06f),
            new Vector3(0.0f, frameHeight * 0.28f, chamberDepth * 0.34f));
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
            payloadVisual?.QueueFree();
            payloadVisual = null;
            payloadVisualKey = string.Empty;
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

        var visualKey = $"{item.ItemKind}:{item.CargoForm}:{item.BundleTemplateId}:{visualContext}";
        if (payloadVisual is null || !string.Equals(payloadVisualKey, visualKey, StringComparison.Ordinal))
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
    private const float EmitSeconds = 0.42f;

    private FactoryItem? _processingBundle;
    private Queue<FactoryItemKind> _pendingManifest = new();
    private double _emitProgress;
    private string _preferredTemplateId = "bulk-iron-ore-standard";

    public CargoUnpackerStructure() : base(inputCapacity: 1, outputCapacity: 1)
    {
    }

    public override BuildPrototypeKind Kind => BuildPrototypeKind.CargoUnpacker;
    public override string Description => "单件解包处理舱。一次接收 1 个世界大包，并按 manifest 节拍持续吐出多个舱内小包。";

    protected override FactoryTransportVisualContext DispatchPayloadContext => FactoryTransportVisualContext.InteriorRail;
    protected override int ChamberCapacity => 1;

    public override IReadOnlyDictionary<string, string> CaptureBlueprintConfiguration()
    {
        return string.IsNullOrWhiteSpace(_preferredTemplateId)
            ? new Dictionary<string, string>()
            : new Dictionary<string, string> { ["bundle_template_id"] = _preferredTemplateId };
    }

    public override bool ApplyBlueprintConfiguration(IReadOnlyDictionary<string, string> configuration)
    {
        if (configuration.TryGetValue("bundle_template_id", out var templateId) && FactoryBundleCatalog.TryGet(templateId, out _))
        {
            _preferredTemplateId = templateId;
        }

        return true;
    }

    public override string? CaptureMapRecipeId()
    {
        return string.IsNullOrWhiteSpace(_preferredTemplateId) ? null : _preferredTemplateId;
    }

    public override bool TryApplyMapRecipe(string recipeId)
    {
        if (!FactoryBundleCatalog.TryGet(recipeId, out _))
        {
            return false;
        }

        _preferredTemplateId = recipeId;
        return true;
    }

    protected override FactoryItem? GetDisplayedProcessingItem()
    {
        return _processingBundle;
    }

    protected override IEnumerable<string> DescribeConversionState()
    {
        if (_processingBundle is null)
        {
            var template = FactoryBundleCatalog.Get(_preferredTemplateId);
            yield return $"待机模板：{FactoryBundleCatalog.DescribeTemplate(template)}";
            yield return $"处理规格：{FactoryBundleCatalog.GetSizeTierLabel(template.SizeTier)}";
            yield break;
        }

        yield return $"处理模板：{FactoryBundleCatalog.GetDisplayName(_processingBundle)}";
        yield return $"处理规格：{FactoryBundleCatalog.GetSizeTierLabel(FactoryBundleCatalog.ResolveSizeTier(_processingBundle))}";
        yield return $"剩余解包：{_pendingManifest.Count} 件舱内小包";
    }

    protected override void AdvanceConverter(SimulationController simulation, double stepSeconds)
    {
        if (_processingBundle is null)
        {
            if (TryTakeInput(out var input) && input is not null)
            {
                _processingBundle = input;
                _preferredTemplateId = string.IsNullOrWhiteSpace(input.BundleTemplateId) ? _preferredTemplateId : input.BundleTemplateId;
                _pendingManifest = FactoryBundleCatalog.ExpandManifest(input);
                _emitProgress = 0.0;
            }

            return;
        }

        if (IsOutputFull)
        {
            return;
        }

        _emitProgress += stepSeconds;
        if (_emitProgress < EmitSeconds || _pendingManifest.Count <= 0)
        {
            return;
        }

        _emitProgress = 0.0;
        var nextItemKind = _pendingManifest.Dequeue();
        TryBufferOutput(simulation.CreateItem(Site, Kind, nextItemKind, FactoryCargoForm.InteriorFeed));

        if (_pendingManifest.Count == 0)
        {
            _processingBundle = null;
        }
    }

    protected override void ApplyMechanicsVisuals()
    {
        var clampNorth = GetNodeOrNull<MeshInstance3D>("UnpackerClampNorth");
        var clampSouth = GetNodeOrNull<MeshInstance3D>("UnpackerClampSouth");
        var carriage = GetNodeOrNull<Node3D>("ProcessingPayloadAnchor");
        if (clampNorth is not null)
        {
            clampNorth.Position = new Vector3(0.0f, 0.60f, -CellSize * 0.26f - Mathf.Sin((float)(Time.GetTicksMsec() * 0.005)) * CellSize * 0.05f);
        }

        if (clampSouth is not null)
        {
            clampSouth.Position = new Vector3(0.0f, 0.60f, CellSize * 0.26f + Mathf.Sin((float)(Time.GetTicksMsec() * 0.005)) * CellSize * 0.05f);
        }

        if (carriage is not null)
        {
            var basePosition = carriage.GetMeta("base_position", carriage.Position).AsVector3();
            var slide = _processingBundle is null ? 0.0f : Mathf.Sin((float)(Time.GetTicksMsec() * 0.004)) * CellSize * 0.06f;
            carriage.Position = basePosition + new Vector3(slide, 0.0f, 0.0f);
        }
    }

    protected override void BuildVisuals()
    {
        if (SiteKind == FactorySiteKind.Interior)
        {
            var previewSize = Footprint.GetPreviewSize(CellSize, Facing);
            var deckWidth = Mathf.Max(CellSize * 1.56f, previewSize.X * 0.92f);
            var deckDepth = Mathf.Max(CellSize * 1.58f, previewSize.Y * 0.92f);
            CreateOpenHeavyChamber(
                "UnpackerChamber",
                new Vector3(deckWidth, 0.14f, deckDepth),
                frameHeight: 0.86f,
                chamberDepth: deckDepth,
                frameColor: new Color("12324A"),
                accentColor: new Color("7DD3FC"));
            CreateBox("UnpackerMouthFrame", new Vector3(Mathf.Max(CellSize * 0.18f, deckWidth * 0.12f), 0.48f, deckDepth * 0.74f), new Color("C7EAFE"), new Vector3(-deckWidth * 0.38f, 0.30f, 0.0f));
            CreateBox("UnpackerCradle", new Vector3(deckWidth * 0.66f, 0.12f, deckDepth * 0.52f), new Color("0EA5E9"), new Vector3(0.0f, 0.18f, 0.0f));
            CreateBox("UnpackerGuideNorth", new Vector3(deckWidth * 0.62f, 0.06f, CellSize * 0.06f), new Color("DBEAFE"), new Vector3(0.0f, 0.26f, -deckDepth * 0.22f));
            CreateBox("UnpackerGuideSouth", new Vector3(deckWidth * 0.62f, 0.06f, CellSize * 0.06f), new Color("DBEAFE"), new Vector3(0.0f, 0.26f, deckDepth * 0.22f));
            CreateInteriorTray(this, "UnpackerOutfeed", new Vector3(deckWidth * 0.34f, 0.10f, CellSize * 0.22f), new Color("0B5A88"), new Color("DBEAFE"), new Vector3(deckWidth * 0.36f, 0.16f, 0.0f));
            CreateBox("UnpackerClampNorth", new Vector3(deckWidth * 0.48f, 0.06f, CellSize * 0.10f), new Color("E0F2FE"), new Vector3(0.0f, 0.60f, -CellSize * 0.26f));
            CreateBox("UnpackerClampSouth", new Vector3(deckWidth * 0.48f, 0.06f, CellSize * 0.10f), new Color("E0F2FE"), new Vector3(0.0f, 0.60f, CellSize * 0.26f));
            CreateInteriorIndicatorLight(this, "UnpackerLamp", new Color("7DD3FC"), new Vector3(-deckWidth * 0.34f, 0.64f, 0.0f), CellSize * 0.09f);
            CreateInteriorLabelPlate(this, "UnpackerTier", "重载", new Color("7DD3FC"), new Vector3(-deckWidth * 0.10f, 0.16f, -deckDepth * 0.32f), 1.1f);
            CreatePayloadAnchor("StagingPayloadAnchor", new Vector3(-deckWidth * 0.38f, 0.24f, 0.0f));
            CreatePayloadAnchor("ProcessingPayloadAnchor", new Vector3(0.0f, 0.28f, 0.0f));
            CreatePayloadAnchor("DispatchPayloadAnchor", new Vector3(deckWidth * 0.38f, 0.24f, 0.0f));
            return;
        }

        CreateBox("Deck", new Vector3(CellSize * 1.2f, 0.16f, CellSize * 1.2f), new Color("164E63"), new Vector3(0.0f, 0.08f, 0.0f));
        CreateBox("Chamber", new Vector3(CellSize * 0.86f, 0.58f, CellSize * 0.86f), new Color("38BDF8"), new Vector3(0.0f, 0.30f, 0.0f));
        CreateBox("FeedRail", new Vector3(CellSize * 1.24f, 0.08f, CellSize * 0.18f), new Color("BAE6FD"), new Vector3(0.0f, 0.22f, 0.0f));
    }
}

public partial class CargoPackerStructure : FactoryCargoConverterStructure
{
    private readonly Dictionary<FactoryItemKind, int> _packedCounts = new();

    private FactoryItem? _processingBundle;
    private double _processProgress;
    private string _configuredTemplateId = "packed-gear-compact";
    private string _lockedTemplateId = string.Empty;

    public CargoPackerStructure() : base(inputCapacity: 1, outputCapacity: 1)
    {
    }

    public override BuildPrototypeKind Kind => BuildPrototypeKind.CargoPacker;
    public override string Description => "单件封包处理舱。围绕目标模板累计舱内小包，清单满足后才压装成 1 个世界大包。";

    protected override FactoryTransportVisualContext StagingPayloadContext => FactoryTransportVisualContext.InteriorRail;
    protected override FactoryTransportVisualContext DispatchPayloadContext => FactoryTransportVisualContext.InteriorStaging;
    protected override int ChamberCapacity => 12;

    public override IReadOnlyDictionary<string, string> CaptureBlueprintConfiguration()
    {
        return string.IsNullOrWhiteSpace(_configuredTemplateId)
            ? new Dictionary<string, string>()
            : new Dictionary<string, string> { ["bundle_template_id"] = _configuredTemplateId };
    }

    public override bool ApplyBlueprintConfiguration(IReadOnlyDictionary<string, string> configuration)
    {
        if (configuration.TryGetValue("bundle_template_id", out var templateId) && FactoryBundleCatalog.TryGet(templateId, out _))
        {
            _configuredTemplateId = templateId;
        }

        return true;
    }

    public override string? CaptureMapRecipeId()
    {
        return string.IsNullOrWhiteSpace(_configuredTemplateId) ? null : _configuredTemplateId;
    }

    public override bool TryApplyMapRecipe(string recipeId)
    {
        if (!FactoryBundleCatalog.TryGet(recipeId, out _))
        {
            return false;
        }

        _configuredTemplateId = recipeId;
        return true;
    }

    protected override bool CanAcceptConversionInput(FactoryItem item, Vector2I sourceCell)
    {
        if (!base.CanAcceptConversionInput(item, sourceCell) || _processingBundle is not null)
        {
            return false;
        }

        if (!TryResolveCandidateTemplate(item, out var template) || template is null)
        {
            return false;
        }

        var projected = BuildProjectedCounts(item.ItemKind);
        return FactoryBundleCatalog.CanAcceptIntoTemplate(template, item.ItemKind, projected, out _);
    }

    protected override FactoryItem? GetDisplayedProcessingItem()
    {
        return _processingBundle;
    }

    protected override IEnumerable<string> DescribeConversionState()
    {
        var templateId = ResolveTemplateIdForInspection();
        var template = FactoryBundleCatalog.Get(templateId);
        var packedUnits = CountPackedUnits();
        yield return $"目标模板：{FactoryBundleCatalog.DescribeTemplate(template)}";
        yield return $"处理规格：{FactoryBundleCatalog.GetSizeTierLabel(template.SizeTier)}";
        yield return $"累计装箱：{packedUnits}/{template.TotalUnits}";
        if (_processingBundle is not null)
        {
            yield return $"压装进度：{Mathf.Clamp((float)(_processProgress / 1.4f), 0.0f, 1.0f) * 100.0f:0}%";
        }
    }

    protected override void AdvanceConverter(SimulationController simulation, double stepSeconds)
    {
        if (_processingBundle is not null)
        {
            if (IsOutputFull)
            {
                return;
            }

            _processProgress += stepSeconds;
            if (_processProgress < 1.4f)
            {
                return;
            }

            TryBufferOutput(_processingBundle);
            _processingBundle = null;
            _processProgress = 0.0;
            _packedCounts.Clear();
            _lockedTemplateId = string.Empty;
            return;
        }

        if (TryTakeInput(out var input) && input is not null)
        {
            if (!TryResolveCandidateTemplate(input, out var template) || template is null)
            {
                return;
            }

            _lockedTemplateId = template.Id;
            _packedCounts[input.ItemKind] = _packedCounts.TryGetValue(input.ItemKind, out var existing) ? existing + 1 : 1;
            if (FactoryBundleCatalog.IsSatisfied(template, _packedCounts, out _))
            {
                _processingBundle = simulation.CreateItem(
                    Site,
                    Kind,
                    template.WorldItemKind,
                    FactoryCargoForm.WorldPacked,
                    template.Id,
                    _packedCounts);
                _processProgress = 0.0;
            }
        }
    }

    protected override void ApplyMechanicsVisuals()
    {
        var ram = GetNodeOrNull<MeshInstance3D>("PackerCompressionRam");
        var clampNorth = GetNodeOrNull<MeshInstance3D>("PackerClampNorth");
        var clampSouth = GetNodeOrNull<MeshInstance3D>("PackerClampSouth");
        var anchor = GetNodeOrNull<Node3D>("ProcessingPayloadAnchor");
        var ratio = Mathf.Clamp((float)(_processProgress / 1.4f), 0.0f, 1.0f);
        if (ram is not null)
        {
            ram.Position = new Vector3(0.0f, 0.70f - ratio * CellSize * 0.20f, 0.0f);
        }

        if (clampNorth is not null)
        {
            clampNorth.Position = new Vector3(0.0f, 0.42f, (-CellSize * 0.30f) + ratio * CellSize * 0.08f);
        }

        if (clampSouth is not null)
        {
            clampSouth.Position = new Vector3(0.0f, 0.42f, (CellSize * 0.30f) - ratio * CellSize * 0.08f);
        }

        if (anchor is not null)
        {
            var basePosition = anchor.GetMeta("base_position", anchor.Position).AsVector3();
            anchor.Position = basePosition + new Vector3(Mathf.Sin((float)(Time.GetTicksMsec() * 0.006)) * CellSize * 0.03f, 0.0f, 0.0f);
        }
    }

    protected override void BuildVisuals()
    {
        if (SiteKind == FactorySiteKind.Interior)
        {
            var previewSize = Footprint.GetPreviewSize(CellSize, Facing);
            var deckWidth = Mathf.Max(CellSize * 1.56f, previewSize.X * 0.92f);
            var deckDepth = Mathf.Max(CellSize * 1.52f, previewSize.Y * 0.92f);
            CreateOpenHeavyChamber(
                "PackerChamber",
                new Vector3(deckWidth, 0.14f, deckDepth),
                frameHeight: 0.84f,
                chamberDepth: deckDepth,
                frameColor: new Color("6A240B"),
                accentColor: new Color("FDBA74"));
            CreateInteriorTray(this, "PackerInfeed", new Vector3(deckWidth * 0.32f, 0.10f, CellSize * 0.20f), new Color("B94A13"), new Color("FED7AA"), new Vector3(-deckWidth * 0.38f, 0.16f, 0.0f));
            CreateBox("PackerCompressionDeck", new Vector3(deckWidth * 0.66f, 0.12f, deckDepth * 0.54f), new Color("C2410C"), new Vector3(0.0f, 0.18f, 0.0f));
            CreateBox("PackerClampNorth", new Vector3(deckWidth * 0.62f, 0.08f, CellSize * 0.08f), new Color("FED7AA"), new Vector3(0.0f, 0.42f, -CellSize * 0.30f));
            CreateBox("PackerClampSouth", new Vector3(deckWidth * 0.62f, 0.08f, CellSize * 0.08f), new Color("FED7AA"), new Vector3(0.0f, 0.42f, CellSize * 0.30f));
            CreateBox("PackerRamColumnWest", new Vector3(CellSize * 0.08f, 0.54f, CellSize * 0.10f), new Color("FCD7AA"), new Vector3(-CellSize * 0.18f, 0.44f, 0.0f));
            CreateBox("PackerRamColumnEast", new Vector3(CellSize * 0.08f, 0.54f, CellSize * 0.10f), new Color("FCD7AA"), new Vector3(CellSize * 0.18f, 0.44f, 0.0f));
            CreateBox("PackerCompressionRam", new Vector3(CellSize * 0.22f, 0.14f, CellSize * 0.22f), new Color("FFE4C2"), new Vector3(0.0f, 0.70f, 0.0f));
            CreateBox("PackerExportCradle", new Vector3(deckWidth * 0.48f, 0.12f, deckDepth * 0.40f), new Color("FB923C"), new Vector3(deckWidth * 0.36f, 0.18f, 0.0f));
            CreateBox("PackerGuideNorth", new Vector3(deckWidth * 0.62f, 0.06f, CellSize * 0.06f), new Color("FED7AA"), new Vector3(0.0f, 0.26f, -deckDepth * 0.22f));
            CreateBox("PackerGuideSouth", new Vector3(deckWidth * 0.62f, 0.06f, CellSize * 0.06f), new Color("FED7AA"), new Vector3(0.0f, 0.26f, deckDepth * 0.22f));
            CreateInteriorIndicatorLight(this, "PackerLamp", new Color("FB923C"), new Vector3(deckWidth * 0.34f, 0.64f, 0.0f), CellSize * 0.09f);
            CreateInteriorLabelPlate(this, "PackerTier", "压装", new Color("FB923C"), new Vector3(deckWidth * 0.10f, 0.16f, -deckDepth * 0.32f), 1.1f);
            CreatePayloadAnchor("StagingPayloadAnchor", new Vector3(-deckWidth * 0.38f, 0.22f, 0.0f));
            CreatePayloadAnchor("ProcessingPayloadAnchor", new Vector3(0.0f, 0.28f, 0.0f));
            CreatePayloadAnchor("DispatchPayloadAnchor", new Vector3(deckWidth * 0.38f, 0.24f, 0.0f));
            return;
        }

        CreateBox("Deck", new Vector3(CellSize * 1.2f, 0.16f, CellSize * 1.2f), new Color("7C2D12"), new Vector3(0.0f, 0.08f, 0.0f));
        CreateBox("Compressor", new Vector3(CellSize * 0.86f, 0.58f, CellSize * 0.86f), new Color("F97316"), new Vector3(0.0f, 0.30f, 0.0f));
    }

    private bool TryResolveCandidateTemplate(FactoryItem item, out FactoryBundleTemplate? template)
    {
        if (!string.IsNullOrWhiteSpace(_configuredTemplateId) && FactoryBundleCatalog.TryGet(_configuredTemplateId, out template))
        {
            return true;
        }

        if (!string.IsNullOrWhiteSpace(_lockedTemplateId) && FactoryBundleCatalog.TryGet(_lockedTemplateId, out template))
        {
            return true;
        }

        return FactoryBundleCatalog.TryResolveAutoPackTemplate(item, out template);
    }

    private Dictionary<FactoryItemKind, int> BuildProjectedCounts(FactoryItemKind incomingItemKind)
    {
        var projected = new Dictionary<FactoryItemKind, int>(_packedCounts);
        if (TryPeekInput(out var queued) && queued is not null)
        {
            projected[queued.ItemKind] = projected.TryGetValue(queued.ItemKind, out var queuedCount) ? queuedCount + 1 : 1;
        }

        projected[incomingItemKind] = projected.TryGetValue(incomingItemKind, out var existing) ? existing + 1 : 1;
        return projected;
    }

    private string ResolveTemplateIdForInspection()
    {
        if (!string.IsNullOrWhiteSpace(_configuredTemplateId))
        {
            return _configuredTemplateId;
        }

        if (!string.IsNullOrWhiteSpace(_lockedTemplateId))
        {
            return _lockedTemplateId;
        }

        return "packed-gear-compact";
    }

    private int CountPackedUnits()
    {
        var total = 0;
        foreach (var pair in _packedCounts)
        {
            total += pair.Value;
        }

        return total;
    }
}

public partial class TransferBufferStructure : FactoryStructure, IFactoryItemProvider, IFactoryItemReceiver
{
    private readonly FactoryItemBuffer _buffer = new(4);
    private double _dispatchCooldown;
    private Node3D? _bufferedPayloadVisual;
    private string _bufferedPayloadVisualKey = string.Empty;

    public override BuildPrototypeKind Kind => BuildPrototypeKind.TransferBuffer;
    public override string Description => "重载/节拍缓冲架。既能暂存世界大包，也能作为封包前的小包汇流位。";

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
        _dispatchCooldown = Mathf.Max(0.0f, (float)(_dispatchCooldown - stepSeconds));
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
        var visualKey = $"{item.ItemKind}:{item.CargoForm}:{item.BundleTemplateId}:{visualContext}";
        if (_bufferedPayloadVisual is null || !string.Equals(_bufferedPayloadVisualKey, visualKey, StringComparison.Ordinal))
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
            CreateBox("BufferDeck", new Vector3(CellSize * 1.24f, 0.12f, CellSize * 0.98f), new Color("062827"), new Vector3(0.0f, 0.06f, 0.0f));
            CreateBox("BufferCradle", new Vector3(CellSize * 0.92f, 0.14f, CellSize * 0.68f), new Color("0F766E"), new Vector3(0.0f, 0.18f, 0.0f));
            CreateBox("BufferGuideNorth", new Vector3(CellSize * 0.82f, 0.08f, CellSize * 0.06f), new Color("99F6E4"), new Vector3(0.0f, 0.28f, -CellSize * 0.30f));
            CreateBox("BufferGuideSouth", new Vector3(CellSize * 0.82f, 0.08f, CellSize * 0.06f), new Color("99F6E4"), new Vector3(0.0f, 0.28f, CellSize * 0.30f));
            CreateBox("BufferRackBack", new Vector3(CellSize * 0.12f, 0.52f, CellSize * 0.74f), new Color("134E4A"), new Vector3(-CellSize * 0.42f, 0.34f, 0.0f));
            CreateInteriorTray(this, "BufferOutfeed", new Vector3(CellSize * 0.44f, 0.08f, CellSize * 0.16f), new Color("115E59"), new Color("CCFBF1"), new Vector3(CellSize * 0.44f, 0.14f, 0.0f));
            CreateInteriorIndicatorLight(this, "BufferLamp", new Color("5EEAD4"), new Vector3(-CellSize * 0.50f, 0.50f, 0.0f), CellSize * 0.08f);
            AddChild(new Node3D
            {
                Name = "BufferPayloadAnchor",
                Position = new Vector3(0.0f, 0.26f, 0.0f)
            });
            return;
        }

        CreateBox("Trench", new Vector3(CellSize * 0.84f, 0.12f, CellSize * 0.84f), new Color("0F766E"), new Vector3(0.0f, 0.06f, 0.0f));
        CreateBox("Tray", new Vector3(CellSize * 0.56f, 0.14f, CellSize * 0.56f), new Color("14B8A6"), new Vector3(0.0f, 0.18f, 0.0f));
    }

    private bool CanAcceptBufferedItem(FactoryItem item, Vector2I sourceCell)
    {
        return CanReceiveFrom(sourceCell)
            && !_buffer.IsFull
            && FactoryCargoRules.StructureAcceptsItem(Kind, FactoryIndustrialStandards.ResolveSiteKind(Site), item);
    }
}
