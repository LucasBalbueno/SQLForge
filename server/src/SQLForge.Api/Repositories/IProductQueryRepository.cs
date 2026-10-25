using SQLForge.Api.Models;
using SQLForge.Api.Services;

namespace SQLForge.Api.Repositories;

// Define um contrato para o repositório de consultas de produtos
public interface IProductQueryRepository
{
    Task<QueryResult> ExecuteAsync(QueryRequest request, BuiltQuery query, CancellationToken cancellationToken);
}