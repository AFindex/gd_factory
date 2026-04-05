using Godot;
using System.Collections.Generic;
using System.Text;

public interface IFactoryStructureDetailProvider
{
    FactoryStructureDetailModel GetDetailModel();
    bool TryMoveDetailInventoryItem(string inventoryId, Vector2I fromSlot, Vector2I toSlot);
    bool TrySetDetailRecipe(string recipeId);
    bool TryInvokeDetailAction(string actionId);
}

public sealed class FactoryStructureDetailModel
{
    public FactoryStructureDetailModel(
        string title,
        string? subtitle,
        IReadOnlyList<string> summaryLines,
        IReadOnlyList<FactoryInventorySectionModel>? inventorySections = null,
        FactoryRecipeSectionModel? recipeSection = null,
        IReadOnlyList<FactoryDetailActionModel>? actions = null)
    {
        Title = title;
        Subtitle = subtitle ?? string.Empty;
        SummaryLines = summaryLines;
        InventorySections = inventorySections ?? System.Array.Empty<FactoryInventorySectionModel>();
        RecipeSection = recipeSection;
        Actions = actions ?? System.Array.Empty<FactoryDetailActionModel>();
    }

    public string Title { get; }
    public string Subtitle { get; }
    public IReadOnlyList<string> SummaryLines { get; }
    public IReadOnlyList<FactoryInventorySectionModel> InventorySections { get; }
    public FactoryRecipeSectionModel? RecipeSection { get; }
    public IReadOnlyList<FactoryDetailActionModel> Actions { get; }

    public string BuildSignature()
    {
        var builder = new StringBuilder();
        builder.Append(Title).Append('|').Append(Subtitle);

        for (var index = 0; index < SummaryLines.Count; index++)
        {
            builder.Append("|summary:").Append(SummaryLines[index]);
        }

        for (var sectionIndex = 0; sectionIndex < InventorySections.Count; sectionIndex++)
        {
            var section = InventorySections[sectionIndex];
            builder.Append("|inventory:")
                .Append(section.InventoryId)
                .Append(':')
                .Append(section.Title)
                .Append(':')
                .Append(section.GridSize.X)
                .Append('x')
                .Append(section.GridSize.Y)
                .Append(':')
                .Append(section.AllowItemMove ? "1" : "0");

            for (var slotIndex = 0; slotIndex < section.Slots.Count; slotIndex++)
            {
                var slot = section.Slots[slotIndex];
                builder.Append("|slot:")
                    .Append(slot.Position.X)
                    .Append(',')
                    .Append(slot.Position.Y)
                    .Append(':')
                    .Append(slot.ItemId ?? "-")
                    .Append(':')
                    .Append(slot.ItemLabel ?? "-");
            }
        }

        if (RecipeSection is not null)
        {
            builder.Append("|recipe:")
                .Append(RecipeSection.Title)
                .Append(':')
                .Append(RecipeSection.ActiveRecipeId ?? string.Empty)
                .Append(':')
                .Append(RecipeSection.Description ?? string.Empty);

            for (var optionIndex = 0; optionIndex < RecipeSection.Options.Count; optionIndex++)
            {
                var option = RecipeSection.Options[optionIndex];
                builder.Append("|recipe-option:")
                    .Append(option.RecipeId)
                    .Append(':')
                    .Append(option.DisplayName)
                    .Append(':')
                    .Append(option.Summary)
                    .Append(':')
                    .Append(option.IsActive ? "1" : "0");
            }
        }

        for (var actionIndex = 0; actionIndex < Actions.Count; actionIndex++)
        {
            var action = Actions[actionIndex];
            builder.Append("|action:")
                .Append(action.ActionId)
                .Append(':')
                .Append(action.Label)
                .Append(':')
                .Append(action.Description ?? string.Empty)
                .Append(':')
                .Append(action.IsEnabled ? "1" : "0");
        }

        return builder.ToString();
    }
}

public sealed class FactoryDetailActionModel
{
    public FactoryDetailActionModel(string actionId, string label, string? description, bool isEnabled = true)
    {
        ActionId = actionId;
        Label = label;
        Description = description ?? string.Empty;
        IsEnabled = isEnabled;
    }

    public string ActionId { get; }
    public string Label { get; }
    public string Description { get; }
    public bool IsEnabled { get; }
}

public sealed class FactoryInventorySectionModel
{
    public FactoryInventorySectionModel(
        string inventoryId,
        string title,
        Vector2I gridSize,
        IReadOnlyList<FactoryInventorySlotModel> slots,
        bool allowItemMove)
    {
        InventoryId = inventoryId;
        Title = title;
        GridSize = gridSize;
        Slots = slots;
        AllowItemMove = allowItemMove;
    }

    public string InventoryId { get; }
    public string Title { get; }
    public Vector2I GridSize { get; }
    public IReadOnlyList<FactoryInventorySlotModel> Slots { get; }
    public bool AllowItemMove { get; }
}

public sealed class FactoryInventorySlotModel
{
    public FactoryInventorySlotModel(
        Vector2I position,
        string? itemId,
        string? itemLabel,
        string? itemDescription,
        Color accentColor)
    {
        Position = position;
        ItemId = itemId;
        ItemLabel = itemLabel;
        ItemDescription = itemDescription;
        AccentColor = accentColor;
    }

    public Vector2I Position { get; }
    public string? ItemId { get; }
    public string? ItemLabel { get; }
    public string? ItemDescription { get; }
    public Color AccentColor { get; }
    public bool HasItem => !string.IsNullOrWhiteSpace(ItemId);
}

public sealed class FactoryRecipeSectionModel
{
    public FactoryRecipeSectionModel(
        string title,
        string? description,
        string? activeRecipeId,
        IReadOnlyList<FactoryRecipeOptionModel> options)
    {
        Title = title;
        Description = description ?? string.Empty;
        ActiveRecipeId = activeRecipeId;
        Options = options;
    }

    public string Title { get; }
    public string Description { get; }
    public string? ActiveRecipeId { get; }
    public IReadOnlyList<FactoryRecipeOptionModel> Options { get; }
}

public sealed class FactoryRecipeOptionModel
{
    public FactoryRecipeOptionModel(string recipeId, string displayName, string summary, bool isActive)
    {
        RecipeId = recipeId;
        DisplayName = displayName;
        Summary = summary;
        IsActive = isActive;
    }

    public string RecipeId { get; }
    public string DisplayName { get; }
    public string Summary { get; }
    public bool IsActive { get; }
}
