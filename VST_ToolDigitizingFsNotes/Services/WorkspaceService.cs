using Newtonsoft.Json;
using System.IO;
using System.Text.RegularExpressions;
using VST_ToolDigitizingFsNotes.Libs.Common;
using VST_ToolDigitizingFsNotes.Libs.Common.Enums;
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

    public async Task<bool> SaveWorkspace(WorkspaceMetadata workspaceMetadata, WorkspaceModel model)
    {
        var json = JsonConvert.SerializeObject(model, Formatting.Indented);
        var path = Path.Combine(workspaceMetadata.Path, "workspace.json");
        await File.WriteAllTextAsync(path, json);
        return true;
    }

    private static readonly string prefixWorkspaceName = "SoHoa";

    public List<FsNoteParentModel> CombineDataUnitOfWorks(SheetFsNoteModel sheet)
    {
        var type = sheet.GetType();
        var allUow = sheet.AllAbbyyUow.ToList();

        if (allUow.Count == 0)
            throw new ArgumentException("All UowAbbyy is null");

        var firstUow = allUow[0] ?? throw new ArgumentException("First UowAbbyy is null");
       

        var listHandle = allUow.Select(x => x!.FsNoteParentModels).ToList();

        if (listHandle == null || listHandle.Count == 0)
            throw new ArgumentException("All UowAbbyy.FsNoteParentModels is null");

        var results = HandleCombineData(listHandle);

        results.ForEach(x =>
        {
            x.Value *= firstUow.CalculationUnit.ToInt32();
            x.Values = x.Values.Select(v => v * firstUow.CalculationUnit.ToInt32()).ToList();
            x.Children.ForEach(c =>
            {
                c.Value *= firstUow.CalculationUnit.ToInt32();
                c.Values = c.Values.Select(v => v * firstUow.CalculationUnit.ToInt32()).ToList();
            });
        });

        return results;
        //if (sheet.UowAbbyy14 == null && sheet.UowAbbyy15 == null && sheet.UowAbbyy11 == null)
        //    throw new ArgumentException("Both UowAbbyy14 and UowAbbyy15 are null");
        //var listHandle = new List<List<FsNoteParentModel>>();

        //if (sheet.UowAbbyy14 != null)
        //{
        //    listHandle.Add(sheet.UowAbbyy14.FsNoteParentModels);
        //}

        //if (sheet.UowAbbyy15 != null)
        //{
        //    listHandle.Add(sheet.UowAbbyy15.FsNoteParentModels);
        //}

        //if (sheet.UowAbbyy11 != null)
        //{
        //    listHandle.Add(sheet.UowAbbyy11.FsNoteParentModels);
        //}

        //var results = HandleCombineData(listHandle);
        //return results;
    }

    private static List<FsNoteParentModel> HandleCombineData(List<List<FsNoteParentModel>> listData)
    {
        var results = new List<FsNoteParentModel>();
        if(listData.Count == 1)
        {
            return listData[0];
        }
        // check all list have the same length
        if (listData.Any(x => x.Count != listData[0].Count))
        {
            throw new ArgumentException("Tất cả dữ liệu phải chung kích thước");
        }

        for (int i = 0; i < listData[0].Count; i++)
        {
            var parent = listData[0][i];
            var countParentData = listData[0][i].Children.Where(x=> x.Value != 0).Count();
            FsNoteParentModel maxData = parent;
            var maxCount = countParentData;
            for (int j = 1; j < listData.Count; j++)
            {
                var parentNext = listData[j][i];
                var countParentNextData = listData[j][i].Children.Where(x => x.Value != 0).Count();
                if (countParentNextData > maxCount)
                {
                    maxData = parentNext;
                    maxCount = countParentNextData;
                }
            }

            results.Add(maxData);
        }

        return results;
    }
    
}