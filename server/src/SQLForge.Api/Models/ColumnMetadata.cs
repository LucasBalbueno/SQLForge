namespace SQLForge.Api.Models;

public sealed record ColumnMetadata(string Name, string Label, string Type, string[] Operators);