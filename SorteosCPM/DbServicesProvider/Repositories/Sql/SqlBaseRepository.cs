using Microsoft.Data.SqlClient;

namespace DbServicesProvider.Repositories.Sql;

public abstract class SqlBaseRepository
{
    protected readonly SqlConnection _readConnection;
    protected readonly SqlConnection _writeConnnection;
    protected SqlTransaction? _dbTransaction;
    protected SqlBaseRepository(SqlConnection readConnection, SqlConnection writeConnection)
    {
        _readConnection = readConnection;
        _writeConnnection = writeConnection;
    }

    public void SetTransaction(SqlTransaction dbTransaction)
    {
        _dbTransaction = dbTransaction;
    }
}
