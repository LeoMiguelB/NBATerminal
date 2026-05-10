using System.Text.Json;
using NBATerminal.Models;

namespace NBATerminal.Services;

public sealed class GameService
{
    private readonly IPythonBridge _pythonBridge;
    private readonly TimeSpan _cacheTtl;
    private IReadOnlyList<GameSummary> _cachedGames = Array.Empty<GameSummary>();
    private DateTimeOffset _lastFetchUtc = DateTimeOffset.MinValue;

    public GameService(IPythonBridge pythonBridge, TimeSpan? cacheTtl = null)
    {
        _pythonBridge = pythonBridge;
        _cacheTtl = cacheTtl ?? TimeSpan.FromSeconds(10);
    }

    public async Task<IReadOnlyList<GameSummary>> GetGamesTodayAsync(
        bool forceRefresh,
        CancellationToken cancellationToken = default
    )
    {
        if (!forceRefresh && DateTimeOffset.UtcNow - _lastFetchUtc < _cacheTtl && _cachedGames.Count > 0)
        {
            return _cachedGames;
        }

        using var payload = await _pythonBridge.ExecuteAsync("games-today", cancellationToken: cancellationToken);
        var games = ParseGames(payload.RootElement);

        _cachedGames = games;
        _lastFetchUtc = DateTimeOffset.UtcNow;

        return _cachedGames;
    }

    private static IReadOnlyList<GameSummary> ParseGames(JsonElement root)
    {
        var list = new List<GameSummary>();

        if (!root.TryGetProperty("games", out var gamesElement) || gamesElement.ValueKind != JsonValueKind.Array)
        {
            return list;
        }

        foreach (var gameElement in gamesElement.EnumerateArray())
        {
            var gameId = gameElement.GetProperty("gameId").GetString() ?? string.Empty;
            var status = gameElement.GetProperty("status").GetString() ?? "Unknown";
            var homeElement = gameElement.GetProperty("homeTeam");
            var awayElement = gameElement.GetProperty("awayTeam");

            var home = new TeamInfo(
                homeElement.GetProperty("teamId").GetString() ?? string.Empty,
                homeElement.GetProperty("name").GetString() ?? "Home",
                homeElement.GetProperty("abbreviation").GetString() ?? "HOME"
            );

            var away = new TeamInfo(
                awayElement.GetProperty("teamId").GetString() ?? string.Empty,
                awayElement.GetProperty("name").GetString() ?? "Away",
                awayElement.GetProperty("abbreviation").GetString() ?? "AWAY"
            );

            list.Add(new GameSummary(gameId, status, home, away));
        }

        return list;
    }
}
