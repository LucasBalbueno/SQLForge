using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Testcontainers.PostgreSql;

namespace SQLForce.TestsIntegration;

// Essa classe inicia a aplicação real definida em Program.cs com um banco PostgreSQL temporário
public sealed class ApiFixture : WebApplicationFactory<Program>, IAsyncLifetime
{
    // Configura o banco de dados usado pelos testes
    private readonly PostgreSqlContainer database = new PostgreSqlBuilder("postgres:17-alpine")
        .WithDatabase("sqlforge_tests")
        .WithUsername("sqlforge")
        .WithPassword("integration-tests-only")
        .Build();

    // Essa propriedade expõe o cliente HTTP que os testes usarão
    public HttpClient Client { get; private set; } = null!;

    // Sobrescreve a configuração do WebHost para usar o banco de dados temporário
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:SqlForge", database.GetConnectionString());
    }

    // Inicializa o banco de dados e cria o cliente HTTP antes de qualquer teste ser executado
    public async Task InitializeAsync()
    {
        await database.StartAsync();
        Client = CreateClient();
    }

    // Limpa os recursos usados pelo banco de dados e pelo cliente HTTP após todos os testes serem executados
    async Task IAsyncLifetime.DisposeAsync()
    {
        Client?.Dispose();
        try
        {
            await base.DisposeAsync();
        }
        finally
        {
            await database.DisposeAsync();
        }
    }
}
