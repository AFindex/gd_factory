using Godot;
using System.Collections.Generic;

public sealed partial class WorldBruteEnemy : FactoryEnemyActor
{
    protected override Color BodyColor => new Color("334155");
    protected override Vector3 BodySize => new Vector3(1.18f, 1.28f, 1.18f);

    public override string EnemyTypeId => "world-brute";
    public override string DisplayName => "大世界重装兽";
    public override float MaxHealth => 560.0f;
    public override float MoveSpeed => 1.28f;
    public override float AggroRange => 6.4f;
    public override float AttackRange => 1.55f;
    public override float AttackDamage => 18.0f;
    public override float AttackCooldownSeconds => 0.88f;
    public override IReadOnlyCollection<BuildPrototypeKind>? PreferredTargetKinds => new[] { BuildPrototypeKind.Wall, BuildPrototypeKind.GunTurret, BuildPrototypeKind.Sink };

    protected override Mesh CreateBodyMesh()
    {
        return new CylinderMesh
        {
            TopRadius = 0.54f,
            BottomRadius = 0.66f,
            Height = BodySize.Y
        };
    }
}

public sealed partial class WorldSiegeEnemy : FactoryEnemyActor
{
    protected override Color BodyColor => new Color("3B0764");
    protected override Vector3 BodySize => new Vector3(1.46f, 1.04f, 1.62f);
    protected override bool UsesAttackTracerVisual => true;
    protected override Color AttackTracerColor => new Color("0F766E");
    protected override Color AttackTracerEmission => new Color("5EEAD4");
    protected override float AttackTracerWidth => 0.12f;
    protected override float PursuitStopDistance => AttackRange * 0.88f;
    protected override Vector3 AttackOriginOffset => new Vector3(0.0f, 0.92f, 0.0f);
    protected override Vector3 AttackImpactOffset => new Vector3(0.0f, 0.86f, 0.0f);

    public override string EnemyTypeId => "world-siege";
    public override string DisplayName => "大世界攻城体";
    public override float MaxHealth => 840.0f;
    public override float MoveSpeed => 0.94f;
    public override float AggroRange => 7.8f;
    public override float AttackRange => 7.1f;
    public override float AttackDamage => 22.0f;
    public override float AttackCooldownSeconds => 1.48f;
    public override IReadOnlyCollection<BuildPrototypeKind>? PreferredTargetKinds => new[] { BuildPrototypeKind.GunTurret, BuildPrototypeKind.AmmoAssembler, BuildPrototypeKind.Sink, BuildPrototypeKind.Storage };

    protected override Mesh CreateBodyMesh()
    {
        return new CapsuleMesh
        {
            Radius = 0.52f,
            Height = 1.86f
        };
    }
}
