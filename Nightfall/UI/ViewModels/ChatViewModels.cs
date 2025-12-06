using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using NeonSDK;
using Nightfall.UI.Models;

namespace Nightfall.UI.ViewModels
{
    public sealed class ChatViewModel : INotifyPropertyChanged
    {
        private readonly NeonClient _client;
        private readonly INeonMicrophoneSource? _mic;
        private string _inputText = string.Empty;
        private bool _isStreaming;
        private CancellationTokenSource? _cts;

        public ObservableCollection<ChatMessage> Messages { get; } = new();

        public string InputText
        {
            get => _inputText;
            set { _inputText = value; OnPropertyChanged(); }
        }

        public bool IsStreaming
        {
            get => _isStreaming;
            set { _isStreaming = value; OnPropertyChanged(); }
        }

        public ICommand SendCommand { get; }
        public ICommand VoiceCommand { get; }
        public ICommand StopCommand { get; }

        public event PropertyChangedEventHandler? PropertyChanged;

        public ChatViewModel(
            NeonClient client,
            INeonMicrophoneSource? mic = null)
        {
            _client = client;
            _mic = mic;

            _client.Events.AssistantToken += OnAssistantToken;
            _client.Events.AssistantComplete += OnAssistantComplete;
            _client.Events.Error += OnError;

            SendCommand = new RelayCommand(async _ => await SendTextAsync(), _ => !IsStreaming);
            VoiceCommand = new RelayCommand(async _ => await SendVoiceAsync(), _ => !IsStreaming && _mic != null);
            StopCommand = new RelayCommand(_ => CancelStreaming(), _ => IsStreaming);
        }

        private void CancelStreaming()
        {
            _cts?.Cancel();
        }

        private async Task SendTextAsync()
        {
            if (string.IsNullOrWhiteSpace(InputText))
                return;

            var text = InputText.Trim();
            InputText = string.Empty;

            // Add user message
            AddMessage(ChatSender.User, text);

            _cts = new CancellationTokenSource();
            IsStreaming = true;

            try
            {
                var tone = new NeonTone
                {
                    Energy = "medium",
                    Label = "focused"
                };

                await _client.SendTextAsync(
                    text,
                    speak: true,
                    source: "typed",
                    tone: tone,
                    cancellationToken: _cts.Token);
            }
            finally
            {
                IsStreaming = false;
            }
        }

        private async Task SendVoiceAsync()
        {
            if (_mic == null)
                return;

            _cts = new CancellationTokenSource();
            IsStreaming = true;

            try
            {
                var tone = new NeonTone
                {
                    Energy = "high",
                    Label = "engaged"
                };

                await _client.SendVoiceAsync(
                    _mic,
                    speak: true,
                    tone: tone,
                    cancellationToken: _cts.Token);
            }
            finally
            {
                IsStreaming = false;
            }
        }

        private void OnAssistantToken(string text)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                if (Messages.Count == 0 || Messages[^1].Sender != ChatSender.Assistant)
                {
                    Messages.Add(new ChatMessage
                    {
                        Sender = ChatSender.Assistant,
                        Text = text
                    });
                }
                else
                {
                    Messages[^1].Text += text;
                }
            });
        }

        private void OnAssistantComplete()
        {
            // You might add UI state changes here later.
        }

        private void OnError(string err)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                Messages.Add(new ChatMessage
                {
                    Sender = ChatSender.Assistant,
                    Text = $"[error] {err}"
                });
            });
        }

        private void AddMessage(ChatSender sender, string text)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                Messages.Add(new ChatMessage { Sender = sender, Text = text });
            });
        }

        private void OnPropertyChanged([CallerMemberName] string? name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    // Simple RelayCommand implementation
    public sealed class RelayCommand : ICommand
    {
        private readonly Func<object?, Task>? _asyncExecute;
        private readonly Action<object?>? _execute;
        private readonly Predicate<object?>? _canExecute;

        public event EventHandler? CanExecuteChanged;

        public RelayCommand(Func<object?, Task> execute, Predicate<object?>? canExecute = null)
        {
            _asyncExecute = execute;
            _canExecute = canExecute;
        }

        public RelayCommand(Action<object?> execute, Predicate<object?>? canExecute = null)
        {
            _execute = execute;
            _canExecute = canExecute;
        }

        public bool CanExecute(object? parameter) =>
            _canExecute?.Invoke(parameter) ?? true;

        public async void Execute(object? parameter)
        {
            if (_asyncExecute != null)
                await _asyncExecute(parameter);
            else
                _execute?.Invoke(parameter);
        }

        public void RaiseCanExecuteChanged() =>
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }
}
