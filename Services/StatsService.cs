using System.Text.Json;
using NBATerminal.Models;

namespace NBATerminal.Services;

public sealed class StatsService
{
    private readonly IPythonBridge _pythonBridge;
    private readonly TimeSpan _cacheTtl;
    private readonly Dictionary<string, (GameDetails Details, DateTimeOffset LastFetchUtc)> _cache = new();

    public StatsService(IPythonBridge pythonBridge, TimeSpan? cacheTtl = null)
    {
        _pythonBridge = pythonBridge;
        _cacheTtl = cacheTtl ?? TimeSpan.FromSeconds(10);
    }

    public async Task<GameDetails> GetGameDetailsAsync(
        GameSummary game,
        bool forceRefresh,
        CancellationToken cancellationToken = default
    )
    {
        if (!forceRefresh
            && _cache.TryGetValue(game.GameId, out var cached)
            && DateTimeOffset.UtcNow - cached.LastFetchUtc < _cacheTtl)
        {
            return cached.Details;
        }

        using var payload = await _pythonBridge.ExecuteAsync(
            "game-stats",
            new Dictionary<string, string> { ["game-id"] = game.GameId },
            cancellationToken
        );

        var details = ParseDetails(payload.RootElement, game);
        _cache[game.GameId] = (details, DateTimeOffset.UtcNow);
        return details;
    }

    private static GameDetails ParseDetails(JsonElement root, GameSummary fallbackGame)
    {
        var gameElement = root.TryGetProperty("game", out var parsedGame) ? parsedGame : default;
        var game = gameElement.ValueKind == JsonValueKind.Object ? ParseGameSummary(gameElement, fallbackGame) : fallbackGame;

        var awayPlayers = new List<PlayerStatLine>();
        var homePlayers = new List<PlayerStatLine>();

        if (root.TryGetProperty("awayPlayers", out var awayElement) && awayElement.ValueKind == JsonValueKind.Array)
        {
            awayPlayers.AddRange(ParsePlayers(awayElement, game.AwayTeam.Abbreviation));
        }

        if (root.TryGetProperty("homePlayers", out var homeElement) && homeElement.ValueKind == JsonValueKind.Array)
        {
            homePlayers.AddRange(ParsePlayers(homeElement, game.HomeTeam.Abbreviation));
        }

        return new GameDetails(game, awayPlayers, homePlayers);
    }

    private static GameSummary ParseGameSummary(JsonElement gameElement, GameSummary fallbackGame)
    {
        var gameId = gameElement.GetProperty("gameId").GetString() ?? fallbackGame.GameId;
        var status = gameElement.GetProperty("status").GetString() ?? fallbackGame.Status;

        var homeElement = gameElement.GetProperty("homeTeam");
        var awayElement = gameElement.GetProperty("awayTeam");

        var homeTeam = new TeamInfo(
            homeElement.GetProperty("teamId").GetString() ?? fallbackGame.HomeTeam.TeamId,
            homeElement.GetProperty("name").GetString() ?? fallbackGame.HomeTeam.Name,
            homeElement.GetProperty("abbreviation").GetString() ?? fallbackGame.HomeTeam.Abbreviation
        );

        var awayTeam = new TeamInfo(
            awayElement.GetProperty("teamId").GetString() ?? fallbackGame.AwayTeam.TeamId,
            awayElement.GetProperty("name").GetString() ?? fallbackGame.AwayTeam.Name,
            awayElement.GetProperty("abbreviation").GetString() ?? fallbackGame.AwayTeam.Abbreviation
        );

        return new GameSummary(gameId, status, homeTeam, awayTeam);
    }

    private static IEnumerable<PlayerStatLine> ParsePlayers(JsonElement playersElement, string teamAbbreviation)
    {
        foreach (var player in playersElement.EnumerateArray())
        {
            yield return new PlayerStatLine(
                player.GetProperty("playerName").GetString() ?? "Unknown",
                teamAbbreviation,
                player.GetProperty("points").GetInt32(),
                player.GetProperty("rebounds").GetInt32(),
                player.GetProperty("assists").GetInt32(),
                player.GetProperty("minutes").GetString() ?? "0"
            );
        }
    }
}
