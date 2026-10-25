namespace SQLForge.Api.Models;

public sealed record QueryResult(
    IReadOnlyList<string> Columns, 
    IReadOnlyList<IDictionary<string, object?>> Rows,
    int Page,
    int PageSize,
    long TotalCount,
    long TotalPages
);