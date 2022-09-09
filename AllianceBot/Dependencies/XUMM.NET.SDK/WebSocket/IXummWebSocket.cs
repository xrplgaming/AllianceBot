using System.Collections.Generic;
using System.Threading;

namespace XUMM.NET.SDK.EMBRS
{
    public interface IXummWebSocket
    {
        IAsyncEnumerable<string> SubscribeAsync(string payloadUuid, CancellationToken cancellationToken);
    }
}
