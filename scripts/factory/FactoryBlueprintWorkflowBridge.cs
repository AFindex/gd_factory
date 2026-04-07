using Godot;

public static class FactoryBlueprintWorkflowBridge
{
    public static FactoryBlueprintRecord SavePendingCapture(
        FactoryBlueprintRecord pendingCapture,
        string requestedName,
        FactoryBlueprintPersistenceTarget target = FactoryBlueprintPersistenceTarget.Runtime)
    {
        var displayName = string.IsNullOrWhiteSpace(requestedName)
            ? pendingCapture.DisplayName
            : requestedName.Trim();
        var savedRecord = new FactoryBlueprintRecord(
            pendingCapture.Id,
            displayName,
            pendingCapture.SourceSiteKind,
            pendingCapture.SuggestedAnchorCell,
            pendingCapture.BoundsSize,
            pendingCapture.Entries,
            pendingCapture.RequiredAttachments);
        FactoryBlueprintLibrary.AddOrUpdate(savedRecord, target);
        FactoryBlueprintLibrary.SelectActive(savedRecord.Id);
        return savedRecord;
    }

    public static void SelectBlueprint(
        string blueprintId,
        FactoryBlueprintWorkflowMode workflowMode,
        System.Action refreshApplyPreview)
    {
        FactoryBlueprintLibrary.SelectActive(blueprintId);
        if (workflowMode == FactoryBlueprintWorkflowMode.ApplyPreview)
        {
            refreshApplyPreview();
        }
    }

    public static void DeleteBlueprint(string blueprintId, System.Action onNoActiveBlueprint)
    {
        FactoryBlueprintLibrary.Remove(blueprintId);
        if (FactoryBlueprintLibrary.GetActive() is null)
        {
            onNoActiveBlueprint();
        }
    }

    public static bool HandleBlueprintWorkspaceExit(
        string workspaceId,
        string blueprintWorkspaceId,
        bool hasActiveBlueprintState,
        System.Action clearWorkflow,
        out string? message)
    {
        if (workspaceId != blueprintWorkspaceId && hasActiveBlueprintState)
        {
            clearWorkflow();
            message = "已切换离开蓝图工作区，并清除当前蓝图选择。";
            return true;
        }

        message = null;
        return false;
    }

    public static string BuildActiveBlueprintText()
    {
        var activeBlueprint = FactoryBlueprintLibrary.GetActive();
        return activeBlueprint is null
            ? "当前蓝图：未选择"
            : $"当前蓝图：{activeBlueprint.DisplayName} ({activeBlueprint.GetSummaryText()})";
    }
}
