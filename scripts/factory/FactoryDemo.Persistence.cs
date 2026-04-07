using System;

public partial class FactoryDemo
{
    private void InitializePersistenceHud()
    {
        _hud?.SetPersistenceStatus(FactoryPersistencePaths.BuildPersistenceSummary(includeInteriorMap: false));
    }

    private void HandleMapSaveRequested()
    {
        SaveCurrentWorldMap(saveToSource: false);
    }

    private void HandleMapSourceSaveRequested()
    {
        SaveCurrentWorldMap(saveToSource: true);
    }

    private void SaveCurrentWorldMap(bool saveToSource)
    {
        if (_grid is null)
        {
            return;
        }

        var sourcePath = DemoLaunchOptions.ResolveFactoryWorldMapPath();
        try
        {
            var result = saveToSource
                ? FactoryMapPersistence.SaveWorldMapToSource(sourcePath, _grid)
                : FactoryMapPersistence.SaveWorldMap(sourcePath, _grid);
            var actionLabel = saveToSource ? "已保存到当前地图源" : "已导出运行时副本";
            _previewMessage = $"{actionLabel}：{result.ResourcePath}";
            _hud?.SetPersistenceStatus($"世界地图已保存：{result.GlobalPath}\n当前地图源：{sourcePath}\n蓝图目录：{FactoryPersistencePaths.GetBlueprintDirectoryGlobalPath()}");
        }
        catch (Exception ex)
        {
            _previewMessage = $"地图保存失败：{ex.Message}";
            _hud?.SetPersistenceStatus($"地图保存失败：{ex.Message}");
        }

        UpdateHud();
    }

    private void ShowBlueprintPersistenceStatus(FactoryBlueprintRecord savedRecord)
    {
        if (_hud is null)
        {
            return;
        }

        var blueprintPath = FactoryPersistencePaths.GetGlobalPath(FactoryPersistencePaths.BuildBlueprintFilePath(savedRecord.Id));
        _previewMessage = $"已保存蓝图：{savedRecord.DisplayName}\n{blueprintPath}";
        _hud.SetPersistenceStatus($"蓝图已保存：{blueprintPath}\n世界地图目录：{FactoryPersistencePaths.GetWorldMapDirectoryGlobalPath()}");
    }
}
