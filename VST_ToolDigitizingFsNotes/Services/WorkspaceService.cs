using System.IO;
using VST_ToolDigitizingFsNotes.Libs.Common;
using VST_ToolDigitizingFsNotes.Libs.Models;
using VST_ToolDigitizingFsNotes.Libs.Services;

namespace VST_ToolDigitizingFsNotes.AppMain.Services;

public class WorkspaceService : IWorkspaceService
{
    private readonly UserSettings _userSettings;
    public WorkspaceService(UserSettings userSettings)
    {
        _userSettings = userSettings;
    }
    public string GenerateName()
    {
        // using pattern {short guid}-{datetime}
        return $"{prefixWorkspaceName}_{DateTime.Now:yyyyMMdd_HHmmss}_{Guid.NewGuid().ToString()[..8]}";
    }

    public bool InitFolder(string workspaceName, out string pathOut)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(_userSettings.WorkspaceFolderPath, nameof(_userSettings.WorkspaceFolderPath));
        var destinationFolder = Path.Combine(_userSettings.WorkspaceFolderPath, workspaceName);

        if (Directory.Exists(destinationFolder))
        {
            pathOut = string.Empty;
            return false;
        }

        Directory.CreateDirectory(destinationFolder);
        Directory.CreateDirectory(Path.Combine(destinationFolder, WorkspaceMetadata.ocrFolderName));
        Directory.CreateDirectory(Path.Combine(destinationFolder, WorkspaceMetadata.outputFolderName));
        Directory.CreateDirectory(Path.Combine(destinationFolder, WorkspaceMetadata.pdfDownloadFolderName));
        Directory.CreateDirectory(Path.Combine(destinationFolder, WorkspaceMetadata.DataFolderName));

        pathOut = destinationFolder;

        return true;
    }



    private static readonly string prefixWorkspaceName = "SoHoa";
}