namespace Hel.Infrastructure.Configuration;

public static class PathTokenResolver
{
    public static string ResolveKnownTokens(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return value;

        string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        string userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        string documents = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

        return value
            .Replace("%LOCALAPPDATA%", localAppData, StringComparison.OrdinalIgnoreCase)
            .Replace("%USERPROFILE%", userProfile, StringComparison.OrdinalIgnoreCase)
            .Replace("%DOCUMENTS%", documents, StringComparison.OrdinalIgnoreCase);
    }
}
