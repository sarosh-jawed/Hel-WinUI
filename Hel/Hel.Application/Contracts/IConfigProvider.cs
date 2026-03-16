namespace Hel.Application.Contracts;

/// <summary>
/// Small abstraction over IConfiguration to keep UI clean and testable.
/// </summary>
public interface IConfigProvider
{
    string GetDefaultOutputFolder();
}
