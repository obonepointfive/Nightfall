using System.Threading;
using System.Threading.Tasks;

namespace NeonSDK
{
    public interface INeonAudioPlayer
    {
        Task PlayAsync(byte[] wavBytes, CancellationToken cancellationToken = default);
    }
}
