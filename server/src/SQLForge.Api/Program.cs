using Npgsql;
using SQLForge.Api.Data;
using SQLForge.Api.Errors;
using SQLForge.Api.Models;
using SQLForge.Api.Services;
using SQLForge.Api.Repositories;
using Microsoft.AspNetCore.Diagnostics;

var builder = WebApplication.CreateBuilder(new WebApplicationOptions
{
    Args = args,
    WebRootPath = Path.Combine(AppContext.BaseDirectory, "wwwroot")
});

// Render fornece PORT; localmente os launch profiles continuam funcionando.
if (builder.Configuration["PORT"] is { Length: > 0 } port)
{
    if (!int.TryParse(port, out var number) || number is < 1 or > 65535)
        throw new InvalidOperationException("PORT deve ser uma porta TCP válida.");
    builder.WebHost.UseUrls($"http://0.0.0.0:{number}");
}

// Pega e valida a connection string das configurações
var connectionString = builder.Configuration.GetConnectionString("SqlForge");
if (string.IsNullOrWhiteSpace(connectionString)) throw new InvalidOperationException("Configure a ConnectionString");

// Injeção de dependência do NpgsqlDataSource para gerenciar conexões com o banco de dados
builder.Services.AddSingleton<NpgsqlDataSource>(_ =>
{
    try { return NpgsqlDataSource.Create(connectionString); }
    catch (ArgumentException)
    {
        throw new InvalidOperationException("ConnectionStrings__SqlForge inválida. Use o formato Npgsql.");
    }
});

// Injeção de dependência do DatabaseInitializer para inicializar o banco de dados
builder.Services.AddSingleton<DatabaseInitializer>();

// Injeção de dependência dos Services
builder.Services.AddScoped<QueryBuilder>();
builder.Services.AddScoped<QueryService>();

// Injeção de dependência do repository
builder.Services.AddScoped<IProductQueryRepository, ProductQueryRepository>();

// Injeção de dependência dos controllers
builder.Services.AddControllers().AddSafeApiErrors();

// Configuração do Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configuração do ProblemDetails para tratamento de erros
builder.Services.AddProblemDetails();

// Configuração do CORS frontend
builder.Services.AddCors(options => options.AddDefaultPolicy(policy => policy
    .WithOrigins(builder.Configuration.GetSection("ClientOrigins").Get<string[]>() ?? ["http://localhost:5500"])
    .AllowAnyHeader().AllowAnyMethod()));

var app = builder.Build();

// Ativo também em desenvolvimento para nunca retornar páginas de exceção aos clientes.
app.UseExceptionHandler();

// Middleware para capturar exceções não tratadas durante o processamento das requisições
app.UseExceptionHandler(handler => handler.Run(async context =>
{
    // Obtem a exceção capturada pelo middleware
    var exception = context.Features.Get<IExceptionHandlerFeature>()?.Error;
    // Identificação de erro de validação
    var validation = exception as QueryValidationException;
    // identificação de falha de conexão com o banco de dados
    var databaseFailure = exception is NpgsqlException or TimeoutException;
    // Escolhe o status code apropriado com base no tipo de exceção
    var status = validation is not null ? 400 : databaseFailure ? 503 : 500;

    // Criação da resposta ProblemDetails
    await Results.Problem(statusCode: status,
        title: validation is not null ? "Consulta inválida" : databaseFailure ? "Banco indisponível" : "Erro inesperado",
        detail: validation?.Message ?? (databaseFailure
            ? "Não foi possível consultar o banco. Verifique a conexão e tente novamente."
            : "Não foi possível concluir a operação.")).ExecuteAsync(context);
}));

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors();

// Em produção o Render termina TLS; o container recebe HTTP.
if (app.Environment.IsDevelopment()) app.UseHttpsRedirection();

app.UseDefaultFiles();
app.UseStaticFiles();

app.UseAuthorization();

app.MapControllers();

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

try
{
    await app.Services.GetRequiredService<DatabaseInitializer>().InitializeAsync();
    app.Logger.LogInformation("Seed executada com sucesso.");
}
catch (Exception exception) when (exception is NpgsqlException or TimeoutException)
{
    throw new InvalidOperationException("Não foi possível inicializar o banco. Confira se o PostgreSQL está ativo, se o banco existe e se a conexão foi configurada. Consulte a documentação.");
}

app.Run();

// Permite iniciar a aplicação real nos testes de integração
public partial class Program { }
