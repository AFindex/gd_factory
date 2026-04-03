using Godot;
using System.Collections.Generic;

public abstract partial class FactoryRecipeMachineStructure : FactoryStructure, IFactoryItemProvider, IFactoryItemReceiver, IFactoryPowerConsumer
{
    private readonly FactorySlottedItemInventory _inputInventory;
    private readonly FactorySlottedItemInventory _outputInventory;

    private double _dispatchCooldown;
    private double _processProgress;
    private bool _isProcessing;
    private int _activeRecipeIndex;
    private float _powerSatisfaction = 1.0f;
    private FactoryPowerStatus _powerStatus = FactoryPowerStatus.Powered;
    private int _powerNetworkId = -1;

    protected FactoryRecipeMachineStructure(int inputColumns, int inputRows, int outputColumns = 1, int outputRows = 1)
    {
        _inputInventory = new FactorySlottedItemInventory(inputColumns, inputRows);
        _outputInventory = new FactorySlottedItemInventory(outputColumns, outputRows);
    }

    protected abstract IReadOnlyList<FactoryRecipeDefinition> AvailableRecipes { get; }
    protected virtual string DetailSubtitle => "制造缓存与配方状态";
    protected virtual string InputSectionTitle => "输入缓存";
    protected virtual string OutputSectionTitle => "输出缓存";
    protected virtual string InputInventoryId => "machine-input";
    protected virtual string OutputInventoryId => "machine-output";
    protected virtual string RecipeSectionTitle => "工艺配方";
    protected virtual string RecipeSectionDescription => "切换当前生产工艺。";
    protected virtual bool AllowInputItemMove => true;
    protected virtual bool AllowOutputItemMove => true;
    protected virtual int MachinePowerRangeCells => ActiveRecipe.PowerDemand > 0.0f ? 3 : 0;
    protected virtual bool SupportsRecipeSelection => AvailableRecipes.Count > 1;
    protected virtual bool CanRunRecipe(SimulationController simulation) => true;
    protected virtual float DispatchCooldownSeconds => 0.2f;
    protected virtual float MinimumPowerThreshold => 0.01f;

    protected FactoryRecipeDefinition ActiveRecipe => AvailableRecipes[Mathf.Clamp(_activeRecipeIndex, 0, AvailableRecipes.Count - 1)];
    protected FactorySlottedItemInventory InputInventory => _inputInventory;
    protected FactorySlottedItemInventory OutputInventory => _outputInventory;
    protected bool HasActiveProcess => _isProcessing;
    protected float ProcessRatio => ActiveRecipe.CycleSeconds <= 0.0f
        ? 0.0f
        : Mathf.Clamp((float)(_processProgress / ActiveRecipe.CycleSeconds), 0.0f, 1.0f);
    protected FactoryPowerStatus CurrentPowerStatus => _powerStatus;
    protected float CurrentPowerSatisfaction => _powerSatisfaction;
    protected int CurrentPowerNetworkId => _powerNetworkId;
    protected bool RequiresPower => ActiveRecipe.PowerDemand > 0.0f;
    protected bool HasBufferedOutput => !_outputInventory.IsEmpty;

    public int PowerConnectionRangeCells => MachinePowerRangeCells;

    public bool WantsPower(SimulationController simulation)
    {
        if (!RequiresPower)
        {
            return false;
        }

        return _isProcessing || (CanRunRecipe(simulation) && CanStartProcessing());
    }

    public float GetRequestedPower(SimulationController simulation)
    {
        return RequiresPower && WantsPower(simulation)
            ? ActiveRecipe.PowerDemand
            : 0.0f;
    }

    public void SetPowerState(FactoryPowerStatus status, float satisfaction, int networkId)
    {
        _powerStatus = status;
        _powerSatisfaction = Mathf.Clamp(satisfaction, 0.0f, 1.0f);
        _powerNetworkId = networkId;
    }

    public bool TryPeekProvidedItem(Vector2I requesterCell, SimulationController simulation, out FactoryItem? item)
    {
        item = null;
        if (!IsOrthogonallyAdjacent(Cell, requesterCell))
        {
            return false;
        }

        return _outputInventory.TryPeekFirst(out item);
    }

    public bool TryTakeProvidedItem(Vector2I requesterCell, SimulationController simulation, out FactoryItem? item)
    {
        item = null;
        if (!IsOrthogonallyAdjacent(Cell, requesterCell))
        {
            return false;
        }

        return _outputInventory.TryTakeFirst(out item);
    }

    public bool CanReceiveProvidedItem(FactoryItem item, Vector2I sourceCell, SimulationController simulation)
    {
        return CanAcceptInput(item, sourceCell);
    }

