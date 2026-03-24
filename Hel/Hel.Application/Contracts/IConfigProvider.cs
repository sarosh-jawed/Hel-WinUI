using Hel.Application.Configuration;

namespace Hel.Application.Contracts;

/// <summary>
/// Small abstraction over typed Hel configuration.
/// Keeps UI and orchestration code decoupled from raw IConfiguration.
/// </summary>
public interface IConfigProvider
{
    HelConfig GetConfig();
    string GetDefaultOutputFolder();
    string GetPrimaryLibraryScopeName();
    string GetLogFolder();
}
