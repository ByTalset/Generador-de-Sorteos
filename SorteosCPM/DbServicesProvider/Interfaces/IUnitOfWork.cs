using System.Data.Common;

namespace DbServicesProvider.Interfaces;

public interface IUnitOfWork : IAsyncDisposable
{
    IRaffleRepository RaffleRepository { get; }
    IAwardsRepository AwardsRepository { get; }
    IParticipantRepository ParticipantRepository { get; }
    Task StartConnectionAsync();
    Task BeginTransactionAsync();
    Task CommitAsync();
    Task RollbackAsync();
}
