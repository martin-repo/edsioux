// --------------------------------------------------------------------------------------------------------------------
// <copyright file="LoadView.xaml.cs" company="Martin Amareld">
//   Copyright(c) 2017 Martin Amareld. All rights reserved. 
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace EdSioux.Views
{
    using System.Windows;

    public partial class LoadView
    {
        private readonly LoadViewModel _viewModel;

        public LoadView()
        {
            InitializeComponent();

            _viewModel = (LoadViewModel)DataContext;

            Left = SystemParameters.PrimaryScreenWidth - Width - 50;
            Top = (SystemParameters.PrimaryScreenHeight / 2) - 175;
        }

        private void OnProgressChanged(object sender, RoutedPropertyChangedEventArgs<double> eventArgs)
        {
            if (eventArgs.NewValue >= 100)
            {
                _viewModel.Cleanup();
                Close();
            }
        }
    }
}