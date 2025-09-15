using System.Threading.Channels;
using DbServicesProvider.Dto;

namespace WorkerService.Services;

public class LoadQueueService
{
    private readonly Channel<LoadFile> _channel = Channel.CreateUnbounded<LoadFile>();
    public async Task EnqueueAsync(LoadFile loadFile)
    {
        if (_channel.Writer.TryWrite(loadFile))
        {
            await _channel.Writer.WriteAsync(loadFile);
        }
    }
    public IAsyncEnumerable<LoadFile> DequeueAsync(CancellationToken cancellationToken) => _channel.Reader.ReadAllAsync(cancellationToken);
}
