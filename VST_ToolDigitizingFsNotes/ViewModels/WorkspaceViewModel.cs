using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using VST_ToolDigitizingFsNotes.Libs.Common;
using VST_ToolDigitizingFsNotes.Libs.Models;
using VST_ToolDigitizingFsNotes.Libs.Services;

namespace VST_ToolDigitizingFsNotes.AppMain.ViewModels;

public partial class WorkspaceViewModel : ObservableObject
{
    [JsonIgnore]
    private readonly IServiceProvider _serviceProvider;
    [JsonIgnore]
    private readonly IWorkspaceService _workspaceService;
    [JsonIgnore]
    private readonly HomeViewModel _homeViewModel;
    [JsonIgnore]
    private readonly UserSettings _userSettings;
    [JsonIgnore]
    public readonly WorkspaceMetadata workspaceMetadata;

    public WorkspaceViewModel(IServiceProvider serviceProvider)
    {
        using (var scope = serviceProvider.CreateScope())
        {
            _serviceProvider = scope.ServiceProvider;
            _workspaceService = _serviceProvider.GetRequiredService<IWorkspaceService>();
            _homeViewModel = _serviceProvider.GetRequiredService<HomeViewModel>();
            _userSettings = _serviceProvider.GetRequiredService<UserSettings>();
        }
        Name = _workspaceService.GenerateName();
        workspaceMetadata = new WorkspaceMetadata
        {
            Name = Name
        };
    }

    public WorkspaceViewModel()
    {

    }

    [ObservableProperty]
    [JsonProperty]

    private string _name;

    [ObservableProperty]
    [JsonProperty]

    private ObservableCollection<FileImportFsNoteModel> _fileImportFsNoteModels = [];

    [ObservableProperty]
    [JsonIgnore]
    private FileImportFsNoteModel? _selectedFileImport;

    [RelayCommand]
    private void SelectFileImport(FileImportFsNoteModel selected)
    {
    }
    [RelayCommand]
    private async Task Start()
    {
        try
        {
            var result = MessageBox.Show("Bạn có chắc chắn muốn thực hiện tác vụ này?", "Xác nhận", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.No)
            {
                return;
            }
            _homeViewModel.IsLoading = true;
            InitWorkspaceFolder();
            await Task.Delay(100);
            await HandleFileImportsAsync();

        }
        catch (Exception)
        {
            MessageBox.Show("Có lỗi xảy ra, vui lòng thử lại sau", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            _homeViewModel.IsLoading = false;
        }
    }

}


public partial class WorkspaceViewModel
{
    #region Methods
    private void InitWorkspaceFolder()
    {
        if (!_workspaceService.InitFolder(Name, out string pathOut))
        {
            throw new Exception("Tên thư mục đã tồn tại, vui lòng chọn tên khác");
        }
        workspaceMetadata.Path = pathOut;
    }

    private async Task HandleFileImportsAsync()
    {
        /// Lặp qua từng file
        foreach (var file in FileImportFsNoteModels)
        {
            /// Lặp qua từng sheet
            foreach (var key in file.FsNoteSheets)
            {
                try
                {
                    _homeViewModel.Status = $"Đang xử lý sheet {key.Key} trong file {file.Name}";
                    await HandleSheetAsync(key.Value);
                }
                catch (Exception)
                {
                    _homeViewModel.Status = $"Lỗi xử lý sheet {key.Key} trong file {file.Name}";
                    await Task.Delay(2000);
                    continue;
                }
            }
        }
    }

