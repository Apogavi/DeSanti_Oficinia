using System.Data.Common;
using DeSanti.Application.Interfaces;
using MySqlConnector;

namespace DeSanti.Infrastructure.Data;

public sealed class MySqlConnectionFactory(string? connectionString) : IDbConnectionFactory
{
    public DbConnection CreateConnection()
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException("Configure ConnectionStrings:DefaultConnection antes de acessar o MySQL.");

        return new MySqlConnection(connectionString);
    }
}
