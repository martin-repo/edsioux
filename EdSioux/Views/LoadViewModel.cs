// --------------------------------------------------------------------------------------------------------------------
// <copyright file="LoadViewModel.cs" company="Martin Amareld">
//   Copyright(c) 2017 Martin Amareld. All rights reserved. 
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace EdSioux.Views
{
    using System.ComponentModel;
    using System.Runtime.CompilerServices;
    using System.Windows;
    using System.Windows.Media;

    using EdSioux.Color;
    using EdSioux.Sioux;

    using JetBrains.Annotations;

    public class LoadViewModel : INotifyPropertyChanged
    {
        public LoadViewModel()
        {
            if ((bool)DesignerProperties.IsInDesignModeProperty.GetMetadata(typeof(DependencyObject)).DefaultValue)
            {
                return;
            }

            HudBrush = ColorManager.Brushes[ColorType.Default];
            SiouxManager.Instance.LoadProgress += OnLoadProgress;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public Brush HudBrush { get; set; }

        public int PercentCompleted { get; set; }

        public void Cleanup()
        {
            SiouxManager.Instance.LoadProgress -= OnLoadProgress;
        }

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void OnLoadProgress(object sender, ProgressEventArgs eventArgs)
        {
            PercentCompleted = eventArgs.Percent;
            OnPropertyChanged(nameof(PercentCompleted));
        }
    }
}