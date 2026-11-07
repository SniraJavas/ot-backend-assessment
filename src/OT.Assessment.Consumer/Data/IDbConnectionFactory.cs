using Microsoft.Data.SqlClient;

namespace OT.Assessment.Consumer.Data;

public interface IDbConnectionFactory
{
    SqlConnection CreateConnection();
}