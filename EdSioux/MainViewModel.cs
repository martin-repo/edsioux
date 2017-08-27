// --------------------------------------------------------------------------------------------------------------------
// <copyright file="MainViewModel.cs" company="Martin Amareld">
//   Copyright(c) 2017 Martin Amareld. All rights reserved. 
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace EdSioux
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Runtime.CompilerServices;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Windows;
    using System.Windows.Media;
    using System.Windows.Media.Animation;

    using EdNetApi.Common;

    using JetBrains.Annotations;

    public class MainViewModel : INotifyPropertyChanged
    {
        private readonly SiouxManager _siouxManager;
        private readonly BlockingCollection<SiouxEventArgs> _messageQueue;
        private readonly ManualResetEventSlim _viewMessageCompletedEvent;

        private Task _messageDispatcherTask;
        private CancellationTokenSource _messageDispatcherTokenSource;

        public MainViewModel()
        {
            if ((bool)DesignerProperties.IsInDesignModeProperty.GetMetadata(typeof(DependencyObject)).DefaultValue)
            {
                return;
            }

            _siouxManager = new SiouxManager();

            HudBrush = Brushes.GhostWhite;
            HudColor = Colors.GhostWhite;
            GlowColor = Colors.Yellow;
            TitleColor = Colors.Red;

            var messageQueue = new ConcurrentQueue<SiouxEventArgs>();
            _messageQueue = new BlockingCollection<SiouxEventArgs>(messageQueue);
            _viewMessageCompletedEvent = new ManualResetEventSlim();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public event EventHandler<EventArgs> ShowMessage;

        public Color TitleColor { get; set; }

        public Color HudColor { get; set; }

        public Brush HudBrush { get; set; }

        public Color GlowColor { get; set; }

        public KeyTime MessageDurationKeyTime { get; set; }

        public KeyTime MessageClosedKeyTime { get; set; }

        public string MessageHeader { get; set; }

        public List<SiouxMessagePart> MessageParts { get; private set; }

        public void MessageCompleted()
        {
            _viewMessageCompletedEvent.Set();
        }

        public void Start()
        {
            Stop();

            _siouxManager.SiouxEventReceived += OnSiouxEventReceived;
            _messageDispatcherTokenSource = new CancellationTokenSource();
            _messageDispatcherTask = Task.Factory.StartNew(
                o => ProcessMessages(_messageDispatcherTokenSource.Token),
                null,
                TaskCreationOptions.LongRunning);
            _siouxManager.Start();
        }

        public void Stop()
        {
            _messageDispatcherTokenSource?.Cancel();
            _messageDispatcherTokenSource = null;

            _messageDispatcherTask?.Wait();
            _messageDispatcherTask = null;

            _siouxManager.Stop();
            _siouxManager.SiouxEventReceived -= OnSiouxEventReceived;
        }

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void OnSiouxEventReceived(object sender, SiouxEventArgs eventArgs)
        {
            _messageQueue.Add(eventArgs);
        }

        private void ProcessMessages(CancellationToken token)
        {
            while (true)
            {
                if (token.IsCancellationRequested)
                {
                    return;
                }

                try
                {
                    var eventArgs = _messageQueue.Take(token);

                    MessageHeader = eventArgs.Header;
                    OnPropertyChanged(nameof(MessageHeader));

                    MessageParts = eventArgs.MessageParts;
                    MessageParts.ForEach(
                        part =>
                            {
                                if (part.Foreground == null)
                                {
                                    part.Foreground = HudBrush;
                                }
                            });
                    OnPropertyChanged(nameof(MessageParts));

                    var duration = eventArgs.DisplayDuration + 1;
                    var close = duration + 1;

                    MessageDurationKeyTime = KeyTime.FromTimeSpan(TimeSpan.FromSeconds(duration));
                    OnPropertyChanged(nameof(MessageDurationKeyTime));

                    MessageClosedKeyTime = KeyTime.FromTimeSpan(TimeSpan.FromSeconds(close));
                    OnPropertyChanged(nameof(MessageClosedKeyTime));

                    _viewMessageCompletedEvent.Reset();
                    ShowMessage.Raise(this, new EventArgs());
                    _viewMessageCompletedEvent.Wait(token);
                }
                catch (OperationCanceledException)
                {
                    return;
                }
            }
        }
    }
}