namespace NBATerminal.Models;

public sealed record GameSummary(
    string GameId,
    string Status,
    TeamInfo HomeTeam,
    TeamInfo AwayTeam
)
{
    public string Display => $"{AwayTeam.Abbreviation} @ {HomeTeam.Abbreviation} - {Status}";
}