    public bool TryReceiveProvidedItem(FactoryItem item, Vector2I sourceCell, SimulationController simulation)
    {
        return CanAcceptInput(item, sourceCell) && _inputInventory.TryAddItem(item);
    }

    public override bool CanAcceptItem(FactoryItem item, Vector2I sourceCell, SimulationController simulation)
    {
        return CanAcceptInput(item, sourceCell);
    }

    public override bool TryAcceptItem(FactoryItem item, Vector2I sourceCell, SimulationController simulation)
    {
        return CanAcceptInput(item, sourceCell) && _inputInventory.TryAddItem(item);
    }

    public override void SimulationStep(SimulationController simulation, double stepSeconds)
    {
        _dispatchCooldown = Mathf.Max(0.0, (float)(_dispatchCooldown - stepSeconds));
        TryDispatchBufferedOutput(simulation);

        if (_isProcessing)
        {
            if (RequiresPower && _powerSatisfaction < MinimumPowerThreshold)
            {
                return;
            }

            var progressStep = RequiresPower
                ? stepSeconds * _powerSatisfaction
                : stepSeconds;
            _processProgress += progressStep;
            if (_processProgress < ActiveRecipe.CycleSeconds)
            {
                return;
            }

            CompleteRecipe(simulation);
            _isProcessing = false;
            _processProgress = 0.0;
            return;
        }

        if (!CanRunRecipe(simulation) || !CanStartProcessing())
        {
            return;
        }

        if (RequiresPower && _powerSatisfaction < MinimumPowerThreshold)
        {
            return;
        }

        ConsumeInputs();
        _isProcessing = true;
        _processProgress = 0.0;
    }

    public override IEnumerable<string> GetInspectionLines()
    {
        foreach (var line in base.GetInspectionLines())
        {
            yield return line;
        }

        yield return $"配方：{ActiveRecipe.DisplayName}";
        yield return $"节拍：{ActiveRecipe.CycleSeconds:0.00} 秒";
        yield return $"输入缓存：{_inputInventory.Count}";
        yield return $"输出缓存：{_outputInventory.Count}";
        yield return _isProcessing
            ? $"进度：{ProcessRatio * 100.0f:0}%"
            : HasBufferedOutput
                ? "状态：产物待输出"
                : "状态：待机";

        if (RequiresPower)
        {
            yield return $"供电：{FactoryPowerPresentation.ToLabel(_powerStatus)} ({_powerSatisfaction * 100.0f:0}%)";
        }
    }

    public override FactoryStructureDetailModel GetDetailModel()
    {
        var summaryLines = new List<string>();
        foreach (var line in GetInspectionLines())
        {
            summaryLines.Add(line);
        }

        var sections = new List<FactoryInventorySectionModel>();
        if (ActiveRecipe.Inputs.Count > 0)
        {
            sections.Add(CreateInventorySection(InputInventoryId, InputSectionTitle, _inputInventory, AllowInputItemMove));
        }

        sections.Add(CreateInventorySection(OutputInventoryId, OutputSectionTitle, _outputInventory, AllowOutputItemMove));

        FactoryRecipeSectionModel? recipeSection = null;
        if (SupportsRecipeSelection)
        {
            var options = new List<FactoryRecipeOptionModel>();
            for (var index = 0; index < AvailableRecipes.Count; index++)
            {
                var recipe = AvailableRecipes[index];
                options.Add(new FactoryRecipeOptionModel(
                    recipe.Id,
                    recipe.DisplayName,
                    BuildRecipeSummary(recipe),
                    index == _activeRecipeIndex));
            }

            recipeSection = new FactoryRecipeSectionModel(
                RecipeSectionTitle,
                RecipeSectionDescription,
                ActiveRecipe.Id,
                options);
        }

        return new FactoryStructureDetailModel(
            InspectionTitle,
            DetailSubtitle,
            summaryLines,
            sections,
            recipeSection);
    }

    public override bool TryMoveDetailInventoryItem(string inventoryId, Vector2I fromSlot, Vector2I toSlot)
    {
        return (inventoryId == InputInventoryId && _inputInventory.TryMoveItem(fromSlot, toSlot))
            || (inventoryId == OutputInventoryId && _outputInventory.TryMoveItem(fromSlot, toSlot));
    }

