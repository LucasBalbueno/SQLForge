using Microsoft.AspNetCore.Mvc;
using SQLForge.Api.Models;
using SQLForge.Api.Services;

namespace SQLForge.Api.Controllers;

[ApiController]
[Route("api/query")]
public sealed class QueryController(QueryService service) : ControllerBase
{
    // Endpoint para retornar os metadados das colunas disponíveis
    [HttpGet("metadata")]
    public ActionResult<IReadOnlyList<ColumnMetadata>> Metadata() => Ok(QueryBuilder.Columns);

    // Endpoint para gerar a consulta SQL a partir do request
    [HttpPost("generate")]
    public ActionResult<GeneratedQuery> Generate(QueryRequest request) => Ok(service.Generate(request));

    // Endpoint para executar a consulta SQL gerada
    [HttpPost("execute")]
    public async Task<ActionResult<QueryResult>> Execute(QueryRequest request, CancellationToken cancellationToken) =>
        Ok(await service.ExecuteAsync(request, cancellationToken));
}
