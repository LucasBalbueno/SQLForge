using Npgsql;
using SQLForge.Api.Data;

var builder = WebApplication.CreateBuilder(args);

// Configura a conexão com o banco de dados PostgreSQL
var connectionString = builder.Configuration.GetConnectionString("SqlForge");
if (string.IsNullOrWhiteSpace(connectionString)) throw new InvalidOperationException("Configure a ConnectionString");

// Injeção de dependência do NpgsqlDataSource para gerenciar conexões com o banco de dados
builder.Services.AddSingleton<NpgsqlDataSource>(_ => NpgsqlDataSource.Create(connectionString));

// Injeção de dependência do DatabaseInitializer para inicializar o banco de dados
builder.Services.AddSingleton<DatabaseInitializer>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

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
