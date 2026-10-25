using Dapper;
using Npgsql;

namespace SQLForge.Api.Data;

public sealed class DatabaseInitializer(NpgsqlDataSource dataSource)
{
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        // Le o arquivo seed.sql
        var sql = await File.ReadAllTextAsync(Path.Combine(AppContext.BaseDirectory, "Data", "seed.sql"), cancellationToken);
        
        // Abre uma conexão com o banco
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        
        // Inicia uma unica transação no PostgreSQL
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        
        // Envia o conteúdo do seed.sql ao PostgreSQL
        await connection.ExecuteAsync(new CommandDefinition(sql, transaction: transaction, commandTimeout: 30, cancellationToken: cancellationToken));
        
        // Confirma as transações no banco
        await transaction.CommitAsync(cancellationToken);
    }
}