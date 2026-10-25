using Dapper;

namespace SQLForge.Api.Services;

// Representa uma query construída com SQL, SQL parametrizado, SQL de contagem e parâmetros dinâmicos
// Query executada por debaixo dos panos, para evitar SQL Injection
public sealed record BuiltQuery(
    // Query textual enviada para o usuário
    string Sql,
    // Query parametrizada para execução no banco de dados
    string ParameterizedSql,
    // Query de contagem para obter o total de registros
    string CountSql,
    // Parâmetros dinâmicos para a query parametrizada
    DynamicParameters Parameters);