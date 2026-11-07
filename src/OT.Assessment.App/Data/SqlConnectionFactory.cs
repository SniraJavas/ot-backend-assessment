
using Microsoft.Data.SqlClient;

namespace OT.Assessment.App.Data;

public sealed class SqlConnectionFactory(
    IConfiguration configuration) : IDbConnectionFactory
{
    private readonly string _connectionString =
        configuration.GetConnectionString("DefaultConnection")
        ?? throw new InvalidOperationException(
            "DefaultConnection connection string is missing.");

    public SqlConnection CreateConnection()
    {
        return new SqlConnection(_connectionString);
    }
}
