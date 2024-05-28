using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using System.Windows;
using VST_ToolDigitizingFsNotes.AppMain.Services;
using VST_ToolDigitizingFsNotes.AppMain.ViewModels;
using VST_ToolDigitizingFsNotes.AppMain.Views;
using VST_ToolDigitizingFsNotes.Libs;
using VST_ToolDigitizingFsNotes.Libs.Common;
using VST_ToolDigitizingFsNotes.Libs.Models;
using VST_ToolDigitizingFsNotes.Libs.Services;
using Application = System.Windows.Application;
using GlobalProperties = VST_ToolDigitizingFsNotes.AppMain.Properties;

namespace VST_ToolDigitizingFsNotes.AppMain
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>

    public partial class App : Application
    {
        private readonly IHost _host;
        public App()
        {
            _host = Host.CreateDefaultBuilder()
                //.AddAppSerilog()
                .ConfigureServices((_, services) =>
                {
                    AddAppServices(services);
                })
                .Build();
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            if (_host.Services == null)
                return;
            using var scope = _host.Services.CreateScope();
            var serviceProvider = scope.ServiceProvider;
            var mainWindow = serviceProvider.GetRequiredService<MainWindow>();
            mainWindow.Show();
        }

        private static void AddAppServices(IServiceCollection services)
        {
            {
                services.AddSingleton(provider => new MainWindow()
                {
                    DataContext = provider.GetRequiredService<MainViewModel>()
                });
            }

            {
                services.AddSingleton<HomeViewModel>();
                services.AddSingleton<MainViewModel>();
                services.AddSingleton<SettingViewModel>();

                services.AddSingleton<IDetectService, DetectService>();
                services.AddSingleton<IMappingService, MappingService>();
                services.AddSingleton<IWorkspaceService, WorkspaceService>();
            }

            {
                services.AddSingleton<INavigateService, NavigateService>();
                services.AddSingleton<Func<Type, ObservableObject>>(provider => type => (ObservableObject)provider.GetRequiredService(type));
            }

            {
                services.AddSingleton<UserSettings>(provider =>
                {
                    return new()
                    {
                        Abbyy11Path = GlobalProperties.Settings.Default.Abbyy11Path,
                        Abbyy14Path = GlobalProperties.Settings.Default.Abbyy14Path,
                        Abbyy15Path = GlobalProperties.Settings.Default.Abbyy15Path,
                        WorkspaceFolderPath = GlobalProperties.Settings.Default.WorkspaceFolderPath,
                        FileMappingPath = GlobalProperties.Settings.Default.FileMappingPath
                    };
                });

                services.AddSingleton<Dictionary<int, FsNoteParentMappingModel>>(provider => []);
            }

            {
                services.AddCoreServices();
            }
        }


        private async void Application_Startup(object sender, StartupEventArgs e)
        {
            await _host.StartAsync();
        }

        private async void Application_Exit(object sender, ExitEventArgs e)
        {
            using (_host)
            {
                await _host.StopAsync(TimeSpan.FromSeconds(1));
            }
        }
    }

    public static class AppExtensions
    {
        [Obsolete("Chưa dùng đến")]
        public static IHostBuilder AddAppSerilog(this IHostBuilder hostBuilder)
        {
            hostBuilder.UseSerilog((hostingContext, loggerConfiguration) =>
            {
                loggerConfiguration
                    .ReadFrom.Configuration(hostingContext.Configuration);
                //.WriteTo.RichTextBox();
            });
            return hostBuilder;
        }
    }

}
