namespace NBATerminal.Models;

public sealed record PlayerStatLine(
    string PlayerName,
    string TeamAbbreviation,
    int Points,
    int Rebounds,
    int Assists,
    string Minutes
)
{
    public string ToDisplayString()
    {
        return $"{PlayerName,-22} PTS:{Points,2} REB:{Rebounds,2} AST:{Assists,2} MIN:{Minutes,5}";
    }
}
