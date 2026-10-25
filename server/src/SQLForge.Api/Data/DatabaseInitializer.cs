using Dapper;
using Npgsql;

namespace SQLForge.Api.Data;

public sealed class DatabaseInitializer(NpgsqlDataSource dataSource, ILogger<DatabaseInitializer> logger)
{
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        // Le o arquivo seed.sql
        var sql = await File.ReadAllTextAsync(Path.Combine(AppContext.BaseDirectory, "Data", "seed.sql"), cancellationToken);
        
        // Abre uma conexão com o banco
        await using var connection = await OpenConnectionAsync(cancellationToken);
        
        // Inicia uma unica transação no PostgreSQL
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        
        // Envia o conteúdo do seed.sql ao PostgreSQL
        await connection.ExecuteAsync(new CommandDefinition(sql, transaction: transaction, commandTimeout: 30, cancellationToken: cancellationToken));
        
        // Confirma as transações no banco
        await transaction.CommitAsync(cancellationToken);
    }

    // Repete somente a abertura da conexão, nunca erros SQL da seed.
    private async Task<NpgsqlConnection> OpenConnectionAsync(CancellationToken cancellationToken)
    {
        int[] delays = [2, 5];
        for (var attempt = 0; ; attempt++)
        {
            try
            {
                return await dataSource.OpenConnectionAsync(cancellationToken);
            }
            catch (Exception exception) when (attempt < delays.Length && IsTransientConnectionFailure(exception))
            {
                logger.LogWarning("Conexão temporariamente indisponível. Nova tentativa em {Seconds}s.", delays[attempt]);
                await Task.Delay(TimeSpan.FromSeconds(delays[attempt]), cancellationToken);
            }
        }
    }

    private static bool IsTransientConnectionFailure(Exception exception) => exception switch
    {
        PostgresException postgres => postgres.SqlState == "57P03" || postgres.SqlState.StartsWith("08", StringComparison.Ordinal),
        NpgsqlException npgsql => npgsql.IsTransient,
        TimeoutException => true,
        _ => false
    };
}
