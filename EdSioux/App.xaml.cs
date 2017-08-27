// --------------------------------------------------------------------------------------------------------------------
// <copyright file="App.xaml.cs" company="Martin Amareld">
//   Copyright(c) 2017 Martin Amareld. All rights reserved. 
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace EdSioux
{
    using System;
    using System.Diagnostics;

    public partial class App 
    {
        public App()
        {
            AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
        }

        private static void OnUnhandledException(object sender, UnhandledExceptionEventArgs eventArgs)
        {
            if (Debugger.IsAttached)
            {
                Debugger.Break();
            }
        }
    }
}