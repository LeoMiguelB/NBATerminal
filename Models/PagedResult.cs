namespace NBATerminal.Models;

public sealed record PagedResult<T>(
    IReadOnlyList<T> Items,
    int PageIndex,
    int PageSize,
    int TotalCount
)
{
    public int TotalPages => TotalCount == 0 ? 1 : (int)Math.Ceiling((double)TotalCount / PageSize);
}
