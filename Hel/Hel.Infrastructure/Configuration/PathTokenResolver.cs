using System;

namespace Hel.Infrastructure.Configuration;

public static class PathTokenResolver
{
    public static string ResolveLocalAppDataTokens(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return value;

        string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

        return value.Replace(
            "%LOCALAPPDATA%",
            localAppData,
            StringComparison.OrdinalIgnoreCase);
    }
}
