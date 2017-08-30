// --------------------------------------------------------------------------------------------------------------------
// <copyright file="MainView.xaml.cs" company="Martin Amareld">
//   Copyright(c) 2017 Martin Amareld. All rights reserved. 
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace EdSioux.Views
{
    using System;
    using System.ComponentModel;
    using System.Windows;
    using System.Windows.Media.Animation;
    using System.Windows.Threading;

    public partial class MainView
    {
        private readonly MainViewModel _viewModel;
        private readonly Dispatcher _uiDispatcher;

        public MainView()
        {
            InitializeComponent();

            _viewModel = (MainViewModel)DataContext;
            _viewModel.ShowMessage += OnShowMessage;

            _uiDispatcher = Dispatcher.CurrentDispatcher;

            Left = SystemParameters.PrimaryScreenWidth - Width - 50;
            Top = (SystemParameters.PrimaryScreenHeight / 2) - (Height / 2);
        }

        private void OnShowMessage(object sender, EventArgs eventArgs)
        {
            _uiDispatcher.BeginInvoke(
                new Action(
                    () =>
                        {
                            DurationKeyFrame.KeyTime = _viewModel.MessageDurationKeyTime;
                            ClosedKeyFrame.KeyTime = _viewModel.MessageClosedKeyTime;
                            VisualStateManager.GoToElementState(this, "ShowMessage", false);
                        }));
        }

        private void OnClosing(object sender, CancelEventArgs eventArgs)
        {
            _viewModel.Stop();
        }

        private void OnLoaded(object sender, RoutedEventArgs eventArgs)
        {
            _viewModel.Start();
        }

        private void OnShowMessageCompleted(object sender, EventArgs eventArgs)
        {
            var clockGroup = (ClockGroup)sender;
            switch (clockGroup.CurrentState)
            {
                case ClockState.Filling:
                    VisualStateManager.GoToElementState(this, "Default", false);
                    break;
                case ClockState.Stopped:
                    _viewModel.MessageCompleted();
                    break;
                ////case ClockState.Active:
                ////default:
                ////    break;
            }
        }

        private void OnExitClick(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}