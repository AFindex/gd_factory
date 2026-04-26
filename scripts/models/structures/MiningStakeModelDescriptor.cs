using Godot;
using NetFactory.Models;

public static class MiningStakeModelDescriptor
{
    public static void BuildModel(IModelBuilder builder, FactorySiteKind siteKind)
    {
        var cs = builder.CellSize;
        var stakeBaseColor = new Color("2563EB");
        var stakeAccentColor = new Color("60A5FA");
        var stakeCableColor = new Color("93C5FD");
        var stakeProgressColor = new Color("FACC15");

        builder.AddDisc("StakePad", cs * 0.18f, 0.16f, stakeBaseColor.Darkened(0.18f), new Vector3(0.0f, 0.08f, 0.0f));
        builder.AddDisc("StakeMast", 0.07f, 0.56f, stakeBaseColor, new Vector3(0.0f, 0.40f, 0.0f));
        builder.AddBox("StakeHead", new Vector3(0.26f, 0.16f, 0.44f), stakeAccentColor, new Vector3(0.0f, 0.70f, 0.0f));
        builder.AddBox("StakeTip", new Vector3(0.12f, 0.10f, 0.26f), stakeAccentColor.Lightened(0.18f), new Vector3(0.0f, 0.62f, 0.20f));
        builder.AddBox("StakeBeacon", new Vector3(0.08f, 0.08f, 0.08f), Colors.White, new Vector3(0.0f, 0.80f, -0.12f));

        var deployBg = builder.AddBox(
            "DeployProgressBackground",
            new Vector3(cs * 0.54f, 0.03f, 0.08f),
            new Color(0.04f, 0.07f, 0.12f, 0.82f),
            new Vector3(0.0f, 1.00f, 0.0f));

        var deployFill = builder.AddBox(
            "DeployProgressFill",
            new Vector3(cs * 0.50f, 0.02f, 0.06f),
            stakeProgressColor,
            new Vector3(0.0f, 1.00f, 0.0f));
        if (deployFill.MaterialOverride is StandardMaterial3D fillMat)
        {
            fillMat.Transparency = BaseMaterial3D.TransparencyEnum.Alpha;
            fillMat.ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded;
            fillMat.EmissionEnabled = true;
            fillMat.Emission = stakeProgressColor.Darkened(0.08f);
        }
    }
}
