using Godot;
using System;
using System.Collections.Generic;

public partial class InserterStructure : FactoryStructure
{
    private const float HeldVisualScale = 0.72f;
    private const string NoFilterRecipeId = "inserter-filter-any";
    private const string FilterConfigurationKey = "filter_item_kind";
    private static readonly IReadOnlyList<FactoryItemKind> SelectableFilterKinds = BuildSelectableFilterKinds();
    private FactoryItem? _heldItem;
    private FactoryItemKind? _filterItemKind;
    private float _swingProgress;
    private bool _isReturning;
    private Node3D? _shoulderPivot;
    private Node3D? _elbowPivot;
    private MeshInstance3D? _claw;
    private Node3D? _heldVisualAnchor;
    private Node3D? _heldVisual;

    public override BuildPrototypeKind Kind => BuildPrototypeKind.Inserter;
    public override string Description => "从后方抓取物品并向前方投送，支持对传送带、仓储、中转缓冲和产物缓存做物品筛选。";

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
            if (!TryAcquireFilteredInput(simulation, out var takenItem) || takenItem is null)
            {
                return;
            }

            _heldItem = takenItem;
            _swingProgress = 0.0f;
            _isReturning = false;
            RebuildHeldVisual();

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
        ClearHeldVisual();
    }

    public override void UpdateVisuals(float tickAlpha)
    {
        UpdateArmPose(Mathf.Clamp(_swingProgress, 0.0f, 1.0f));

        if (_heldVisual is not null)
        {
            _heldVisual.Visible = _heldItem is not null;
        }
    }

    public override IEnumerable<string> GetInspectionLines()
    {
        foreach (var line in base.GetInspectionLines())
        {
            yield return line;
        }

        yield return $"筛选：{DescribeFilter()}";
        yield return _heldItem is not null
            ? $"状态：正在搬运 {FactoryItemCatalog.GetDisplayName(_heldItem.ItemKind)}"
            : _isReturning
                ? "状态：回臂复位"
                : "状态：待抓取";
    }

    public override FactoryStructureDetailModel GetDetailModel()
    {
        var summaryLines = new List<string>();
        foreach (var line in GetInspectionLines())
        {
            summaryLines.Add(line);
        }

        return new FactoryStructureDetailModel(
            InspectionTitle,
            "抓取参数",
            summaryLines,
            recipeSection: BuildFilterSection());
    }

    public override bool TrySetDetailRecipe(string recipeId)
    {
        if (string.Equals(recipeId, NoFilterRecipeId, StringComparison.Ordinal))
        {
            _filterItemKind = null;
            return true;
        }

        return TryResolveFilterFromRecipeId(recipeId, out _filterItemKind);
    }

    public override IReadOnlyDictionary<string, string> CaptureBlueprintConfiguration()
    {
        return _filterItemKind.HasValue
            ? new Dictionary<string, string> { [FilterConfigurationKey] = _filterItemKind.Value.ToString() }
            : new Dictionary<string, string>();
    }

    public override bool ApplyBlueprintConfiguration(IReadOnlyDictionary<string, string> configuration)
    {
        if (!configuration.TryGetValue(FilterConfigurationKey, out var filterItemKindRaw))
        {
            _filterItemKind = null;
            return configuration.Count == 0;
        }

        if (!Enum.TryParse<FactoryItemKind>(filterItemKindRaw, out var filterItemKind))
        {
            return false;
        }

        _filterItemKind = filterItemKind;
        return true;
    }

    public override string? CaptureMapRecipeId()
    {
        return _filterItemKind.HasValue ? BuildFilterRecipeId(_filterItemKind.Value) : null;
    }

    public override bool TryApplyMapRecipe(string recipeId)
    {
        if (string.IsNullOrWhiteSpace(recipeId))
        {
            _filterItemKind = null;
            return true;
        }

        return TrySetDetailRecipe(recipeId);
    }

    protected override void CaptureRuntimeState(FactoryStructureRuntimeSnapshot snapshot)
    {
        base.CaptureRuntimeState(snapshot);
        snapshot.State["swing_progress"] = FactoryRuntimeSnapshotValues.FormatFloat(_swingProgress);
        snapshot.State["is_returning"] = FactoryRuntimeSnapshotValues.FormatBool(_isReturning);
        if (_filterItemKind.HasValue)
        {
            snapshot.State[FilterConfigurationKey] = _filterItemKind.Value.ToString();
        }
        if (_heldItem is not null)
        {
            snapshot.State["held_item_id"] = FactoryRuntimeSnapshotValues.FormatInt(_heldItem.Id);
            snapshot.State["held_item_kind"] = _heldItem.ItemKind.ToString();
            snapshot.State["held_item_source"] = _heldItem.SourceKind.ToString();
        }
    }

    protected override void ApplyRuntimeState(FactoryStructureRuntimeSnapshot snapshot, SimulationController simulation)
    {
        base.ApplyRuntimeState(snapshot, simulation);
        _swingProgress = FactoryRuntimeSnapshotValues.TryGetFloat(snapshot.State, "swing_progress", out var swingProgress)
            ? Mathf.Clamp(swingProgress, 0.0f, 1.0f)
            : 0.0f;
        _isReturning = FactoryRuntimeSnapshotValues.TryGetBool(snapshot.State, "is_returning", out var isReturning) && isReturning;
        _filterItemKind = snapshot.State.TryGetValue(FilterConfigurationKey, out var filterItemKindRaw)
            && Enum.TryParse<FactoryItemKind>(filterItemKindRaw, out var filterItemKind)
                ? filterItemKind
                : null;

        _heldItem = null;
        if (FactoryRuntimeSnapshotValues.TryGetInt(snapshot.State, "held_item_id", out var itemId)
            && snapshot.State.TryGetValue("held_item_kind", out var itemKindRaw)
            && snapshot.State.TryGetValue("held_item_source", out var sourceKindRaw)
            && Enum.TryParse<FactoryItemKind>(itemKindRaw, out var itemKind)
            && Enum.TryParse<BuildPrototypeKind>(sourceKindRaw, out var sourceKind))
        {
            _heldItem = simulation.CreateItemWithId(itemId, sourceKind, itemKind);
        }

        if (_heldItem is null)
        {
            ClearHeldVisual();
            return;
        }

        RebuildHeldVisual();
    }

    protected override void BuildVisuals()
    {
        if (SiteKind == FactorySiteKind.Interior)
        {
            CreateBox("Base", new Vector3(CellSize * 0.82f, 0.12f, CellSize * 0.82f), new Color("0F172A"), new Vector3(0.0f, 0.06f, 0.0f));
            CreateInteriorTray(this, "InputTray", new Vector3(CellSize * 0.30f, 0.08f, CellSize * 0.18f), new Color("1D4ED8"), new Color("DBEAFE"), new Vector3(-CellSize * 0.26f, 0.12f, 0.0f));
            CreateInteriorTray(this, "OutputTray", new Vector3(CellSize * 0.30f, 0.08f, CellSize * 0.18f), new Color("0F766E"), new Color("CCFBF1"), new Vector3(CellSize * 0.26f, 0.12f, 0.0f));
            CreateBox("Column", new Vector3(CellSize * 0.16f, 0.52f, CellSize * 0.16f), new Color("475569"), new Vector3(0.0f, 0.38f, 0.0f));
            CreateBox("ServiceCap", new Vector3(CellSize * 0.26f, 0.10f, CellSize * 0.26f), new Color("CBD5E1"), new Vector3(0.0f, 0.66f, 0.0f));

            _shoulderPivot = new Node3D
            {
                Name = "ShoulderPivot",
                Position = new Vector3(0.0f, 0.68f, 0.0f)
            };
            AddChild(_shoulderPivot);

            CreateArmMesh(
                _shoulderPivot,
                "UpperArm",
                new Vector3(CellSize * 0.28f, 0.07f, 0.09f),
                new Color("94A3B8"),
                new Vector3(CellSize * 0.14f, 0.0f, 0.0f));

            _elbowPivot = new Node3D
            {
                Name = "ElbowPivot",
                Position = new Vector3(CellSize * 0.28f, 0.0f, 0.0f)
            };
            _shoulderPivot.AddChild(_elbowPivot);

            CreateArmMesh(
                _elbowPivot,
                "Forearm",
                new Vector3(CellSize * 0.24f, 0.07f, 0.08f),
                new Color("38BDF8"),
                new Vector3(CellSize * 0.12f, 0.0f, 0.0f));

            _claw = CreateArmMesh(
                _elbowPivot,
                "Claw",
                new Vector3(CellSize * 0.10f, 0.10f, 0.18f),
                new Color("F8FAFC"),
                new Vector3(CellSize * 0.24f, 0.0f, 0.0f));

            _heldVisualAnchor = new Node3D
            {
                Name = "HeldItemAnchor",
                Position = new Vector3(CellSize * 0.24f, CellSize * 0.09f, 0.0f)
            };
            _elbowPivot.AddChild(_heldVisualAnchor);

            CreateInteriorIndicatorLight(this, "ServiceLamp", new Color("67E8F9"), new Vector3(0.0f, 0.78f, 0.0f), CellSize * 0.07f);
            UpdateArmPose(0.0f);
            return;
        }

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

        _heldVisualAnchor = new Node3D
        {
            Name = "HeldItemAnchor",
            Position = new Vector3(CellSize * 0.30f, CellSize * 0.11f, 0.0f)
        };
        _elbowPivot.AddChild(_heldVisualAnchor);

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

        if (_heldVisualAnchor is not null)
        {
            _heldVisualAnchor.Rotation = new Vector3(
                Mathf.Lerp(-0.12f, 0.10f, eased),
                Mathf.Lerp(0.55f, -0.18f, eased),
                Mathf.Lerp(0.08f, -0.08f, eased));
        }
    }

    private void RebuildHeldVisual()
    {
        ClearHeldVisual();

        if (_heldItem is null || _heldVisualAnchor is null)
        {
            return;
        }

        var visual = FactoryTransportVisualFactory.CreateVisual(_heldItem, CellSize);
        visual.Name = $"Held_{_heldItem.ItemKind}";
        visual.Scale = Vector3.One * HeldVisualScale;
        visual.Position = Vector3.Zero;
        _heldVisualAnchor.AddChild(visual);
        _heldVisual = visual;
    }

    private void ClearHeldVisual()
    {
        if (_heldVisual is null)
        {
            return;
        }

        _heldVisual.QueueFree();
        _heldVisual = null;
    }

    private bool TryAcquireFilteredInput(SimulationController simulation, out FactoryItem? takenItem)
    {
        takenItem = null;
        if (!TryPeekFilteredInput(simulation, out var previewItem)
            || previewItem is null
            || !simulation.CanReceiveProvidedItem(this, Site, GetOutputCell(), previewItem))
        {
            return false;
        }

        return TryTakeFilteredInput(simulation, out takenItem) && takenItem is not null;
    }

    private bool TryPeekFilteredInput(SimulationController simulation, out FactoryItem? item)
    {
        item = null;
        if (Site.TryGetStructure(GetInputCell(), out var providerStructure)
            && providerStructure is IFactoryFilteredItemProvider filteredProvider)
        {
            return filteredProvider.TryPeekFilteredProvidedItem(Cell, simulation, _filterItemKind, out item);
        }

        if (!simulation.TryPeekProvidedItem(Site, GetInputCell(), Cell, out item) || item is null)
        {
            return false;
        }

        return MatchesFilter(item);
    }

    private bool TryTakeFilteredInput(SimulationController simulation, out FactoryItem? item)
    {
        item = null;
        if (Site.TryGetStructure(GetInputCell(), out var providerStructure)
            && providerStructure is IFactoryFilteredItemProvider filteredProvider)
        {
            return filteredProvider.TryTakeFilteredProvidedItem(Cell, simulation, _filterItemKind, out item);
        }

        if (!simulation.TryTakeProvidedItem(Site, GetInputCell(), Cell, out item) || item is null)
        {
            return false;
        }

        return MatchesFilter(item);
    }

    private bool MatchesFilter(FactoryItem item)
    {
        return !_filterItemKind.HasValue || item.ItemKind == _filterItemKind.Value;
    }

    private FactoryRecipeSectionModel BuildFilterSection()
    {
        var options = new List<FactoryRecipeOptionModel>
        {
            new(
                NoFilterRecipeId,
                "不过滤",
                "抓取来源结构当前可提供的任意物品。",
                !_filterItemKind.HasValue,
                FactoryPresentation.GetBuildPrototypeAccentColor(Kind),
                FactoryPresentation.GetItemIcon(FactoryItemKind.GenericCargo))
        };

        for (var index = 0; index < SelectableFilterKinds.Count; index++)
        {
            var itemKind = SelectableFilterKinds[index];
            options.Add(new FactoryRecipeOptionModel(
                BuildFilterRecipeId(itemKind),
                FactoryItemCatalog.GetDisplayName(itemKind),
                $"只抓取 {FactoryItemCatalog.GetDisplayName(itemKind)}；不匹配的物品会留在来源结构中。",
                _filterItemKind == itemKind,
                FactoryPresentation.GetItemAccentColor(itemKind),
                FactoryPresentation.GetItemIcon(itemKind)));
        }

        return new FactoryRecipeSectionModel(
            "抓取筛选",
            "切换机械臂允许抓取的物品种类。对传送带、仓储、中转缓冲和产物缓存都会生效。",
            _filterItemKind.HasValue ? BuildFilterRecipeId(_filterItemKind.Value) : NoFilterRecipeId,
            options);
    }

    private string DescribeFilter()
    {
        return _filterItemKind.HasValue
            ? FactoryItemCatalog.GetDisplayName(_filterItemKind.Value)
            : "不过滤";
    }

    private static bool TryResolveFilterFromRecipeId(string recipeId, out FactoryItemKind? itemKind)
    {
        itemKind = null;
        if (string.IsNullOrWhiteSpace(recipeId))
        {
            return false;
        }

        const string prefix = "inserter-filter-";
        if (!recipeId.StartsWith(prefix, StringComparison.Ordinal))
        {
            return false;
        }

        var itemKindRaw = recipeId[prefix.Length..];
        if (!Enum.TryParse<FactoryItemKind>(itemKindRaw, out var parsedItemKind)
            || parsedItemKind == FactoryItemKind.BuildingKit)
        {
            return false;
        }

        itemKind = parsedItemKind;
        return true;
    }

    private static string BuildFilterRecipeId(FactoryItemKind itemKind)
    {
        return $"inserter-filter-{itemKind}";
    }

    private static IReadOnlyList<FactoryItemKind> BuildSelectableFilterKinds()
    {
        var kinds = new List<FactoryItemKind>();
        foreach (var itemKind in Enum.GetValues<FactoryItemKind>())
        {
            if (itemKind == FactoryItemKind.BuildingKit)
            {
                continue;
            }

            kinds.Add(itemKind);
        }

        return kinds;
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
