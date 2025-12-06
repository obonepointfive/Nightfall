using System.Threading.Tasks;

namespace NeonSDK
{
    // Simple abstraction so you can plug in NAudio, MediaPlayer, etc.
    public interface INeonAudioPlayer
    {
        Task PlayAsync(byte[] wavData);
    }
}
