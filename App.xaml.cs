using System.IO;
using System.Windows;
using GestionalePalestra.Data;
using GestionalePalestra.ViewModels;

namespace GestionalePalestra;

public partial class App : Application
{
    private async void Application_Startup(object sender, StartupEventArgs e)
    {
        try
        {
            var databasePath = Path.Combine(AppContext.BaseDirectory, "gym.sqlite");
            var repository = new GymRepository(databasePath);
            await repository.InitializeAsync();

            var viewModel = new MainViewModel(repository);
            await viewModel.LoadAsync();

            var window = new MainWindow
            {
                DataContext = viewModel
            };

            MainWindow = window;
            window.Show();
        }
        catch (Exception exception)
        {
            MessageBox.Show(exception.Message, "Startup error", MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown(-1);
        }
    }
}
