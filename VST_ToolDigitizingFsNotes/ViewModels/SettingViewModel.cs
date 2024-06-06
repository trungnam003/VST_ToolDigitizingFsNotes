using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Ookii.Dialogs.Wpf;
using VST_ToolDigitizingFsNotes.AppMain.Extensions;
using VST_ToolDigitizingFsNotes.Libs.Common;

namespace VST_ToolDigitizingFsNotes.AppMain.ViewModels
{
    public partial class SettingViewModel : ObservableObject
    {
        [ObservableProperty] private string _abbyy11Path;
        partial void OnAbbyy11PathChanged(string value)
        {
            _userSettings.Abbyy11Path = value;
            _userSettings.SaveSettings();
        }

        [ObservableProperty] private string _abbyy14Path;
        partial void OnAbbyy14PathChanged(string value)
        {
            _userSettings.Abbyy14Path = value;
            _userSettings.SaveSettings();
        }

        [ObservableProperty] private string _abbyy15Path;
        partial void OnAbbyy15PathChanged(string value)
        {
            _userSettings.Abbyy15Path = value;
            _userSettings.SaveSettings();
        }

        [ObservableProperty] private string _workspaceFolderPath;
        partial void OnWorkspaceFolderPathChanged(string value)
        {
            _userSettings.WorkspaceFolderPath = value;
            _userSettings.SaveSettings();
        }

        [ObservableProperty] private string _fileMappingPath;
        partial void OnFileMappingPathChanged(string value)
        {
            _userSettings.FileMappingPath = value;
            _userSettings.SaveSettings();
        }

        private readonly UserSettings _userSettings;

        public SettingViewModel(UserSettings userSettings)
        {
            _userSettings = userSettings;
            // get settings from properties
            WorkspaceFolderPath = _userSettings.WorkspaceFolderPath ?? string.Empty;
            Abbyy11Path = _userSettings.Abbyy11Path ?? string.Empty;
            Abbyy14Path = _userSettings.Abbyy14Path ?? string.Empty;
            Abbyy15Path = _userSettings.Abbyy15Path ?? string.Empty;
            FileMappingPath = _userSettings.FileMappingPath ?? string.Empty;
        }
        [RelayCommand]
        private void SelectWorkspaceFolder()
        {
            var dialog = new VistaFolderBrowserDialog();
            if (dialog.ShowDialog() == true)
            {
                WorkspaceFolderPath = dialog.SelectedPath;
            }
        }

        [RelayCommand]
        private void SelectFileMappingPath()
        {
            var dialog = new VistaOpenFileDialog()
            {
                Filter = "Excel Files (*.xlsx)|*.xlsx",

            };
            if (dialog.ShowDialog() == true)
            {
                FileMappingPath = dialog.FileName;
            }
        }

        [RelayCommand]
        private void SelectFileAbbyy14Exe()
        {
            var dialog = new VistaOpenFileDialog()
            {
                Filter = "Exe Files (*.exe)|*.exe",

            };
            if (dialog.ShowDialog() == true)
            {
                Abbyy14Path = dialog.FileName;
            }
        }

        [RelayCommand]
        private void SelectFileAbbyy15Exe()
        {
            var dialog = new VistaOpenFileDialog()
            {
                Filter = "Exe Files (*.exe)|*.exe",

            };
            if (dialog.ShowDialog() == true)
            {
                Abbyy15Path = dialog.FileName;
            }
        }

        [RelayCommand]
        private void SelectFileAbbyy11Exe()
        {
            var dialog = new VistaOpenFileDialog()
            {
                Filter = "Exe Files (*.exe)|*.exe",

            };
            if (dialog.ShowDialog() == true)
            {
                Abbyy11Path = dialog.FileName;
            }
        }
    }
}
