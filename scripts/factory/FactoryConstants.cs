using Godot;

public static class FactoryConstants
{
    public const float CellSize = 2.0f;
    public const float HalfCell = CellSize * 0.5f;
    public const int GridMin = -12;
    public const int GridMax = 12;
    public const float SimulationStepSeconds = 0.05f;
    public const float ProducerSpawnSeconds = 0.8f;
    public const float BeltItemsPerSecond = 1.6f;
    public const float StorageDispatchSeconds = 0.3f;
    public const int StorageCapacity = 8;
    public const float InserterCycleSeconds = 0.55f;
    public const float CameraPitchDegrees = -55.0f;
    public const float CameraMinZoom = 15.0f;
    public const float CameraMaxZoom = 34.0f;
    public const float CameraDefaultZoom = 22.0f;
}
