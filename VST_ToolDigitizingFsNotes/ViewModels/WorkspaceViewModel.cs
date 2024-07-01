using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Force.DeepCloner;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using NPOI.XSSF.UserModel;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using VST_ToolDigitizingFsNotes.Libs.Common;
using VST_ToolDigitizingFsNotes.Libs.Handlers;
using VST_ToolDigitizingFsNotes.Libs.Models;
using VST_ToolDigitizingFsNotes.Libs.Services;

namespace VST_ToolDigitizingFsNotes.AppMain.ViewModels;

public enum WorkspaceInitStatus
{
    ReadFromJson,
    CreateNew
}

public partial class WorkspaceViewModel : ObservableObject
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IWorkspaceService _workspaceService;
    private readonly HomeViewModel _homeViewModel;
    private readonly UserSettings _userSettings;
    private readonly IMediator _mediator;
    private readonly IPdfService _pdfService;
    private readonly IDetectService _detectService;
    private readonly IMappingService _mappingService;
    public readonly WorkspaceMetadata workspaceMetadata;

    public WorkspaceViewModel(IServiceProvider serviceProvider, string? dir = null)
    {
        _serviceProvider = serviceProvider;
        _workspaceService = _serviceProvider.GetRequiredService<IWorkspaceService>();
        _homeViewModel = _serviceProvider.GetRequiredService<HomeViewModel>();
        _userSettings = _serviceProvider.GetRequiredService<UserSettings>();
        _mediator = _serviceProvider.GetRequiredService<IMediator>();
        _pdfService = _serviceProvider.GetRequiredService<IPdfService>();
        _detectService = _serviceProvider.GetRequiredService<IDetectService>();
        _mappingService = _serviceProvider.GetRequiredService<IMappingService>();
        Name = _workspaceService.GenerateName();
        workspaceMetadata = new WorkspaceMetadata
        {
            Name = Name,
            Path = dir ?? string.Empty
        };
    }

    public WorkspaceInitStatus WorkspaceInitStatus { get; set; } = WorkspaceInitStatus.CreateNew;

    [ObservableProperty]
    private string _name;

    [ObservableProperty]
    private ObservableCollection<FileImportFsNoteModel> _fileImportFsNoteModels = [];

    [ObservableProperty]
    private FileImportFsNoteModel? _selectedFileImport;

    partial void OnSelectedFileImportChanged(FileImportFsNoteModel? value)
    {
        if (value != null)
        {
            var sheets = value.FsNoteSheets.Select(x => x.Key).ToList() ?? [];
            Sheets = new ObservableCollection<string>(sheets);
            SelectedSheetName = sheets.FirstOrDefault() ?? string.Empty;
        }

    }

    [ObservableProperty]
    private ObservableCollection<SheetFsNoteDataModel>? _dataSelected;

    [ObservableProperty]
    private ObservableCollection<string>? _sheets;

    [ObservableProperty]
    private string _selectedSheetName = string.Empty;
    [ObservableProperty]
    private string _selectedSheetInfomation = string.Empty;


    partial void OnSelectedSheetNameChanged(string value)
    {
        Task.Run(() => LoadDataAsync(value));
    }

    private Task LoadDataAsync(string key)
    {
        if (!string.IsNullOrEmpty(key) && SelectedFileImport != null && SelectedFileImport.FsNoteSheets.TryGetValue(key, out SheetFsNoteModel? value))
        {
            SelectedSheetInfomation = value.Information;
            DataSelected = [.. value.Data];
        }
        return Task.CompletedTask;
    }

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
            await _mappingService.LoadMapping2();
            if (WorkspaceInitStatus == WorkspaceInitStatus.CreateNew)
            {
                InitWorkspaceFolder();
                var model = new WorkspaceModel()
                {
                    Name = Name,
                    FileImports = FileImportFsNoteModels.Select(x => x).ToList()
                };
                await _workspaceService.SaveWorkspace(workspaceMetadata, model);
            }
            else
            {
                workspaceMetadata.Name = Name;
            }
            await Task.Delay(100);
            await HandleFileImportsAsync();

        }
        catch (Exception ex)
        {
            MessageBox.Show($"Có lỗi xảy ra, {ex.Message}", "Đây là lỗi nhé!", MessageBoxButton.OK, MessageBoxImage.Error);
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
                if (!string.IsNullOrEmpty(key.Value.ErrorMessage))
                    continue;
                try
                {
                    _homeViewModel.Status = $"Đang xử lý sheet {key.Key} trong file {file.Name}";
                    await HandleSheetAsync(key.Value);
                }
                catch (Exception ex)
                {
                    _homeViewModel.Status = $"Lỗi xử lý sheet {key.Key} trong file {file.Name}";
                    await Task.Delay(200);
                    Debug.WriteLine(ex.Message);
                    continue;
                }
            }
        }
    }

    private async Task HandleSheetAsync(SheetFsNoteModel sheet)
    {
        ArgumentNullException.ThrowIfNullOrEmpty(sheet.FileUrl, "FileUrl is null or empty");

        var fileName = Path.GetFileName(sheet.FileUrl);
        var sheetMetadata = sheet.Meta = new()
        {
            FilePdfFsPath = Path.Combine(workspaceMetadata.PdfDownloadPath, fileName),
            FileOcrV11Path = Path.Combine(workspaceMetadata.OcrPath, Path.GetFileNameWithoutExtension(fileName) + "_V11.xlsx"),
            FileOcrV14Path = Path.Combine(workspaceMetadata.OcrPath, Path.GetFileNameWithoutExtension(fileName) + "_V14.xlsx"),
            FileOcrV15Path = Path.Combine(workspaceMetadata.OcrPath, Path.GetFileNameWithoutExtension(fileName) + "_V15.xlsx"),
        };

        if (File.Exists(sheetMetadata.FilePdfFsPath))
        {
            sheetMetadata.IsDownloaded = true;
        }
        else
        {
            using var client = new DownloadFileHttpClient();
            using var stream = await client.DownloadFileStreamAsync(sheet.FileUrl);

            using var fileStream = new FileStream(sheetMetadata.FilePdfFsPath, FileMode.Create, FileAccess.Write);
            await stream.CopyToAsync(fileStream);
            {
                /// Đóng stream và filestream để giải phóng cho phép các process ABBYY sử dụng file
                stream.Close();
                fileStream.Close();
                stream.Dispose();
                fileStream.Dispose();
                client.Dispose();
            }
            var totalPage = await _pdfService.GetPdfPageCountAsync(sheetMetadata.FilePdfFsPath);
            var splitResult = await _pdfService.SplitPdfAsync(sheetMetadata.FilePdfFsPath, 30, totalPage);
            sheetMetadata.IsDownloaded = File.Exists(sheetMetadata.FilePdfFsPath) && splitResult;
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

        if (File.Exists(sheetMetadata.FileOcrV14Path))
        {
            sheetMetadata.IsFileOcrV14Created = true;
        }
        else
        {
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
        }

        if (File.Exists(sheetMetadata.FileOcrV15Path))
        {
            sheetMetadata.IsFileOcrV15Created = true;
        }
        else
        {
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
        }

        if (tasks.Count > 0)
        {
            _homeViewModel.Status = $"Đang OCR file {fileName} (11)(14)(15)";
            await Task.WhenAll(tasks);
            //sheetMetadata.IsFileOcrV11Created = File.Exists(sheetMetadata.FileOcrV14Path);
            sheetMetadata.IsFileOcrV14Created = File.Exists(sheetMetadata.FileOcrV14Path);
            sheetMetadata.IsFileOcrV15Created = File.Exists(sheetMetadata.FileOcrV15Path);
        }
        await HandleMultiTaskAsync(sheet);

        sheet.UowAbbyy14?.Dispose();
        sheet.UowAbbyy15?.Dispose();
    }

    public async Task HandleMultiTaskAsync(SheetFsNoteModel sheet)
    {
        var metadata = sheet.Meta;
        if (metadata == null)
        {
            throw new Exception("Sheet metadata is null");
        }
        if (!metadata.IsFileOcrV15Created || metadata.FileOcrV15Path == null)
        {
            throw new Exception("File Ocr V15 is not created");
        }
        if (!metadata.IsFileOcrV14Created || metadata.FileOcrV14Path == null)
        {
            throw new Exception("File Ocr V15 is not created");
        }
        sheet.UowAbbyy15 = new UnitOfWorkModel();
        sheet.UowAbbyy15.FsNoteParentModels.Clear();
        sheet.UowAbbyy15.FsNoteParentModels.AddRange(sheet.RawDataImport.Select(x => x.DeepClone()));

        sheet.UowAbbyy14 = new UnitOfWorkModel();
        sheet.UowAbbyy14.FsNoteParentModels.Clear();
        sheet.UowAbbyy14.FsNoteParentModels.AddRange(sheet.RawDataImport.Select(x => x.DeepClone()));
        var startWatch = Stopwatch.StartNew();
        var tasks = new List<Task>();
        var t1 = HandleSingleAsync(metadata.FileOcrV15Path, sheet.UowAbbyy15);
        tasks.Add(t1);
        var t2 = HandleSingleAsync(metadata.FileOcrV14Path, sheet.UowAbbyy14);
        tasks.Add(t2);

        await Task.WhenAll(tasks);
        await t1;
        await t2;
        startWatch.Stop();

        var dict = sheet.Data.Where(x => !x.IsParent).ToDictionary(x => x.Id, x => x);

        foreach (var parent in sheet.UowAbbyy14.FsNoteParentModels)
        {
            foreach (var child in parent.Children)
            {
                if (dict.TryGetValue(child.FsNoteId, out var value))
                {
                    value.TotalValue = child.Value;
                    value.Values = child.Values;
                }
            }
        }

        Debug.WriteLine($"(1) Time elapsed: {startWatch.ElapsedMilliseconds} ms");

    }

    public async Task HandleSingleAsync(string ocrPath, UnitOfWorkModel uow)
    {

        await using var fsOcr = new FileStream(ocrPath, FileMode.Open, FileAccess.Read);
        var workbookOcr = await Task.Run(() => new XSSFWorkbook(fsOcr));
        uow.OcrWorkbook = workbookOcr;
        {
            fsOcr.Close();
            fsOcr.Dispose();
        }
        var reqDetectData = new DetectDataRequest(ref uow);
        var taskDetectData = await _mediator.Send(reqDetectData);
        _detectService.StartDetectFsNotesAsync(uow);

    }
    #endregion
}
