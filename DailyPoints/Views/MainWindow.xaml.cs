using System;
using System.ComponentModel;

namespace DailyPoints.Views
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void MainWindow_OnClosing(object sender, CancelEventArgs e)
        {
            if (DataContext is IDisposable disposableVm)
            {
                disposableVm.Dispose();
            }
        }
    }
}