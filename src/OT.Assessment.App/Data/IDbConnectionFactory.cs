
using Microsoft.Data.SqlClient;

namespace OT.Assessment.App.Data;

public interface IDbConnectionFactory
{
    SqlConnection CreateConnection();
}
