using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using SQLForge.Api.Models;

namespace SQLForce.TestsIntegration;

public sealed class QueryApiTests(ApiFixture fixture) : IClassFixture<ApiFixture>
{
    // Verifica se o endpoint de saúde confirma que a API está disponível.
    [Fact]
    public async Task Health_returns_ok()
    {
        using var response = await fixture.Client.GetAsync("/health");
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("ok", body.GetProperty("status").GetString());
    }

    // Verifica se os metadados descrevem todas as colunas, tipos e operadores disponíveis.
    [Fact]
    public async Task Metadata_describes_all_available_columns()
    {
        var columns = await fixture.Client.GetFromJsonAsync<ColumnMetadata[]>("/api/query/metadata");
        Assert.NotNull(columns);
        Assert.Equal(new[] { "id", "name", "category", "price", "stock_quantity" }, columns.Select(c => c.Name));
        Assert.Equal("decimal", columns.Single(c => c.Name == "price").Type);
        Assert.Contains("gte", columns.Single(c => c.Name == "price").Operators);
        Assert.Contains("contains", columns.Single(c => c.Name == "name").Operators);
    }

    // Verifica se a geração de SQL aplica filtros, ordenação e paginação corretamente.
    [Fact]
    public async Task Generate_returns_sql_with_filters_order_and_pagination()
    {
        using var response = await fixture.Client.PostAsJsonAsync("/api/query/generate", new QueryRequest
        {
            Columns = ["id", "name"],
            Filters = [new("category", "eq", "Notebooks")],
            OrderBy = [new("name", "DESC")],
            Page = 2,
            PageSize = 5
        });
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<GeneratedQuery>();
        Assert.Equal("SELECT id, name\nFROM products\nWHERE category = E'Notebooks'\nORDER BY name DESC, id ASC\nLIMIT 5 OFFSET 5;", result?.Sql);
    }

    // Verifica se a execução retorna os produtos da seed e somente as colunas solicitadas.
    [Fact]
    public async Task Execute_returns_seeded_products_and_only_selected_columns()
    {
        var result = await Execute(new() { Columns = ["id", "name", "price"] });
        Assert.Equal(new[] { "id", "name", "price" }, result.Columns);
        Assert.Equal(60, result.TotalCount);
        Assert.Equal(6, result.TotalPages);
        Assert.Equal(1, result.Page);
        Assert.Equal(10, result.PageSize);
        Assert.Equal(Enumerable.Range(1, 10), result.Rows.Select(row => Number(row, "id")));
        Assert.All(result.Rows, row => Assert.Equal(new[] { "id", "name", "price" }, row.Keys));
        Assert.Equal("Notebook Atlas 14", ((JsonElement)result.Rows[0]["name"]!).GetString());
        Assert.Equal(3299.90m, ((JsonElement)result.Rows[0]["price"]!).GetDecimal());
    }

    // Verifica se a paginação retorna os registros corretos sem alterar o total de resultados.
    [Theory]
    [InlineData(1, 5, 1, 5, 12)]
    [InlineData(2, 5, 6, 5, 12)]
    [InlineData(3, 20, 41, 20, 3)]
    [InlineData(2, 30, 31, 30, 2)]
    [InlineData(13, 5, 61, 0, 12)]
    public async Task Execute_paginates_without_changing_total(int page, int size, int firstId, int count, int pages)
    {
        var result = await Execute(new() { Columns = ["id"], Page = page, PageSize = size });
        Assert.Equal(page, result.Page);
        Assert.Equal(size, result.PageSize);
        Assert.Equal(60, result.TotalCount);
        Assert.Equal(pages, result.TotalPages);
        Assert.Equal(Enumerable.Range(firstId, count), result.Rows.Select(row => Number(row, "id")));
    }

    // Verifica se filtros combinados e ordenação por preço produzem os resultados esperados.
    [Fact]
    public async Task Execute_combines_filters_and_sorts_by_price()
    {
        var result = await Execute(new()
        {
            Columns = ["id", "price"],
            Filters = [new("category", "eq", "Notebooks"), new("price", "lt", "4000")],
            OrderBy = [new("price", "DESC")],
            PageSize = 5
        });
        Assert.Equal(4, result.TotalCount);
        Assert.Equal(1, result.TotalPages);
        Assert.Equal(new[] { 2, 1, 5, 9 }, result.Rows.Select(row => Number(row, "id")));
    }

    // Verifica se os operadores de texto filtram os produtos conforme o valor informado.
    [Theory]
    [InlineData("contains", "Gamer", 7, 8)]
    [InlineData("startsWith", "Notebook Atlas", 1, 2)]
    [InlineData("endsWith", "14", 1, 5, 10)]
    public async Task Execute_applies_text_operators(string op, string value, params int[] ids)
    {
        var result = await Execute(new()
        {
            Columns = ["id"],
            Filters = [new("category", "eq", "Notebooks"), new("name", op, value)]
        });
        Assert.Equal(ids.Length, result.TotalCount);
        Assert.Equal(ids, result.Rows.Select(row => Number(row, "id")));
    }

    // Verifica se valores especiais são tratados como texto literal, sem alterar a consulta.
    [Theory]
    [InlineData("eq", "nonexistent")]
    [InlineData("eq", "' OR 1=1 --")]
    [InlineData("contains", "%")]
    [InlineData("contains", "_")]
    public async Task Execute_treats_special_values_as_literal_text(string op, string value)
    {
        var result = await Execute(new() { Columns = ["id"], Filters = [new("name", op, value)] });
        Assert.Empty(result.Rows);
        Assert.Equal(0, result.TotalCount);
        Assert.Equal(0, result.TotalPages);
    }

    // Verifica se requisições inválidas retornam respostas ProblemDetails com status 400.
    [Theory]
    [InlineData("generate")]
    [InlineData("execute")]
    public async Task Invalid_requests_return_problem_details(string endpoint)
    {
        string[] bodies =
        [
            """{"columns":[]}""",
            """{"columns":["id"],"page":0}""",
            """{"columns":["id"],"pageSize":7}""",
            """{"columns":["unknown"]}""",
            """{"columns":["id","id"]}""",
            """{"columns":["id"],"filters":[{"column":"price","operator":"contains","value":"1"}]}""",
            """{"columns":["id"],"filters":[{"column":"price","operator":"eq","value":"invalid"}]}""",
            """{"columns":["id"],"orderBy":[{"column":"id","direction":"INVALID"}]}""",
            """{"columns":["id"],"page":"invalid"}""",
            "{invalid json"
        ];
        foreach (var body in bodies)
        {
            using var content = new StringContent(body, Encoding.UTF8, "application/json");
            using var response = await fixture.Client.PostAsync($"/api/query/{endpoint}", content);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
            var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Equal(400, problem.GetProperty("status").GetInt32());
            Assert.False(string.IsNullOrWhiteSpace(problem.GetProperty("title").GetString()));
            Assert.False(string.IsNullOrWhiteSpace(problem.GetProperty("detail").GetString()));
        }
    }

    // Função auxiliar para executar uma requisição de consulta e retornar o resultado.
    private async Task<QueryResult> Execute(QueryRequest request)
    {
        using var response = await fixture.Client.PostAsJsonAsync("/api/query/execute", request);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<QueryResult>())!;
    }

    private static int Number(IDictionary<string, object?> row, string column) => ((JsonElement)row[column]!).GetInt32();
}
