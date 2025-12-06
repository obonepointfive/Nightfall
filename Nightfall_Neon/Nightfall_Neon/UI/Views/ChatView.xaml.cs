using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Nightfall.UI.ViewModels;

namespace Nightfall.UI.Views
{
    public partial class ChatView : UserControl
    {
        public ChatView()
        {
            InitializeComponent();
        }

        // ---------------------------------------------------------
        // MIC BUTTON EVENTS
        // ---------------------------------------------------------

        private void MicToggle_Checked(object sender, RoutedEventArgs e)
        {
            if (DataContext is ChatViewModel vm)
            {
                vm.StartPressToTalk();
            }
        }

        private async void MicToggle_Unchecked(object sender, RoutedEventArgs e)
        {
            if (DataContext is ChatViewModel vm)
            {
                await vm.StopPressToTalkAsync();
            }
        }
    }
}
