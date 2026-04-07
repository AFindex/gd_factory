using System.Collections.Generic;

public static class FactoryMapSmokeSupport
{
    public static bool VerifyTargets(params string[] targetIds)
    {
        var report = targetIds.Length == 0
            ? FactoryMapValidationService.ValidateAllTargets()
            : ValidateNamedTargets(targetIds);
        if (report.HasErrors)
        {
            FactoryMapValidationService.PrintReport(report);
            return false;
        }

        return true;
    }

    private static FactoryMapValidationReport ValidateNamedTargets(IReadOnlyList<string> targetIds)
    {
        var targets = new List<FactoryMapValidationTarget>(targetIds.Count);
        for (var i = 0; i < targetIds.Count; i++)
        {
            if (!FactoryMapValidationCatalog.TryGetTarget(targetIds[i], out var target) || target is null)
            {
                throw new System.InvalidOperationException($"Unknown factory map validation target '{targetIds[i]}'.");
            }

            targets.Add(target);
        }

        return FactoryMapValidationService.ValidateTargets(targets);
    }
}
