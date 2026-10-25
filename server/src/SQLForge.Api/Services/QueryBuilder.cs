using System.Globalization;
using Dapper;
using SQLForge.Api.Models;

namespace SQLForge.Api.Services;

public sealed class QueryBuilder
{
    // Operadores permitidos em colunas textuais e numéricas
    private static readonly string[] TextOperators = ["eq", "ne", "contains", "startsWith", "endsWith"];
    private static readonly string[] NumberOperators = ["eq", "ne", "gt", "gte", "lt", "lte"];

    // Metadados das colunas da tabela de produtos. Define tudo que a API aceita consultar
    public static readonly IReadOnlyList<ColumnMetadata> Columns = new List<ColumnMetadata>
    {
        new("id", "ID", "integer", NumberOperators),
        new("name", "Nome", "text", TextOperators),
        new("category", "Categoria", "text", TextOperators),
        new("price", "Preço", "decimal", NumberOperators),
        new("stock_quantity", "Estoque", "integer", NumberOperators)
    }.AsReadOnly();

    // Transforma uma QueryRequest em uma BuiltQuery
    public BuiltQuery Build(QueryRequest request)
    {
        // Validação do número de colunas
        if (request.Columns is not { Count: > 0 and <= 5 } || request.Columns.Distinct().Count() != request.Columns.Count)
            throw Invalid("Selecione de uma a cinco colunas, sem repetições.");

        // Validação dos nomes das colunas
        foreach (var column in request.Columns) GetColumn(column);

        // Validação da paginação
        if (request.Page < 1 || request.PageSize is not (5 or 10 or 20 or 30))
            throw Invalid("A página deve ser positiva e o tamanho deve ser 5, 10, 20 ou 30.");

        // Validação de filtos e ordenações (Limitado por performance)
        if (request.Filters is null || request.Filters.Count > 20 || request.OrderBy is null || request.OrderBy.Count > 5)
            throw Invalid("Informe no máximo 20 filtros e 5 ordenações.");

        // Armazena os valores dos parâmetros e das condições textuais e parametrizadas para a query
        var parameters = new DynamicParameters();
        var literalConditions = new List<string>();
        var parameterConditions = new List<string>();

        // Percorre os filtros pelo índice
        for (var index = 0; index < request.Filters.Count; index++)
        {
            var filter = request.Filters[index];
            // Verifica se o filtro é nulo
            if (filter is null) throw Invalid("Filtro inválido.");

            // Verifica se a coluna do filtro é válida e se o operador é permitido para o tipo da coluna
            var column = GetColumn(filter.Column);
            if (!column.Operators.Contains(filter.Operator)) throw Invalid("Operador inválido para a coluna selecionada.");

            // Converte o valor do filtro para o tipo correto da coluna
            var value = ParseValue(column, filter.Value);

            // Converte o operador do filtro para o operador SQL correspondente
            var sqlOperator = filter.Operator switch
            {
                "eq" => "=",
                "ne" => "<>",
                "gt" => ">",
                "gte" => ">=",
                "lt" => "<",
                "lte" => "<=",
                _ => "LIKE"
            };

            // Tratamento de operadores textuais (LIKE)
            var suffix = "";
            if (sqlOperator == "LIKE")
            {
                var text = ((string)value).Replace("!", "!!").Replace("%", "!%").Replace("_", "!_");

                value = filter.Operator switch
                {
                    "contains" => $"%{text}%",
                    "startsWith" => $"{text}%",
                    _ => $"%{text}"
                };

                suffix = " ESCAPE '!'";
            }

            // Criação dos parâmetros e das condições textuais e parametrizadas para a query
            var parameter = $"p{index}";
            parameters.Add(parameter, value);
            literalConditions.Add($"{column.Name} {sqlOperator} {Literal(value)}{suffix}");
            parameterConditions.Add($"{column.Name} {sqlOperator} @{parameter}{suffix}");
        }

        // Construção da ordenação
        var orders = new List<string>();
        var orderedColumns = new HashSet<string>();

        // Validação da ordenação
        foreach (var order in request.OrderBy)
        {
            if (order is null) throw Invalid("Ordenação inválida.");

            var column = GetColumn(order.Column);

            if (order.Direction is not ("ASC" or "DESC") || !orderedColumns.Add(column.Name))
                throw Invalid("Use ASC ou DESC e não repita colunas na ordenação.");
            orders.Add($"{column.Name} {order.Direction}");
        }

        // Ordena por ID mesmo que o usuário não solicite
        if (!orderedColumns.Contains("id")) orders.Add("id ASC");

        // Construção do SELECT, ORDER BY, OFFSET e LIMIT (Paginação)
        var select = $"SELECT {string.Join(", ", request.Columns)}\nFROM products";
        var orderBy = $"\nORDER BY {string.Join(", ", orders)}";
        var offset = ((long)request.Page - 1) * request.PageSize;
        parameters.Add("limit", request.PageSize);
        parameters.Add("offset", offset);

        // Retorna a BuiltQuery com SQL textual, SQL parametrizado e SQL de contagem
        return new BuiltQuery(
            select + Where(literalConditions) + orderBy + $"\nLIMIT {request.PageSize} OFFSET {offset};",
            select + Where(parameterConditions) + orderBy + "\nLIMIT @limit OFFSET @offset;",
            "SELECT COUNT(*) FROM products" + Where(parameterConditions) + ";", parameters);
    }

    // Método que constroi a cláusula WHERE com operador AND
    private static string Where(List<string> conditions)
        => conditions.Count == 0 ? "" : "\nWHERE " + string.Join("\n  AND ", conditions);

    // Procura uma coluna pelo nome. Se não encontrar, lança uma exceção de validação
    private static ColumnMetadata GetColumn(string name)
        => Columns.FirstOrDefault(c => c.Name == name) ?? throw Invalid("Coluna inválida.");

    // Cria uma exceção de validação com a mensagem informada
    private static QueryValidationException Invalid(string message)
        => new(message);

    // Método para validar e converter os valores dos filtros
    private static object ParseValue(ColumnMetadata column, string? value)
    {
        // Validação de filtro nulo
        if (value is null || value.Length > 200 || value.Contains('\0')) throw Invalid("Informe um valor com até 200 caracteres, sem caracteres nulos.");

        // Validação de valores textuais
        if (column.Type == "text") return value;

        // Validação de valores numéricos
        if (column.Type == "integer" && int.TryParse(value, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var integer))
            return integer;
        if (column.Type == "decimal" && decimal.TryParse(value, NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint,
            CultureInfo.InvariantCulture, out var number)) return number;
        throw Invalid("Valor numérico inválido. Use ponto como separador decimal.");
    }

    // Cria a representação literal do SQL
    // Tira \\ e ' do valor para evitar SQL Injection, e adiciona E' no início e ' no final para indicar que é uma string literal
    private static string Literal(object value) => value is string text
        ? "E'" + text.Replace("\\", "\\\\").Replace("'", "''") + "'"
        : Convert.ToString(value, CultureInfo.InvariantCulture)!;
}
