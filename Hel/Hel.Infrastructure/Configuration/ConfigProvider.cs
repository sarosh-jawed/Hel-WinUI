using Hel.Application.Configuration;
using Hel.Application.Contracts;

namespace Hel.Infrastructure.Configuration;

public sealed class ConfigProvider : IConfigProvider
{
    private readonly HelConfig _config;

    public ConfigProvider(HelConfig config)
    {
        _config = config;
    }

    public HelConfig GetConfig() => _config;

    public string GetDefaultOutputFolder()
    {
        string outputRoot = PathTokenResolver.ResolveKnownTokens(_config.Output.Root);
        string monthFolder = DateTime.Now.ToString(_config.Output.MonthFolderFormat);

        string fullPath = Path.Combine(outputRoot, monthFolder);
        Directory.CreateDirectory(fullPath);

        return fullPath;
    }

    public string GetPrimaryLibraryScopeName()
    {
        var firstRule = _config.LibraryRules.FirstOrDefault();

        if (firstRule is null || string.IsNullOrWhiteSpace(firstRule.LibraryName))
        {
            throw new InvalidOperationException(
                "LibraryRules must contain at least one valid LibraryName for filtering.");
        }

        return firstRule.LibraryName.Trim();
    }

    public string GetLogFolder()
    {
        string logsRoot = PathTokenResolver.ResolveKnownTokens(_config.Output.LogsRoot);
        Directory.CreateDirectory(logsRoot);
        return logsRoot;
    }
}
