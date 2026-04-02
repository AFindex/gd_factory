using Godot;

public static class FactoryConstants
{
    public const float CellSize = 2.0f;
    public const float HalfCell = CellSize * 0.5f;
    public const int GridMin = -16;
    public const int GridMax = 16;
    public const float SimulationStepSeconds = 0.05f;
    public const float ProducerSpawnSeconds = 0.8f;
    public const float BeltItemsPerSecond = 1.6f;
    public const float StorageDispatchSeconds = 0.3f;
    public const int StorageCapacity = 8;
    public const float InserterCycleSeconds = 0.55f;
    public const float StructureDamageFlashSeconds = 0.9f;
    public const float StructureHealthBarHeight = 1.86f;
    public const float MobileInteriorCombatOverlayScale = 0.62f;
    public const float NormalCombatOverlayScale = 1.0f;
    public const float GunTurretRange = 9.5f;
    public const float GunTurretCooldownSeconds = 0.42f;
    public const int GunTurretAmmoCapacity = 10;
    public const float GunTurretDamage = 18.0f;
    public const float GunTurretReturnSpeed = 6.2f;
    public const float GunTurretTrackingSpeed = 12.5f;
    public const float GunTurretAimToleranceRadians = 0.22f;
    public const float GunTurretTracerLifetime = 0.12f;
    public const float GunTurretMuzzleFlashLifetime = 0.08f;
    public const float AmmoAssemblerSpawnSeconds = 0.85f;
    public const float EnemyMeleeSpeed = 2.6f;
    public const float EnemyRangedSpeed = 2.1f;
    public const float EnemyMeleeAttackRange = 1.35f;
    public const float EnemyRangedAttackRange = 5.9f;
    public const float EnemyAggroRange = 5.2f;
    public const float EnemyPursuitLeashMultiplier = 1.65f;
    public const float EnemyAttackTracerLifetime = 0.18f;
    public const float CameraPitchDegrees = -55.0f;
    public const float CameraMinZoom = 15.0f;
    public const float CameraMaxZoom = 34.0f;
    public const float CameraDefaultZoom = 22.0f;
}
