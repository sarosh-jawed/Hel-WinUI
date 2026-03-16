using Hel.Application.Contracts;
using Microsoft.Extensions.Configuration;
using System;
using System.IO;

namespace Hel.Infrastructure.Configuration;

public sealed class ConfigProvider : IConfigProvider
{
    private readonly IConfiguration _config;

    public ConfigProvider(IConfiguration config)
    {
        _config = config;
    }

    public string GetDefaultOutputFolder()
    {
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

        var outputRoot = _config["App:OutputRoot"]
            ?.Replace("%LOCALAPPDATA%", localAppData, StringComparison.OrdinalIgnoreCase)
            ?? Path.Combine(localAppData, "Hel", "Output");

        var monthFolder = DateTime.Now.ToString("yyyy-MM");
        var full = Path.Combine(outputRoot, monthFolder);

        Directory.CreateDirectory(full);
        return full;
    }
}
