using Godot;
using System.Text;

public static class HeavyCargoTrace
{
    private const string Prefix = "HEAVY_CARGO_TRACE";

    public static bool IsTracked(FactoryItem? item)
    {
        return item is not null
            && (item.CargoForm == FactoryCargoForm.WorldBulk || item.CargoForm == FactoryCargoForm.WorldPacked);
    }

    public static void Log(string stage, FactoryItem? item, FactoryStructure? structure = null, string? details = null)
    {
        if (!IsTracked(item))
        {
            return;
        }

        var builder = new StringBuilder();
        builder.Append(Prefix);
        builder.Append(" t=").Append(Time.GetTicksMsec());
        builder.Append(" id=").Append(item!.Id);
        builder.Append(" cargo=").Append(item.CargoForm);
        builder.Append(" kind=").Append(item.ItemKind);
        if (!string.IsNullOrWhiteSpace(item.BundleTemplateId))
        {
            builder.Append(" template=").Append(item.BundleTemplateId);
        }

        builder.Append(" stage=").Append(stage);

        if (structure is not null)
        {
            builder.Append(" structure=").Append(structure.Kind);
            builder.Append(" site=").Append(structure.Site.SiteId);
            builder.Append(" cell=(").Append(structure.Cell.X).Append(',').Append(structure.Cell.Y).Append(')');
            builder.Append(" sid=").Append(structure.GetInstanceId());
        }

        if (!string.IsNullOrWhiteSpace(details))
        {
            builder.Append(' ').Append(details);
        }

        GD.Print(builder.ToString());
    }
}
