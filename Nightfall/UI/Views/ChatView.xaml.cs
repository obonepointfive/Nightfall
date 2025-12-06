using System.Net.Http;
using System.Windows;
using System.Windows.Controls;
using NeonSDK;
using Nightfall.UI.ViewModels;

namespace Nightfall.UI.Views
{
    public partial class ChatView : UserControl
    {
        public ChatView()
        {
            InitializeComponent();

            // TODO: plug in a real INeonAudioPlayer + INeonMicrophoneSource
            INeonAudioPlayer? audioPlayer = null;
            INeonMicrophoneSource? microphone = null;

            var neonClient = new NeonClient(audioPlayer, new HttpClient());
            DataContext = new ChatViewModel(neonClient, microphone);
        }
    }
}
