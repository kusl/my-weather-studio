using System.Threading.Channels;
using Weather.Core.Models;

namespace Weather.Core.Abstractions;

public interface INeighborhoodWarmer
{
    ChannelReader<GridPoint> Reader { get; }

    void RequestWarming(GridPoint origin);
}
