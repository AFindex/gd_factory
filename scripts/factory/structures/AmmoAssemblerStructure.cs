using Godot;
using System.Collections.Generic;

public partial class AmmoAssemblerStructure : FactoryStructure, IFactoryItemProvider
{
    private double _cooldown;
    private FactoryItem? _bufferedAmmo;
    private MeshInstance3D? _indicator;
    private int _activeRecipeIndex;

    public override BuildPrototypeKind Kind => BuildPrototypeKind.AmmoAssembler;
    public override string Description => "持续生产弹药并向前方防线补给。";
    public override float MaxHealth => 62.0f;
    private FactoryRecipeDefinition ActiveRecipe => FactoryRecipeCatalog.AmmoAssemblerRecipes[_activeRecipeIndex];

    public override void SimulationStep(SimulationController simulation, double stepSeconds)
    {
        _cooldown -= stepSeconds;
        if (_cooldown > 0.0)
        {
            return;
        }

        _bufferedAmmo ??= simulation.CreateItem(Kind, ActiveRecipe.OutputItemKind);
        if (simulation.TrySendItem(this, GetOutputCell(), _bufferedAmmo))
        {
            _bufferedAmmo = null;
            _cooldown = ActiveRecipe.CycleSeconds;
            if (_indicator is not null)
            {
                _indicator.Scale = new Vector3(1.1f, 1.18f, 1.1f);
            }
        }
    }

    public bool TryPeekProvidedItem(Vector2I requesterCell, SimulationController simulation, out FactoryItem? item)
    {
        item = null;
        if (!IsOrthogonallyAdjacent(Cell, requesterCell) || _bufferedAmmo is null)
        {
            return false;
        }

        item = _bufferedAmmo;
        return true;
    }

    public bool TryTakeProvidedItem(Vector2I requesterCell, SimulationController simulation, out FactoryItem? item)
    {
        item = null;
        if (!TryPeekProvidedItem(requesterCell, simulation, out var previewItem))
        {
            return false;
        }

        item = previewItem;
        _bufferedAmmo = null;
        _cooldown = ActiveRecipe.CycleSeconds;
        return true;
    }

    public override IEnumerable<string> GetInspectionLines()
    {
        foreach (var line in base.GetInspectionLines())
        {
            yield return line;
        }

        yield return $"配方：{ActiveRecipe.DisplayName}";
        yield return $"节拍：{ActiveRecipe.CycleSeconds:0.00} 秒/批";
        yield return $"产出：{(_bufferedAmmo is null ? "生产中" : FactoryPresentation.GetItemLabel(_bufferedAmmo))}";
    }

    public override FactoryStructureDetailModel GetDetailModel()
    {
        var summaryLines = new List<string>();
        foreach (var line in GetInspectionLines())
        {
            summaryLines.Add(line);
        }

        var outputSlot = new FactoryInventorySlotModel(
            Vector2I.Zero,
            _bufferedAmmo is null ? null : _bufferedAmmo.Id.ToString(),
            _bufferedAmmo is null ? null : FactoryPresentation.GetItemLabel(_bufferedAmmo),
            _bufferedAmmo is null ? "当前缓存：生产中" : "当前缓存：待输出弹药",
            _bufferedAmmo is null ? new Color("475569") : FactoryPresentation.GetItemAccentColor(_bufferedAmmo.ItemKind));

        var inventorySection = new FactoryInventorySectionModel(
            "ammo-output",
            "弹药缓存",
            new Vector2I(1, 1),
            new[] { outputSlot },
            false);

        var recipeOptions = new List<FactoryRecipeOptionModel>();
        for (var index = 0; index < FactoryRecipeCatalog.AmmoAssemblerRecipes.Count; index++)
        {
            var recipe = FactoryRecipeCatalog.AmmoAssemblerRecipes[index];
            recipeOptions.Add(new FactoryRecipeOptionModel(
                recipe.Id,
                recipe.DisplayName,
                $"{recipe.Summary} | 节拍 {recipe.CycleSeconds:0.00}s",
                index == _activeRecipeIndex));
        }

        var recipeSection = new FactoryRecipeSectionModel(
            "装配配方",
            "切换当前弹药装配方案。",
            ActiveRecipe.Id,
            recipeOptions);

        return new FactoryStructureDetailModel(
            InspectionTitle,
            "弹药产线缓存与配方",
            summaryLines,
            new[] { inventorySection },
            recipeSection);
    }

    public override bool TrySetDetailRecipe(string recipeId)
    {
        for (var index = 0; index < FactoryRecipeCatalog.AmmoAssemblerRecipes.Count; index++)
        {
            if (FactoryRecipeCatalog.AmmoAssemblerRecipes[index].Id != recipeId)
            {
                continue;
            }

            _activeRecipeIndex = index;
            return true;
        }

        return false;
    }

    public override IReadOnlyDictionary<string, string> CaptureBlueprintConfiguration()
    {
        return new Dictionary<string, string>
        {
            ["recipe_id"] = ActiveRecipe.Id
        };
    }

    public override bool ApplyBlueprintConfiguration(IReadOnlyDictionary<string, string> configuration)
    {
        if (!configuration.TryGetValue("recipe_id", out var recipeId))
        {
            return configuration.Count == 0;
        }

        return TrySetDetailRecipe(recipeId);
    }

    public override void UpdateVisuals(float tickAlpha)
    {
        if (_indicator is not null)
        {
            var targetScale = _bufferedAmmo is null ? Vector3.One : new Vector3(1.15f, 1.15f, 1.15f);
            _indicator.Scale = _indicator.Scale.Lerp(targetScale, tickAlpha * 0.45f);
        }
    }

    protected override void BuildVisuals()
    {
        CreateBox("Base", new Vector3(CellSize * 0.9f, 0.22f, CellSize * 0.9f), new Color("3F3F46"), new Vector3(0.0f, 0.11f, 0.0f));
        CreateBox("Body", new Vector3(CellSize * 0.76f, 0.92f, CellSize * 0.76f), new Color("71717A"), new Vector3(0.0f, 0.68f, 0.0f));
        CreateBox("MagazineRack", new Vector3(CellSize * 0.28f, 0.48f, CellSize * 0.58f), new Color("F59E0B"), new Vector3(-0.14f, 1.08f, 0.0f));
        _indicator = CreateBox("Beacon", new Vector3(CellSize * 0.20f, 0.20f, CellSize * 0.20f), new Color("FDE68A"), new Vector3(CellSize * 0.26f, 1.18f, 0.0f));
    }
}
