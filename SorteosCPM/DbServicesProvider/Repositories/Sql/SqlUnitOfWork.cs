using System.Data;
using DbServicesProvider.Interfaces;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;

namespace DbServicesProvider.Repositories.Sql;

public class SqlUnitOfWork : IUnitOfWork
{
    private readonly ILogger _logger;
    private readonly SqlConnection _writeConnection;
    private readonly SqlConnection _readConnection;
    private SqlTransaction? _dbTransaction;
    public IRaffleRepository RaffleRepository { get; }
    public IAwardsRepository AwardsRepository { get; }
    public IParticipantRepository ParticipantRepository { get; }

    public SqlUnitOfWork(ILogger logger, string connectionString)
    {
        _logger = logger;
        _writeConnection = new SqlConnection(connectionString);
        _readConnection = new SqlConnection(connectionString);
        RaffleRepository = new SqlRaffleRepository(_readConnection, _writeConnection);
        AwardsRepository = new SqlAwardsRepository(_readConnection, _writeConnection);
        ParticipantRepository = new SqlParticipantRepository(_readConnection, _writeConnection);
    }
    public async Task StartConnectionAsync()
    {
        if (_readConnection.State != ConnectionState.Open)
        {
            await _readConnection.OpenAsync();
            _logger.LogInformation("Opening database read connection {_readConnection}.", _readConnection.State);
        }
    }

    public async Task BeginTransactionAsync()
    {
        if (_writeConnection.State != ConnectionState.Open)
        {
            await _writeConnection.OpenAsync();
            _logger.LogInformation("Opening database write connection {_writeConnection}.", _writeConnection.State);
        }
        _dbTransaction = (SqlTransaction)await _writeConnection.BeginTransactionAsync(IsolationLevel.ReadCommitted);
        _logger.LogInformation("Opening a transaction {_dbTransaction} for this database connection.", _dbTransaction.GetType().FullName);
        (RaffleRepository as SqlBaseRepository)?.SetTransaction(_dbTransaction);
        (AwardsRepository as SqlBaseRepository)?.SetTransaction(_dbTransaction);
        (ParticipantRepository as SqlBaseRepository)?.SetTransaction(_dbTransaction);
    }

    public async Task CommitAsync()
    {
        if (_dbTransaction != null)
            await _dbTransaction.CommitAsync();
    }
    public async Task RollbackAsync()
    {
        if (_dbTransaction != null)
            await _dbTransaction.RollbackAsync();
    }
    public async ValueTask DisposeAsync()
    {
        if (_dbTransaction != null)
            await _dbTransaction.DisposeAsync();

        await _writeConnection.DisposeAsync();
        await _readConnection.DisposeAsync();
    }
}
