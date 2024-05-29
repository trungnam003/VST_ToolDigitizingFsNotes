using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VST_ToolDigitizingFsNotes.AppMain.Services;

namespace VST_ToolDigitizingFsNotes.AppMain.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {

        [ObservableProperty]
        private INavigateService _navigateService;

        public MainViewModel(INavigateService navigateService)
        {
            NavigateService = navigateService;
            NavigateService.NavigateTo<HomeViewModel>();
        }

        [RelayCommand]
        private void NavigateToHome()
        {
            NavigateService.NavigateTo<HomeViewModel>();
        }

        [RelayCommand]
        private void NavigateToSettings()
        {
            NavigateService.NavigateTo<SettingViewModel>();
        }

        [RelayCommand]
        private void NavigateToTest()
        {
            NavigateService.NavigateTo<TestMapDataViewModel>();
        }
    }
}
