using System;
using System.Windows;

namespace Demo.WindowsPresentation
{
    public partial class App : Application
    {

        protected override void OnStartup(StartupEventArgs e)
        {
            StartupUri = new Uri("MainWindow.xaml", UriKind.Relative);
        }

    }
}
