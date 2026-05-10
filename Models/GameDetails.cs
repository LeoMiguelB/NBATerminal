namespace NBATerminal.Models;

public sealed record GameDetails(
    GameSummary Game,
    IReadOnlyList<PlayerStatLine> AwayPlayers,
    IReadOnlyList<PlayerStatLine> HomePlayers
);