    private async Task HandleSheetAsync(SheetFsNoteModel sheet)
    {
        ArgumentNullException.ThrowIfNullOrEmpty(sheet.FileUrl, "FileUrl is null or empty");

        var sheetMetadata = sheet.Meta = new();
        var fileName = Path.GetFileName(sheet.FileUrl);

        sheetMetadata.FilePdfFsPath = Path.Combine(workspaceMetadata.PdfDownloadPath, fileName);
        sheetMetadata.FileOcrV11Path = Path.Combine(workspaceMetadata.OcrPath, Path.GetFileNameWithoutExtension(fileName) + "_V11.xlsx");
        sheetMetadata.FileOcrV14Path = Path.Combine(workspaceMetadata.OcrPath, Path.GetFileNameWithoutExtension(fileName) + "_V14.xlsx");
        sheetMetadata.FileOcrV15Path = Path.Combine(workspaceMetadata.OcrPath, Path.GetFileNameWithoutExtension(fileName) + "_V15.xlsx");

        using var client = new DownloadFileHttpClient();
        using var stream = await client.DownloadFileStreamAsync(sheet.FileUrl);

        using var fileStream = new FileStream(sheetMetadata.FilePdfFsPath, FileMode.Create, FileAccess.Write);
        await stream.CopyToAsync(fileStream);
        sheetMetadata.IsDownloaded = File.Exists(sheetMetadata.FilePdfFsPath);
        {
            /// Đóng stream và filestream để giải phóng cho phép các process ABBYY sử dụng file
            stream.Close();
            fileStream.Close();
            stream.Dispose();
            fileStream.Dispose();
            client.Dispose();
        }
        var tasks = new List<Task>();
        /// ABBYY 11
        //var abbyy11String = new AbbyyCmdString.Builder()
        //    .SetAbbyyPath(_userSettings.Abbyy11Path!)
        //    .SetInputPath(sheetMetadata.FilePdfFsPath)
        //    .SetOutputPath(sheetMetadata.FileOcrV11Path)
        //    .SetQuitOnDone(true)
        //    .UseVietnameseLanguge()
        //    .Build();
        //var p11 = new AbbyyCmdManager(abbyy11String).StartAbbyyProcess();
        //var t11 = p11.WaitForExitAsync();
        //tasks.Add(t11);
        /// ABBYY 14
        var abbyy14String = new AbbyyCmdString.Builder()
            .SetAbbyyPath(_userSettings.Abbyy14Path!)
            .SetInputPath(sheetMetadata.FilePdfFsPath)
            .SetOutputPath(sheetMetadata.FileOcrV14Path)
            .SetQuitOnDone(true)
            .UseVietnameseLanguge()
            .Build();
        var p14 = new AbbyyCmdManager(abbyy14String).StartAbbyyProcess();
        var t14 = p14.WaitForExitAsync();
        tasks.Add(t14);
        /// ABBYY 15
        var abbyy15String = new AbbyyCmdString.Builder()
            .SetAbbyyPath(_userSettings.Abbyy15Path!)
            .SetInputPath(sheetMetadata.FilePdfFsPath)
            .SetOutputPath(sheetMetadata.FileOcrV15Path)
            .SetQuitOnDone(true)
            .UseVietnameseLanguge()
            .Build();
        var p15 = new AbbyyCmdManager(abbyy15String).StartAbbyyProcess();
        var t15 = p15.WaitForExitAsync();
        tasks.Add(t15);

        _homeViewModel.Status = $"Đang OCR file {fileName} (11)(14)(15)";
        await Task.WhenAll(tasks);

        sheetMetadata.IsFileOcrV11Created = File.Exists(sheetMetadata.FileOcrV14Path);
        sheetMetadata.IsFileOcrV14Created = File.Exists(sheetMetadata.FileOcrV14Path);
        sheetMetadata.IsFileOcrV15Created = File.Exists(sheetMetadata.FileOcrV15Path);

        //try
        //{
        //    // json convert and ignore loop
        //    dynamic json = new
        //    {
        //        Name,
        //        FileImportFsNoteModels
        //    };
        //    var jsonStr = JsonConvert.SerializeObject(json, Formatting.Indented, new JsonSerializerSettings
        //    {
        //        ReferenceLoopHandling = ReferenceLoopHandling.Ignore
        //    });
        //}
        //catch (Exception ex)
        //{
        //    throw;
        //}
    }

    #endregion
}
