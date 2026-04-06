public static class FactoryMapSmokeSupport
{
    public static bool VerifyDocuments(params string[] resourcePaths)
    {
        if (!FactoryMapRuntimeLoader.VerifyMalformedMapRejected())
        {
            return false;
        }

        for (var i = 0; i < resourcePaths.Length; i++)
        {
            if (!FactoryMapRuntimeLoader.VerifyRoundTrip(resourcePaths[i]))
            {
                return false;
            }
        }

        return true;
    }
}
