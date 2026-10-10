using System.Windows;
using TextWorkbench.Presentation.ViewModels;
using TextWorkbench.Presentation.Views;

namespace TextWorkbench
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private void OnStartup(object sender, StartupEventArgs e)
        {
            var viewModel = new MainWindowViewModel();
            var mainWindow = new MainWindow
            {
                DataContext = viewModel
            };
            mainWindow.Show();
        }
    }
}
