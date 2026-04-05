using Godot;
using System.Collections.Generic;

public partial class MobileFactoryMiningStakeStructure : FactoryStructure
{
    private static readonly Color StakeBaseColor = new("2563EB");
    private static readonly Color StakeAccentColor = new("60A5FA");
    private static readonly Color StakeCableColor = new("93C5FD");
    private MobileFactoryMiningInputPortStructure? _owner;
    private FactoryResourceKind _resourceKind;
    private string _depositName = "未绑定矿区";
    private Vector3 _linkTargetWorld = Vector3.Zero;
    private bool _reportedDestroyed;

    public override BuildPrototypeKind Kind => BuildPrototypeKind.MiningStake;
    public override string Description => "由采矿输入端口在矿区外侧部署的采矿子建筑，可被敌人摧毁。";
    public override float MaxHealth => 20.0f;

    public void ConfigureStake(
        MobileFactoryMiningInputPortStructure owner,
        GridManager worldSite,
        Vector2I worldCell,
        FacingDirection facing,
        FactoryResourceDepositDefinition deposit,
        Vector3 linkTargetWorld,
        string reservationOwnerId)
    {
        _owner = owner;
        _resourceKind = deposit.ResourceKind;
        _depositName = deposit.DisplayName;
        _linkTargetWorld = linkTargetWorld;
        Configure(worldSite, worldCell, facing, reservationOwnerId);
    }

    public override IEnumerable<string> GetInspectionLines()
    {
        foreach (var line in base.GetInspectionLines())
        {
            yield return line;
        }

        yield return $"矿区：{_depositName}";
        yield return $"矿种：{FactoryResourceCatalog.GetDisplayName(_resourceKind)}";
    }

    public override void ApplyDamage(float damage, SimulationController simulation)
    {
        var wasDestroyed = IsDestroyed;
        base.ApplyDamage(damage, simulation);
        if (!wasDestroyed && IsDestroyed && !_reportedDestroyed)
        {
            _reportedDestroyed = true;
            _owner?.HandleDeployedStakeDestroyed(this);
        }
    }

    protected override void BuildVisuals()
    {
        BuildLinkVisual();
        CreateDisc("StakePad", CellSize * 0.18f, 0.16f, StakeBaseColor.Darkened(0.18f), new Vector3(0.0f, 0.08f, 0.0f));
        CreateDisc("StakeMast", 0.07f, 0.56f, StakeBaseColor, new Vector3(0.0f, 0.40f, 0.0f));
        CreateBox("StakeHead", new Vector3(0.26f, 0.16f, 0.44f), StakeAccentColor, new Vector3(0.0f, 0.70f, 0.0f));
        CreateBox("StakeTip", new Vector3(0.12f, 0.10f, 0.26f), StakeAccentColor.Lightened(0.18f), new Vector3(0.0f, 0.62f, 0.20f));
        CreateBox("StakeBeacon", new Vector3(0.08f, 0.08f, 0.08f), Colors.White, new Vector3(0.0f, 0.80f, -0.12f));
    }

    private void BuildLinkVisual()
    {
        var start = new Vector3(0.0f, 0.18f, 0.0f);
        var localTarget = ToLocal(_linkTargetWorld);
        localTarget.Y = start.Y;
        var delta = localTarget - start;
        var length = new Vector2(delta.X, delta.Z).Length();
        if (length <= 0.05f)
        {
            return;
        }

        var link = new MeshInstance3D
        {
            Name = "StakeCable",
            Mesh = new BoxMesh { Size = new Vector3(0.08f, 0.04f, length) },
            Position = start + (delta * 0.5f),
            Rotation = new Vector3(0.0f, Mathf.Atan2(delta.X, delta.Z), 0.0f),
            CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
            MaterialOverride = new StandardMaterial3D
            {
                AlbedoColor = StakeCableColor,
                Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
                Roughness = 0.18f,
                ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
                EmissionEnabled = true,
                Emission = StakeCableColor.Darkened(0.10f)
            }
        };
        AddChild(link);
    }
}
