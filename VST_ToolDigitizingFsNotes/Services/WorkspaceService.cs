using VST_ToolDigitizingFsNotes.Libs.Services;

namespace VST_ToolDigitizingFsNotes.AppMain.Services;

public class WorkspaceService : IWorkspaceService
{
    public string GenerateName()
    {
        // using pattern {short guid}-{datetime}
        return $"{Guid.NewGuid().ToString()[..8]}-{DateTime.Now:yyyyMMdd-HHmmss}";
    }
}