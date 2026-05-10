using System.Diagnostics;
using System.Text;
using System.Text.Json;

namespace NBATerminal.Services;

public sealed class PythonBridge : IPythonBridge
{
    private readonly string _pythonExecutable;
    private readonly string _scriptPath;

    public PythonBridge(string? pythonExecutable = null)
    {
        _pythonExecutable = ResolvePythonExecutable(pythonExecutable);
        _scriptPath = Path.Combine(AppContext.BaseDirectory, "Services", "python", "nba_adapter.py");
    }

    public async Task<JsonDocument> ExecuteAsync(
        string command,
        IReadOnlyDictionary<string, string>? arguments = null,
        CancellationToken cancellationToken = default
    )
    {
        if (!File.Exists(_scriptPath))
        {
            throw new InvalidOperationException($"Python adapter script not found at '{_scriptPath}'.");
        }

        var cliArguments = BuildCliArguments(command, arguments);
        var startInfo = new ProcessStartInfo
        {
            FileName = _pythonExecutable,
            Arguments = $"\"{_scriptPath}\" {cliArguments}",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = new Process { StartInfo = startInfo };
        process.Start();

        var outputTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
        var errorTask = process.StandardError.ReadToEndAsync(cancellationToken);

        await process.WaitForExitAsync(cancellationToken);

        var standardOutput = await outputTask;
        var standardError = await errorTask;

        if (process.ExitCode != 0)
        {
            var message = string.IsNullOrWhiteSpace(standardError) ? "Python bridge failed." : standardError.Trim();
            throw new InvalidOperationException(message);
        }

        if (string.IsNullOrWhiteSpace(standardOutput))
        {
            throw new InvalidOperationException("Python bridge returned no data.");
        }

        return JsonDocument.Parse(standardOutput);
    }

    private static string BuildCliArguments(string command, IReadOnlyDictionary<string, string>? arguments)
    {
        var builder = new StringBuilder(command);

        if (arguments is null)
        {
            return builder.ToString();
        }

        foreach (var pair in arguments)
        {
            builder.Append(' ');
            builder.Append("--");
            builder.Append(pair.Key);
            builder.Append(' ');
            builder.Append('"');
            builder.Append(pair.Value.Replace("\"", "\\\""));
            builder.Append('"');
        }

        return builder.ToString();
    }

    private static string ResolvePythonExecutable(string? configuredExecutable)
    {
        if (!string.IsNullOrWhiteSpace(configuredExecutable))
        {
            return configuredExecutable;
        }

        var localVenvPython = Path.Combine(AppContext.BaseDirectory, ".venv", "bin", "python");
        if (File.Exists(localVenvPython))
        {
            return localVenvPython;
        }

        var workspaceVenvPython = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".venv", "bin", "python"));
        if (File.Exists(workspaceVenvPython))
        {
            return workspaceVenvPython;
        }

        return "python3";
    }
}
