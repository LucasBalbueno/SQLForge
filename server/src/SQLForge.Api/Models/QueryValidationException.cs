namespace SQLForge.Api.Models;

public sealed class QueryValidationException(string message) : Exception(message);