namespace VST_ToolDigitizingFsNotes.Libs.Services
{
    public interface IWorkspaceService
    {
        string GenerateName();
        bool InitFolder(string workspaceName, out string pathOut);
    }
}
