using System.Text.Json;

namespace NBATerminal.Services;

public interface IPythonBridge
{
    Task<JsonDocument> ExecuteAsync(
        string command,
        IReadOnlyDictionary<string, string>? arguments = null,
        CancellationToken cancellationToken = default
    );
}
