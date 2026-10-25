using Npgsql;
using SQLForge.Api.Data;
using SQLForge.Api.Errors;
using SQLForge.Api.Repositories;
using SQLForge.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// Configura a conexão com o banco de dados PostgreSQL
var connectionString = builder.Configuration.GetConnectionString("SqlForge");
if (string.IsNullOrWhiteSpace(connectionString)) throw new InvalidOperationException("Configure a ConnectionString");

// Injeção de dependência do NpgsqlDataSource para gerenciar conexões com o banco de dados
builder.Services.AddSingleton<NpgsqlDataSource>(_ => NpgsqlDataSource.Create(connectionString));

// Injeção de dependência do DatabaseInitializer para inicializar o banco de dados
builder.Services.AddSingleton<DatabaseInitializer>();

// Injeção de dependência dos Services
builder.Services.AddScoped<QueryBuilder>();
builder.Services.AddScoped<QueryService>();

// Injeção de dependência do repository
builder.Services.AddScoped<IProductQueryRepository, ProductQueryRepository>();

// Injeção de dependência dos controllers
builder.Services.AddControllers().AddSafeApiErrors();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Ativo também em desenvolvimento para nunca retornar páginas de exceção aos clientes.
app.UseExceptionHandler();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

try
{
    await app.Services.GetRequiredService<DatabaseInitializer>().InitializeAsync();
    app.Logger.LogInformation("Seed executada com sucesso.");
}
catch (NpgsqlException)
{
    throw new InvalidOperationException("Não foi possível inicializar o banco. Confira se o PostgreSQL está ativo, se o banco existe e se a conexão foi configurada. Consulte a documentação.");
}

app.Run();
