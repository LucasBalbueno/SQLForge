namespace SQLForge.Api.Models;

public sealed record QueryFilter(string Column, string Operator, string Value);