using Godot;

public partial class FactoryDemo
{
    private void LoadStarterWorldMap()
    {
        if (_grid is null || _structureRoot is null || _simulation is null)
        {
            return;
        }

        FactoryMapRuntimeLoader.LoadWorldMap(
            FactoryMapPaths.StaticSandboxWorld,
            _grid,
            _structureRoot,
            _simulation);
    }

    private static bool RunFactoryMapSmokeChecks()
    {
        return FactoryMapSmokeSupport.VerifyDocuments(FactoryMapPaths.StaticSandboxWorld);
    }
}
