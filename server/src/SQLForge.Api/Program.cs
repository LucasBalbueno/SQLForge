using Npgsql;

var builder = WebApplication.CreateBuilder(args);

// Configura a conexão com o banco de dados PostgreSQL
var connectionString = builder.Configuration.GetConnectionString("SqlForge");
if (string.IsNullOrWhiteSpace(connectionString)) throw new InvalidOperationException("Configure a ConnectionString");

// Injeção de dependência do NpgsqlDataSource para gerenciar conexões com o banco de dados
builder.Services.AddSingleton<NpgsqlDataSource>(_ => NpgsqlDataSource.Create(connectionString));

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

app.Run();
