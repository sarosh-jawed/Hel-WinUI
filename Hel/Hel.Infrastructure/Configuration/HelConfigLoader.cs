using Hel.Application.Configuration;
using Microsoft.Extensions.Configuration;

namespace Hel.Infrastructure.Configuration;

/// <summary>
/// Loads and validates the full Hel configuration from IConfiguration.
/// This keeps startup logic centralized and testable.
/// </summary>
public static class HelConfigLoader
{
    public static HelConfig LoadAndValidate(IConfiguration configuration)
    {
        var config = new HelConfig();

        // Bind the entire configuration root into HelConfig.
        configuration.Bind(config);

        var errors = HelConfigValidator.Validate(config);
        if (errors.Count > 0)
        {
            string message =
                "Hel configuration is invalid:" + Environment.NewLine +
                string.Join(Environment.NewLine, errors.Select((e, i) => $"{i + 1}. {e}"));

            throw new InvalidOperationException(message);
        }

        return config;
    }
}