    public override bool TrySetDetailRecipe(string recipeId)
    {
        if (!SupportsRecipeSelection || _isProcessing)
        {
            return false;
        }

        for (var index = 0; index < AvailableRecipes.Count; index++)
        {
            if (AvailableRecipes[index].Id != recipeId)
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

    protected void SetActiveRecipeById(string recipeId)
    {
        for (var index = 0; index < AvailableRecipes.Count; index++)
        {
            if (AvailableRecipes[index].Id == recipeId)
            {
                _activeRecipeIndex = index;
                return;
            }
        }
    }

    protected virtual string BuildRecipeSummary(FactoryRecipeDefinition recipe)
    {
        var inputText = recipe.Inputs.Count == 0
            ? "无输入"
            : string.Join(" + ", BuildIngredientText(recipe.Inputs));
        var outputText = string.Join(" + ", BuildOutputText(recipe.Outputs));
        var powerText = recipe.PowerDemand > 0.0f ? $" | {recipe.PowerDemand:0} kW" : string.Empty;
        return $"{inputText} -> {outputText} | {recipe.CycleSeconds:0.00}s{powerText}";
    }

    private bool CanAcceptInput(FactoryItem item, Vector2I sourceCell)
    {
        if (!IsOrthogonallyAdjacent(Cell, sourceCell) || ActiveRecipe.Inputs.Count == 0 || _inputInventory.IsFull)
        {
            return false;
        }

        for (var index = 0; index < ActiveRecipe.Inputs.Count; index++)
        {
            var ingredient = ActiveRecipe.Inputs[index];
            if (ingredient.ItemKind != item.ItemKind)
            {
                continue;
            }

            return _inputInventory.CountByKind(item.ItemKind) < ingredient.Amount * 2;
        }

        return false;
    }

    private bool CanStartProcessing()
    {
        if (_isProcessing || !CanStoreAllOutputs())
        {
            return false;
        }

        for (var index = 0; index < ActiveRecipe.Inputs.Count; index++)
        {
            var ingredient = ActiveRecipe.Inputs[index];
            if (_inputInventory.CountByKind(ingredient.ItemKind) < ingredient.Amount)
            {
                return false;
            }
        }

        return true;
    }

    private bool CanStoreAllOutputs()
    {
        var freeSlots = _outputInventory.Capacity - _outputInventory.Count;
        var neededSlots = 0;
        for (var index = 0; index < ActiveRecipe.Outputs.Count; index++)
        {
            neededSlots += ActiveRecipe.Outputs[index].Amount;
        }

        return freeSlots >= neededSlots;
    }

    private void ConsumeInputs()
    {
        for (var index = 0; index < ActiveRecipe.Inputs.Count; index++)
        {
            var ingredient = ActiveRecipe.Inputs[index];
            for (var amountIndex = 0; amountIndex < ingredient.Amount; amountIndex++)
            {
                _inputInventory.TryTakeFirstMatching(ingredient.ItemKind, out _);
            }
        }
    }

    private void CompleteRecipe(SimulationController simulation)
    {
        for (var index = 0; index < ActiveRecipe.Outputs.Count; index++)
        {
            var output = ActiveRecipe.Outputs[index];
            for (var amountIndex = 0; amountIndex < output.Amount; amountIndex++)
            {
                _outputInventory.TryAddItem(simulation.CreateItem(Kind, output.ItemKind));
            }
        }
    }

    private void TryDispatchBufferedOutput(SimulationController simulation)
    {
        if (_dispatchCooldown > 0.0 || !_outputInventory.TryPeekFirst(out var item) || item is null)
        {
            return;
        }

        if (simulation.TrySendItem(this, GetOutputCell(), item))
        {
            _outputInventory.TryTakeFirst(out _);
            _dispatchCooldown = DispatchCooldownSeconds;
        }
    }

    private static IEnumerable<string> BuildIngredientText(IReadOnlyList<FactoryRecipeIngredientDefinition> inputs)
    {
        for (var index = 0; index < inputs.Count; index++)
        {
            var ingredient = inputs[index];
            yield return $"{FactoryPresentation.GetItemLabel(new FactoryItem(0, ingredient.ItemKind switch
            {
                FactoryItemKind.MachinePart => BuildPrototypeKind.Assembler,
                FactoryItemKind.AmmoMagazine => BuildPrototypeKind.Assembler,
                FactoryItemKind.HighVelocityAmmo => BuildPrototypeKind.Assembler,
                _ => BuildPrototypeKind.Producer
            }, ingredient.ItemKind)).Replace(" #0", string.Empty)} x{ingredient.Amount}";
        }
    }

    private static IEnumerable<string> BuildOutputText(IReadOnlyList<FactoryRecipeOutputDefinition> outputs)
    {
        for (var index = 0; index < outputs.Count; index++)
        {
            var output = outputs[index];
            yield return $"{FactoryPresentation.GetItemLabel(new FactoryItem(0, output.ItemKind switch
            {
                FactoryItemKind.MachinePart => BuildPrototypeKind.Assembler,
                FactoryItemKind.AmmoMagazine => BuildPrototypeKind.Assembler,
                FactoryItemKind.HighVelocityAmmo => BuildPrototypeKind.Assembler,
                _ => BuildPrototypeKind.Producer
            }, output.ItemKind)).Replace(" #0", string.Empty)} x{output.Amount}";
        }
    }
}
