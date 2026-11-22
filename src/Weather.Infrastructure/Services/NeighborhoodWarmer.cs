using System.Threading.Channels;
using Microsoft.Extensions.Logging;
using Weather.Core.Abstractions;
using Weather.Core.Models;
using Weather.Infrastructure.Logging;

namespace Weather.Infrastructure.Services;

internal sealed class NeighborhoodWarmer(ILogger<NeighborhoodWarmer> logger) : INeighborhoodWarmer
{
    private readonly Channel<GridPoint> _channel = Channel.CreateBounded<GridPoint>(new BoundedChannelOptions(capacity: 256)
    {
        FullMode = BoundedChannelFullMode.DropOldest,
        SingleReader = true,
        SingleWriter = false,
    });

    public ChannelReader<GridPoint> Reader => _channel.Reader;

    public void RequestWarming(GridPoint origin)
    {
        if (!_channel.Writer.TryWrite(origin))
        {
            logger.WarmingQueueClosed(origin);
        }
    }
}
