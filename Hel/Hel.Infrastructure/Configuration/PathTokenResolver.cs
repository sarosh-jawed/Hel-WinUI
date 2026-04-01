using System;
using System.IO;

namespace Hel.Infrastructure.Configuration;

public static class PathTokenResolver
{
    public static string ResolveKnownTokens(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return value;

        // Prefer environment variables first so packaged apps resolve to the real user profile folders.
        // In packaged contexts, Environment.GetFolderPath(LocalApplicationData) can point to a container path.
        string localAppData =
            Environment.GetEnvironmentVariable("LOCALAPPDATA")
            ?? Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

        string userProfile =
            Environment.GetEnvironmentVariable("USERPROFILE")
            ?? Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

        string documents =
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

        // Extra safety: if MyDocuments is empty for any reason, fallback to USERPROFILE\Documents.
        if (string.IsNullOrWhiteSpace(documents) && !string.IsNullOrWhiteSpace(userProfile))
            documents = Path.Combine(userProfile, "Documents");

        return value
            .Replace("%LOCALAPPDATA%", localAppData, StringComparison.OrdinalIgnoreCase)
            .Replace("%USERPROFILE%", userProfile, StringComparison.OrdinalIgnoreCase)
            .Replace("%DOCUMENTS%", documents, StringComparison.OrdinalIgnoreCase);
    }
}
