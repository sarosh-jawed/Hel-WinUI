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
        string outputRoot = PathTokenResolver.ResolveLocalAppDataTokens(_config.Output.Root);
        string monthFolder = DateTime.Now.ToString(_config.Output.MonthFolderFormat);

        string fullPath = Path.Combine(outputRoot, monthFolder);
        Directory.CreateDirectory(fullPath);

        return fullPath;
    }
}
