using System.Windows;
using DailyPoints.Databases;
using DailyPoints.Views;
using Prism.Ioc;

namespace DailyPoints
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App
    {
        protected override Window CreateShell()
        {
            return Container.Resolve<MainWindow>();
        }

        protected override void RegisterTypes(IContainerRegistry containerRegistry)
        {
            containerRegistry.Register<IRepository, PointTransactionRepository>();
        }
    }
}