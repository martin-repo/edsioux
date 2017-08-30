// --------------------------------------------------------------------------------------------------------------------
// <copyright file="App.xaml.cs" company="Martin Amareld">
//   Copyright(c) 2017 Martin Amareld. All rights reserved. 
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace EdSioux
{
    using System;
    using System.Diagnostics;
    using System.Windows;

    using Squirrel;

    public partial class App
    {
        public App()
        {
            AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
        }

        protected override async void OnStartup(StartupEventArgs eventArgs)
        {
            base.OnStartup(eventArgs);

            try
            {
                using (var updateManager = UpdateManager.GitHubUpdateManager("https://github.com/mbedatpro/edsioux"))
                {
                    await updateManager.Result.UpdateApp();
                }
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message);
            }
           
        }

        private static void OnUnhandledException(object sender, UnhandledExceptionEventArgs eventArgs)
        {
            if (Debugger.IsAttached)
            {
                Debugger.Break();
            }

            MessageBox.Show(((Exception)eventArgs.ExceptionObject).Message);
        }
    }
}