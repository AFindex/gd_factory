using Godot;
using System.Collections.Generic;

public partial class ProducerStructure : FactoryStructure, IFactoryItemProvider
{
    private double _cooldown;
    private MeshInstance3D? _indicator;
    private FactoryItem? _bufferedItem;
    private int _activeRecipeIndex;

    public override BuildPrototypeKind Kind => BuildPrototypeKind.Producer;
    private FactoryRecipeDefinition ActiveRecipe => FactoryRecipeCatalog.ProducerRecipes[_activeRecipeIndex];

    public override string Description => "Spawner feeding one item forward every few ticks.";

    public override void SimulationStep(SimulationController simulation, double stepSeconds)
    {
        _cooldown -= stepSeconds;

        if (_cooldown > 0.0)
        {
            return;
        }

        _bufferedItem ??= simulation.CreateItem(Kind, ActiveRecipe.OutputItemKind);
        if (simulation.TrySendItem(this, GetOutputCell(), _bufferedItem))
        {
            _bufferedItem = null;
            _cooldown = ActiveRecipe.CycleSeconds;
            if (_indicator is not null)
            {
                _indicator.Scale = new Vector3(1.0f, 1.2f, 1.0f);
            }
        }
    }

    public bool TryPeekProvidedItem(Vector2I requesterCell, SimulationController simulation, out FactoryItem? item)
    {
        item = null;

        if (!IsOrthogonallyAdjacent(Cell, requesterCell) || _bufferedItem is null)
        {
            return false;
        }

        item = _bufferedItem;
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
        _bufferedItem = null;
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
        yield return $"节拍：{ActiveRecipe.CycleSeconds:0.00} 秒/件";
        yield return $"产出：{(_bufferedItem is null ? "生产中" : FactoryPresentation.GetItemLabel(_bufferedItem))}";
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
            _bufferedItem is null ? null : _bufferedItem.Id.ToString(),
            _bufferedItem is null ? null : FactoryPresentation.GetItemLabel(_bufferedItem),
            _bufferedItem is null ? "当前缓存：空" : "当前缓存：待输出物料",
            _bufferedItem is null ? new Color("475569") : FactoryPresentation.GetItemAccentColor(_bufferedItem.ItemKind));

        var inventorySection = new FactoryInventorySectionModel(
            "producer-output",
            "输出缓存",
            new Vector2I(1, 1),
            new[] { outputSlot },
            false);

        var recipeOptions = new List<FactoryRecipeOptionModel>();
        for (var index = 0; index < FactoryRecipeCatalog.ProducerRecipes.Count; index++)
        {
            var recipe = FactoryRecipeCatalog.ProducerRecipes[index];
            recipeOptions.Add(new FactoryRecipeOptionModel(
                recipe.Id,
                recipe.DisplayName,
                $"{recipe.Summary} | 节拍 {recipe.CycleSeconds:0.00}s",
                index == _activeRecipeIndex));
        }

        var recipeSection = new FactoryRecipeSectionModel(
            "生产配方",
            "切换生产器当前输出类型。",
            ActiveRecipe.Id,
            recipeOptions);

        return new FactoryStructureDetailModel(
            InspectionTitle,
            "生产缓存与配方切换",
            summaryLines,
            new[] { inventorySection },
            recipeSection);
    }

    public override bool TrySetDetailRecipe(string recipeId)
    {
        for (var index = 0; index < FactoryRecipeCatalog.ProducerRecipes.Count; index++)
        {
            if (FactoryRecipeCatalog.ProducerRecipes[index].Id != recipeId)
            {
                continue;
            }

            _activeRecipeIndex = index;
            return true;
        }

        return false;
    }

    public override void UpdateVisuals(float tickAlpha)
    {
        if (_indicator is not null)
        {
            var targetScale = _bufferedItem is null ? Vector3.One : new Vector3(1.15f, 1.15f, 1.15f);
            _indicator.Scale = _indicator.Scale.Lerp(targetScale, tickAlpha * 0.5f);
        }
    }

    protected override void BuildVisuals()
    {
        CreateBox("Base", new Vector3(CellSize * 0.9f, 0.8f, CellSize * 0.9f), new Color("6D8B74"), new Vector3(0.0f, 0.4f, 0.0f));
        CreateBox("Tower", new Vector3(CellSize * 0.45f, 1.4f, CellSize * 0.45f), new Color("9DC08B"), new Vector3(-0.15f, 1.1f, 0.0f));
        _indicator = CreateBox("Outlet", new Vector3(CellSize * 0.35f, 0.2f, CellSize * 0.35f), new Color("D7FFC2"), new Vector3(CellSize * 0.45f, 0.75f, 0.0f));
    }
}
