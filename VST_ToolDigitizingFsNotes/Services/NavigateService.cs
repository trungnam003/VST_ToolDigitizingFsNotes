using CommunityToolkit.Mvvm.ComponentModel;

namespace VST_ToolDigitizingFsNotes.AppMain.Services;

public interface INavigateService
{
    ObservableObject? CurrentViewModel { get; }
    void NavigateTo<T>() where T : ObservableObject;
}

public partial class NavigateService(Func<Type, ObservableObject> viewModelFactory) : ObservableObject, INavigateService
{
    [ObservableProperty]
    private ObservableObject? _currentViewModel;
    public void NavigateTo<T>() where T : ObservableObject
    {
        var vm = viewModelFactory(typeof(T));
        CurrentViewModel = vm;
    }
}