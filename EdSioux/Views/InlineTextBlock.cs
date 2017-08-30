// --------------------------------------------------------------------------------------------------------------------
// <copyright file="InlineTextBlock.cs" company="Martin Amareld">
//   Copyright(c) 2017 Martin Amareld. All rights reserved. 
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace EdSioux.Views
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Documents;

    using EdSioux.Models;

    public class InlineTextBlock : TextBlock
    {
        public static readonly DependencyProperty MessageSourceProperty = DependencyProperty.Register(
            "MessageSource",
            typeof(List<SiouxMessagePart>),
            typeof(InlineTextBlock),
            new UIPropertyMetadata(null, OnPropertyChanged));

        public List<SiouxMessagePart> MessageSource
        {
            get => (List<SiouxMessagePart>)GetValue(MessageSourceProperty);
            set => SetValue(MessageSourceProperty, value);
        }

        private static void OnPropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs eventArgs)
        {
            var textBlock = (InlineTextBlock)sender;
            var source = (List<SiouxMessagePart>)eventArgs.NewValue;

            textBlock.Inlines.Clear();
            if (source != null)
            {
                textBlock.Inlines.AddRange(
                    source.Select(part => new Run { Text = part.Text, Foreground = part.Foreground }));
            }
        }
    }
}