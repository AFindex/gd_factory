using Godot;
using System.Collections.Generic;

public partial class MobileFactoryMiningStakeStructure : FactoryStructure
{
    private static readonly Color StakeBaseColor = new("2563EB");
    private static readonly Color StakeAccentColor = new("60A5FA");
    private static readonly Color StakeCableColor = new("93C5FD");
    private static readonly Color StakeProgressColor = new("FACC15");
    private MobileFactoryMiningInputPortStructure? _owner;
    private FactoryResourceKind _resourceKind;
    private string _depositName = "未绑定矿区";
    private Vector3 _linkTargetWorld = Vector3.Zero;
    private bool _reportedDestroyed;
    private bool _isDeploying;
    private float _deploymentDurationSeconds;
    private float _deploymentElapsedSeconds;
    private MeshInstance3D? _stakeCable;
    private MeshInstance3D? _stakeMast;
    private MeshInstance3D? _stakeHead;
    private MeshInstance3D? _stakeTip;
    private MeshInstance3D? _stakeBeacon;
    private MeshInstance3D? _deployProgressBackground;
    private MeshInstance3D? _deployProgressFill;
    private StandardMaterial3D? _deployProgressFillMaterial;
    private Vector3 _cableStartLocal = Vector3.Zero;
    private Vector3 _cableDeltaLocal = Vector3.Zero;

    public override BuildPrototypeKind Kind => BuildPrototypeKind.MiningStake;
    public override string Description => "由采矿输入端口在矿区外侧部署的采矿子建筑，可被敌人摧毁。";
    public override float MaxHealth => 20.0f;
    public bool IsDeploymentComplete => !_isDeploying;
    public float DeploymentProgress => _isDeploying
        ? Mathf.Clamp(_deploymentDurationSeconds <= 0.001f ? 1.0f : _deploymentElapsedSeconds / _deploymentDurationSeconds, 0.0f, 1.0f)
        : 1.0f;

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

    public void BeginDeployment(float durationSeconds)
    {
        _isDeploying = true;
        _deploymentDurationSeconds = Mathf.Max(0.08f, durationSeconds);
        _deploymentElapsedSeconds = 0.0f;
        UpdateDeploymentVisualState(0.0f);
    }

    public void PrepareForRemoval()
    {
        _owner = null;
        _reportedDestroyed = true;
    }

