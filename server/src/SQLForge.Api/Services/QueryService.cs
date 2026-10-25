using SQLForge.Api.Models;
using SQLForge.Api.Repositories;

namespace SQLForge.Api.Services;

// Coordena o fluxo entre o QueryBuilder e o repositório (banco)
// Recebe o serviço lógico e o repositório de dados
public sealed class QueryService(QueryBuilder builder, IProductQueryRepository repository)
{
    // Chama Build para gerar a consulta SQL (textual e parametrizada) e retorna apenas a consulta SQL textual
    public GeneratedQuery Generate(QueryRequest request) => new(builder.Build(request).Sql);

    // Chama Build para gerar a consulta SQL (textual e parametrizada) e retorna o resultado da execução da consulta SQL parametrizada
    public Task<QueryResult> ExecuteAsync(QueryRequest request, CancellationToken cancellationToken) =>
        repository.ExecuteAsync(request, builder.Build(request), cancellationToken);
}
