using System.Data;
using Dapper;
using Npgsql;
using SQLForge.Api.Models;
using SQLForge.Api.Services;

namespace SQLForge.Api.Repositories;

// Responsável por executar a query no banco de dados
public sealed class ProductQueryRepository(NpgsqlDataSource dataSource) : IProductQueryRepository
{
    // Recebe a requisição de consulta, a query construída (textual e parâmetrizada) e o token de cancelamento
    public async Task<QueryResult> ExecuteAsync(QueryRequest request, BuiltQuery query, CancellationToken cancellationToken)
    {
        // Abre uma conexão com o banco
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);

        // Inicia uma unica transação no PostgreSQL com contagem de leitura para garantir consistência dos dados
        await using var transaction = await connection.BeginTransactionAsync(IsolationLevel.RepeatableRead, cancellationToken);

        // Executa a query de contagem para obter o total de registros
        var total = await connection.ExecuteScalarAsync<long>(
            new CommandDefinition(
                query.CountSql,
                query.Parameters,
                transaction,
                commandTimeout: 15,
                cancellationToken: cancellationToken));

        // Executa a query de seleção para obter os registros na página solicitada
        var records = await connection.QueryAsync(
            new CommandDefinition(
                query.ParameterizedSql,
                query.Parameters,
                transaction,
                commandTimeout: 15,
                cancellationToken: cancellationToken));

        // Dapper retorna um IEnumerable<dynamic>, então precisamos converter para uma lista de dicionários para facilitar o acesso aos dados
        var rows = records
            .Select(row =>
                (IDictionary<string, object?>)
                new Dictionary<string, object?>(
                    (IDictionary<string, object?>)row))
            .ToList();

        // Confirma as transações no banco
        await transaction.CommitAsync(cancellationToken);

        // Retorna o resultado da consulta, incluindo os registros, a página atual, o tamanho da página e o total de páginas
        return new QueryResult(
            request.Columns,
            rows,
            request.Page,
            request.PageSize,
            total,
            total / request.PageSize + (total % request.PageSize == 0 ? 0 : 1));
    }
}