    public override IEnumerable<string> GetInspectionLines()
    {
        foreach (var line in base.GetInspectionLines())
        {
            yield return line;
        }

        yield return $"矿区：{_depositName}";
        yield return $"矿种：{FactoryResourceCatalog.GetDisplayName(_resourceKind)}";
        if (_isDeploying)
        {
            yield return $"部署进度：{DeploymentProgress * 100.0f:0}%";
        }
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

    public override void SimulationStep(SimulationController simulation, double stepSeconds)
    {
        base.SimulationStep(simulation, stepSeconds);
        if (!_isDeploying || IsDestroyed)
        {
            return;
        }

        _deploymentElapsedSeconds = Mathf.Min(_deploymentDurationSeconds, _deploymentElapsedSeconds + (float)stepSeconds);
        if (_deploymentElapsedSeconds + 0.0001f < _deploymentDurationSeconds)
        {
            return;
        }

        _isDeploying = false;
        _deploymentElapsedSeconds = _deploymentDurationSeconds;
        _owner?.HandleDeployingStakeCompleted(this);
    }

    public override void UpdateVisuals(float tickAlpha)
    {
        base.UpdateVisuals(tickAlpha);
        UpdateDeploymentVisualState(DeploymentProgress);
    }

    protected override void BuildVisuals()
    {
        BuildLinkVisual();
        CreateDisc("StakePad", CellSize * 0.18f, 0.16f, StakeBaseColor.Darkened(0.18f), new Vector3(0.0f, 0.08f, 0.0f));
        _stakeMast = CreateDisc("StakeMast", 0.07f, 0.56f, StakeBaseColor, new Vector3(0.0f, 0.40f, 0.0f));
        _stakeHead = CreateBox("StakeHead", new Vector3(0.26f, 0.16f, 0.44f), StakeAccentColor, new Vector3(0.0f, 0.70f, 0.0f));
        _stakeTip = CreateBox("StakeTip", new Vector3(0.12f, 0.10f, 0.26f), StakeAccentColor.Lightened(0.18f), new Vector3(0.0f, 0.62f, 0.20f));
        _stakeBeacon = CreateBox("StakeBeacon", new Vector3(0.08f, 0.08f, 0.08f), Colors.White, new Vector3(0.0f, 0.80f, -0.12f));

        _deployProgressBackground = CreateBox(
            "DeployProgressBackground",
            new Vector3(CellSize * 0.54f, 0.03f, 0.08f),
            new Color(0.04f, 0.07f, 0.12f, 0.82f),
            new Vector3(0.0f, 1.00f, 0.0f));
        _deployProgressBackground.Visible = false;

        _deployProgressFillMaterial = new StandardMaterial3D
        {
            AlbedoColor = StakeProgressColor,
            Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
            ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
            EmissionEnabled = true,
            Emission = StakeProgressColor.Darkened(0.08f)
        };
        _deployProgressFill = new MeshInstance3D
        {
            Name = "DeployProgressFill",
            Mesh = new BoxMesh { Size = new Vector3(CellSize * 0.50f, 0.02f, 0.06f) },
            Position = new Vector3(0.0f, 1.00f, 0.0f),
            MaterialOverride = _deployProgressFillMaterial,
            Visible = false
        };
        AddChild(_deployProgressFill);

        UpdateDeploymentVisualState(DeploymentProgress);
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

        _cableStartLocal = start;
        _cableDeltaLocal = delta;
        _stakeCable = new MeshInstance3D
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
        AddChild(_stakeCable);
    }

    private void UpdateDeploymentVisualState(float progress)
    {
        progress = Mathf.Clamp(progress, 0.0f, 1.0f);

        if (_stakeCable is not null)
        {
            _stakeCable.Visible = progress > 0.001f;
            _stakeCable.Position = _cableStartLocal + (_cableDeltaLocal * (progress * 0.5f));
            _stakeCable.Scale = new Vector3(1.0f, 1.0f, Mathf.Max(0.001f, progress));
        }

        if (_stakeMast is not null)
        {
            _stakeMast.Visible = progress > 0.001f;
            _stakeMast.Position = new Vector3(0.0f, 0.12f + (0.28f * progress), 0.0f);
            _stakeMast.Scale = new Vector3(1.0f, Mathf.Max(0.001f, progress), 1.0f);
        }

        if (_stakeHead is not null)
        {
            _stakeHead.Visible = progress > 0.10f;
            _stakeHead.Position = new Vector3(0.0f, Mathf.Lerp(0.28f, 0.70f, progress), 0.0f);
            _stakeHead.Scale = Vector3.One * Mathf.Max(0.08f, progress);
        }

        if (_stakeTip is not null)
        {
            _stakeTip.Visible = progress > 0.18f;
            _stakeTip.Position = new Vector3(0.0f, Mathf.Lerp(0.24f, 0.62f, progress), Mathf.Lerp(0.04f, 0.20f, progress));
            _stakeTip.Scale = Vector3.One * Mathf.Max(0.08f, progress);
        }

        if (_stakeBeacon is not null)
        {
            _stakeBeacon.Visible = progress > 0.35f;
            _stakeBeacon.Position = new Vector3(0.0f, Mathf.Lerp(0.30f, 0.80f, progress), -0.12f);
            _stakeBeacon.Scale = Vector3.One * Mathf.Max(0.12f, 0.55f + (progress * 0.45f));
        }

        if (_deployProgressBackground is not null)
        {
            _deployProgressBackground.Visible = _isDeploying;
        }

        if (_deployProgressFill is not null)
        {
            _deployProgressFill.Visible = _isDeploying;
            _deployProgressFill.Scale = new Vector3(Mathf.Max(0.01f, progress), 1.0f, 1.0f);
            _deployProgressFill.Position = new Vector3((-0.25f * CellSize) + (progress * 0.25f * CellSize), 1.00f, 0.0f);
        }
    }
}
