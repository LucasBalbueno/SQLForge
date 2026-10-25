using SQLForge.Api.Models;
using SQLForge.Api.Services;

namespace SQLForge.UnitTests;

public sealed class QueryBuilderTests
{
    private readonly QueryBuilder builder = new();

    // Verifica a seleção de colunas, a ordenação estável e a paginação padrão de uma consulta sem filtros.
    [Fact]
    public void BuildsDefaultQuery()
    {
        var request = new QueryRequest { Columns = ["name", "price"] };

        var query = builder.Build(request);

        Assert.Equal("SELECT name, price\nFROM products\nORDER BY id ASC\nLIMIT 10 OFFSET 0;", query.Sql);
        Assert.EndsWith("LIMIT @limit OFFSET @offset;", query.ParameterizedSql);
        Assert.Equal(10, query.Parameters.Get<int>("limit"));
        Assert.Equal(0L, query.Parameters.Get<long>("offset"));
        Assert.Equal("SELECT COUNT(*) FROM products;", query.CountSql);
    }

    // Verifica se filtros combinados valem para a consulta e a contagem, com ordenação e paginação solicitadas.
    [Fact]
    public void AppliesFiltersOrderingAndPagination()
    {
        var request = new QueryRequest
        {
            Columns = ["name", "price"],
            Filters = [new("price", "gte", "100.50"), new("stock_quantity", "gt", "0")],
            OrderBy = [new("price", "DESC")],
            Page = 3,
            PageSize = 20
        };

        var query = builder.Build(request);

        Assert.Contains("WHERE price >= @p0\n  AND stock_quantity > @p1", query.ParameterizedSql);
        Assert.Equal("SELECT COUNT(*) FROM products\nWHERE price >= @p0\n  AND stock_quantity > @p1;", query.CountSql);
        Assert.Contains("ORDER BY price DESC, id ASC", query.ParameterizedSql);
        Assert.Equal(100.50m, query.Parameters.Get<decimal>("p0"));
        Assert.Equal(0, query.Parameters.Get<int>("p1"));
        Assert.Equal(20, query.Parameters.Get<int>("limit"));
        Assert.Equal(40L, query.Parameters.Get<long>("offset"));
    }

    // Garante que um valor com SQL malicioso seja enviado como parâmetro, sem entrar no SQL executável.
    [Fact]
    public void KeepsUserValuesInParameters()
    {
        const string value = "'; DROP TABLE products; --";
        var request = new QueryRequest { Columns = ["name"], Filters = [new("name", "eq", value)] };

        var query = builder.Build(request);

        Assert.Contains("name = @p0", query.ParameterizedSql);
        Assert.DoesNotContain(value, query.ParameterizedSql);
        Assert.DoesNotContain(value, query.CountSql);
        Assert.Equal(value, query.Parameters.Get<string>("p0"));
        Assert.Contains("name = E'''; DROP TABLE products; --'", query.Sql);
    }

    // Garante que os caracteres especiais informados pelo usuário sejam tratados como texto na busca LIKE.
    [Fact]
    public void EscapesLikeCharacters()
    {
        var request = new QueryRequest { Columns = ["name"], Filters = [new("name", "contains", "50%_!")] };

        var query = builder.Build(request);

        Assert.Equal("%50!%!_!!%", query.Parameters.Get<string>("p0"));
        Assert.Contains("LIKE @p0 ESCAPE '!'", query.ParameterizedSql);
    }

    // Rejeita a ausência de colunas, nomes fora da lista permitida e colunas repetidas.
    [Theory]
    [InlineData()]
    [InlineData("id; DROP TABLE products")]
    [InlineData("name", "name")]
    public void RejectsInvalidColumns(params string[] columns)
    {
        var request = new QueryRequest { Columns = [.. columns] };

        Assert.Throws<QueryValidationException>(() => builder.Build(request));
    }

    // Rejeita operadores incompatíveis e valores que não correspondem ao tipo numérico da coluna.
    [Theory]
    [InlineData("name", "gt", "text")]
    [InlineData("price", "eq", "invalid")]
    [InlineData("id", "eq", "1.5")]
    public void RejectsInvalidFilters(string column, string operation, string value)
    {
        var request = new QueryRequest { Columns = ["id"], Filters = [new(column, operation, value)] };

        Assert.Throws<QueryValidationException>(() => builder.Build(request));
    }

    // Rejeita páginas não positivas e tamanhos de página fora das opções disponíveis.
    [Theory]
    [InlineData(0, 10)]
    [InlineData(1, 7)]
    public void RejectsInvalidPagination(int page, int pageSize)
    {
        var request = new QueryRequest { Columns = ["id"], Page = page, PageSize = pageSize };

        Assert.Throws<QueryValidationException>(() => builder.Build(request));
    }

    // Impede que uma direção de ordenação contendo SQL seja aceita no lugar de ASC ou DESC.
    [Fact]
    public void RejectsInvalidOrderDirection()
    {
        var request = new QueryRequest { Columns = ["id"], OrderBy = [new("id", "DESC; DELETE FROM products")] };

        Assert.Throws<QueryValidationException>(() => builder.Build(request));
    }
}
