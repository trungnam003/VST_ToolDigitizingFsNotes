using VST_ToolDigitizingFsNotes.Libs.Common;

namespace VST_ToolDigitizingFsNotes.AppMain.Extensions
{
    internal static class UserSettingsExtension
    {
        public static void SaveSettings(this UserSettings userSettings)
        {
            Properties.Settings.Default.WorkspaceFolderPath = userSettings.WorkspaceFolderPath;
            Properties.Settings.Default.Abbyy11Path = userSettings.Abbyy11Path;
            Properties.Settings.Default.Abbyy14Path = userSettings.Abbyy14Path;
            Properties.Settings.Default.Abbyy15Path = userSettings.Abbyy15Path;
            Properties.Settings.Default.FileMappingPath = userSettings.FileMappingPath;
            Properties.Settings.Default.Save();
        }
    }
}
