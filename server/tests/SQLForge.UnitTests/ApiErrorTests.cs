using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using SQLForge.Api.Controllers;
using SQLForge.Api.Errors;
using SQLForge.Api.Models;
using SQLForge.Api.Repositories;
using SQLForge.Api.Services;

namespace SQLForge.UnitTests;

public sealed class ApiErrorTests
{
    // Verifica que a API retorna 500 em JSON com mensagem genérica, sem expor SQL, credenciais ou detalhes da exceção.
    [Fact]
    public async Task Unexpected_errors_return_generic_500()
    {
        using var response = await PostAsync("execute", """{"columns":["id"]}""");

        await AssertProblem(response, HttpStatusCode.InternalServerError,
            "Erro interno do servidor.",
            "Não foi possível processar a requisição. Tente novamente mais tarde.");
    }

    // Verifica que a API retorna 400 com uma mensagem útil ao cliente, mantendo o formato seguro de erro.
    [Fact]
    public async Task Domain_validation_returns_safe_400()
    {
        using var response = await PostAsync("generate", """{"columns":["unknown"]}""");

        await AssertProblem(response, HttpStatusCode.BadRequest,
            "Consulta inválida.", "Coluna inválida.");
    }

    // Verifica erros de desserialização e validação dos campos, sem expor detalhes internos no 400.
    [Theory]
    [InlineData("""{"columns":["id"],"page":"secret"}""")]
    [InlineData("""{"columns":[]}""")]
    public async Task Invalid_input_returns_generic_400(string body)
    {
        using var response = await PostAsync("generate", body);

        await AssertProblem(response, HttpStatusCode.BadRequest,
            "Requisição inválida.", "Verifique o formato e os valores dos campos enviados.");
    }

    // Centraliza a verificação do status, do tipo de conteúdo e do corpo da resposta de erro.
    private static async Task AssertProblem(HttpResponseMessage response, HttpStatusCode status,
        string title, string detail)
    {
        Assert.Equal(status, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var problem = document.RootElement;

        Assert.Equal(new[] { "detail", "status", "title", "traceId" },
            problem.EnumerateObject().Select(property => property.Name).OrderBy(name => name));
        Assert.Equal((int)status, problem.GetProperty("status").GetInt32());
        Assert.Equal(title, problem.GetProperty("title").GetString());
        Assert.Equal(detail, problem.GetProperty("detail").GetString());
        Assert.False(string.IsNullOrWhiteSpace(problem.GetProperty("traceId").GetString()));
    }

    // Inicia a API em memória e envia um POST, sem depender de um banco de dados real.
    private static async Task<HttpResponseMessage> PostAsync(string endpoint, string body)
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = "Development" });
        builder.WebHost.UseTestServer();
        builder.Services.AddControllers().AddApplicationPart(typeof(QueryController).Assembly).AddSafeApiErrors();
        builder.Services.AddScoped<QueryBuilder>();
        builder.Services.AddScoped<QueryService>();
        builder.Services.AddSingleton<IProductQueryRepository, FailingRepository>();
        await using var app = builder.Build();
        app.UseExceptionHandler();
        app.MapControllers();
        await app.StartAsync();

        using var client = app.GetTestClient();
        // Mesmo solicitando HTML em desenvolvimento, a resposta deve ser um JSON seguro.
        client.DefaultRequestHeaders.Accept.ParseAdd("text/html");
        using var content = new StringContent(body, Encoding.UTF8, "application/json");
        return await client.PostAsync($"/api/query/{endpoint}", content);
    }

    // Repository de teste que simula uma exceção
    private sealed class FailingRepository : IProductQueryRepository
    {
        // Simula uma falha com dados sensíveis fictícios para verificar que eles não chegam ao cliente.
        public Task<QueryResult> ExecuteAsync(QueryRequest request, BuiltQuery query, CancellationToken cancellationToken)
            => throw new InvalidOperationException("SELECT * FROM secret; Password=secret", new Exception("internal secret"));
    }
}
