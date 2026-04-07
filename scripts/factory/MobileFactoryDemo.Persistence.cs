using System;

public partial class MobileFactoryDemo
{
    private void InitializePersistenceHud()
    {
        _hud?.SetPersistenceStatus(FactoryPersistencePaths.BuildPersistenceSummary(includeInteriorMap: true));
    }

    private void HandleWorldMapSaveRequested()
    {
        SaveCurrentWorldMap(saveToSource: false);
    }

    private void HandleWorldMapSourceSaveRequested()
    {
        SaveCurrentWorldMap(saveToSource: true);
    }

    private void SaveCurrentWorldMap(bool saveToSource)
    {
        if (_grid is null)
        {
            return;
        }

        var sourcePath = DemoLaunchOptions.ResolveMobileWorldMapPath();
        try
        {
            var result = saveToSource
                ? FactoryMapPersistence.SaveWorldMapToSource(sourcePath, _grid)
                : FactoryMapPersistence.SaveWorldMap(sourcePath, _grid);
            var status = saveToSource
                ? $"世界地图已保存到当前源：{result.GlobalPath}"
                : $"世界地图运行时副本已导出：{result.GlobalPath}";
            _worldPreviewMessage = saveToSource
                ? $"已保存世界地图到当前源：{result.ResourcePath}"
                : $"已导出世界地图副本：{result.ResourcePath}";
            _hud?.SetPersistenceStatus($"{status}\n世界地图源：{sourcePath}\n内部地图目录：{FactoryPersistencePaths.GetInteriorMapDirectoryGlobalPath()}\n蓝图目录：{FactoryPersistencePaths.GetBlueprintDirectoryGlobalPath()}");
            ShowWorldEvent(status, true);
        }
        catch (Exception ex)
        {
            var status = $"世界地图保存失败：{ex.Message}";
            _worldPreviewMessage = status;
            _hud?.SetPersistenceStatus(status);
            ShowWorldEvent(status, false);
        }

        UpdateHud();
    }

    private void HandleInteriorMapSaveRequested()
    {
        SaveCurrentInteriorMap(saveToSource: false);
    }

    private void HandleInteriorMapSourceSaveRequested()
    {
        SaveCurrentInteriorMap(saveToSource: true);
    }

    private void SaveCurrentInteriorMap(bool saveToSource)
    {
        if (_mobileFactory is null)
        {
            return;
        }

        var sourcePath = DemoLaunchOptions.ResolveMobileInteriorMapPath();
        try
        {
            var result = saveToSource
                ? FactoryMapPersistence.SaveInteriorMapToSource(
                    sourcePath,
                    _mobileFactory.InteriorSite,
                    _mobileFactory.Profile.Id)
                : FactoryMapPersistence.SaveInteriorMap(
                    sourcePath,
                    _mobileFactory.InteriorSite,
                    _mobileFactory.Profile.Id);
            var status = saveToSource
                ? $"内部地图已保存到当前源：{result.GlobalPath}"
                : $"内部地图运行时副本已导出：{result.GlobalPath}";
            _interiorPreviewMessage = saveToSource
                ? $"已保存内部地图到当前源：{result.ResourcePath}"
                : $"已导出内部地图副本：{result.ResourcePath}";
            _hud?.SetPersistenceStatus($"世界地图目录：{FactoryPersistencePaths.GetWorldMapDirectoryGlobalPath()}\n{status}\n内部地图源：{sourcePath}\n蓝图目录：{FactoryPersistencePaths.GetBlueprintDirectoryGlobalPath()}");
            ShowWorldEvent(status, true);
        }
        catch (Exception ex)
        {
            var status = $"内部地图保存失败：{ex.Message}";
            _interiorPreviewMessage = status;
            _hud?.SetPersistenceStatus(status);
            ShowWorldEvent(status, false);
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
        _interiorPreviewMessage = $"已保存蓝图：{savedRecord.DisplayName}\n{blueprintPath}";
        _hud.SetPersistenceStatus($"蓝图已保存：{blueprintPath}\n世界地图目录：{FactoryPersistencePaths.GetWorldMapDirectoryGlobalPath()}\n内部地图目录：{FactoryPersistencePaths.GetInteriorMapDirectoryGlobalPath()}");
    }
}
