using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Force.DeepCloner;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using NPOI.HSSF.UserModel;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using VST_ToolDigitizingFsNotes.AppMain.Services;
using VST_ToolDigitizingFsNotes.Libs.Common;
using VST_ToolDigitizingFsNotes.Libs.Common.Enums;
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
    private const int SecondPerPageDelay = 12;
    private readonly IServiceProvider _serviceProvider;
    private readonly IWorkspaceService _workspaceService;
    private readonly HomeViewModel _homeViewModel;
    private readonly UserSettings _userSettings;
    private readonly IMediator _mediator;
    private readonly IPdfService _pdfService;
    private readonly IDetectService _detectService;
    private readonly IMappingService _mappingService;
    public readonly WorkspaceMetadata workspaceMetadata;
    private readonly DataReaderSheetSetting _dataReaderSheetSetting;
    private readonly StockCodeFsNoteMapping _stockCodeFsNoteMapping;

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
        _dataReaderSheetSetting = _serviceProvider.GetRequiredService<DataReaderSheetSetting>();
        _stockCodeFsNoteMapping = _serviceProvider.GetRequiredService<StockCodeFsNoteMapping>();

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
            SelectedSheetName = string.Empty;
            var sheet = sheets.FirstOrDefault();
            if (!string.IsNullOrEmpty(sheet))
            {
                SelectedSheetName = sheet;
            }
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
        if (string.IsNullOrEmpty(value))
        {
            return;
        }
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
            //var result = MessageBox.Show("Bạn có chắc chắn muốn thực hiện tác vụ này?", "Xác nhận", MessageBoxButton.YesNo, MessageBoxImage.Question);
            //if (result == MessageBoxResult.No)
            //{
            //    return;
            //}
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

    [RelayCommand]
    private async Task ExportDataToFileAsync()
    {
        _homeViewModel.IsLoading = true;
        try
        {
            await Task.Delay(10);
            foreach (var file in FileImportFsNoteModels)
            {
                var fileName = Path.GetFileName(file.SourcePath);
                file.DestinationPath = Path.Combine(workspaceMetadata.OutputPath, fileName);
                File.Copy(file.SourcePath, file.DestinationPath, true);
            }

            foreach (var file in FileImportFsNoteModels)
            {
                if (string.IsNullOrEmpty(file.DestinationPath))
                {
                    continue;
                }
                await using var fs = new FileStream(file.DestinationPath, FileMode.Open, FileAccess.Read);
                var workbook = await Task.Run(() => new HSSFWorkbook(fs));

                LoadDataToFileImport(workbook, file.FsNoteSheets);
                EvaluateAllFormulaCellSheet(workbook);

                await using var fsW = new FileStream(file.DestinationPath, FileMode.Create, FileAccess.Write);
                workbook.Write(fsW);
                workbook.Dispose();
                await fs.DisposeAsync();
            }

            MessageBox.Show("Xuất file thành công", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show("Lỗi: " + ex.Message, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            _homeViewModel.IsLoading = false;
        }
    }

    private void LoadDataToFileImport(HSSFWorkbook workbook, Dictionary<string, SheetFsNoteModel> sheets)
    {
        foreach (var key in sheets.Keys)
        {
            var value = sheets[key];
            var sheet = workbook.GetSheet(key);
            if (sheet == null)
            {
                continue;
            }
            var data = value.RawDataImport;
            foreach (var parent in data)
            {
                var children = parent.Children;
                foreach (var child in children)
                {
                    if (child.Value == 0 || child.Values.Count == 0)
                    {
                        continue;
                    }
                    var cell = sheet.GetRow(child.Cell.Item1)?.GetCell(child.Cell.Item2);
                    if (cell == null)
                    {
                        continue;
                    }

                    if (child.Values.Count == 1)
                    {
                        cell.SetCellValue(child.Value);
                    }
                    else
                    {

                        //var sb = new StringBuilder();
                        //for (int i = 0; i < child.Values.Count; i++)
                        //{
                        //    var itemData = child.Values[i];
                        //    var pattern = $"{itemData} + ";
                        //    if (i == child.Values.Count - 1)
                        //    {
                        //        pattern = $"{itemData}";
                        //    }
                        //    sb.Append(pattern);
                        //}
                        //cell.SetCellFormula(sb.ToString());

                        // use sum excel formula
                        var formula = $"SUM({string.Join(',', child.Values)})";
                        cell.SetCellFormula(formula);
                    }
                }
                var colFormula = _dataReaderSheetSetting.ParentValueAddress.Col;
                var rowFormula = parent.Cell.Item1;
                var cellFormula = sheet.GetRow(rowFormula)?.GetCell(colFormula);

                var colValue = _dataReaderSheetSetting.ValueAddress.Col;
                var rowValue = parent.Cell.Item1;
                var cellValue = sheet.GetRow(rowValue)?.GetCell(colValue);

                if (cellFormula != null)
                {
                    IFormulaEvaluator formulaEvaluator = workbook.GetCreationHelper().CreateFormulaEvaluator();
                    formulaEvaluator.EvaluateFormulaCell(cellFormula);
                }

                if (cellValue != null)
                {
                    IFormulaEvaluator formulaEvaluator = workbook.GetCreationHelper().CreateFormulaEvaluator();
                    formulaEvaluator.EvaluateFormulaCell(cellValue);
                }

                var childrenFormula = parent.FormulaCells;
                foreach (var child in childrenFormula)
                {
                    var cell = sheet.GetRow(child.Cell.Item1)?.GetCell(child.Cell.Item2);
                    if (cell != null)
                    {
                        IFormulaEvaluator formulaEvaluator = workbook.GetCreationHelper().CreateFormulaEvaluator();
                        formulaEvaluator.EvaluateFormulaCell(cell);
                    }
                }
            }
        }
    }

    private void EvaluateAllFormulaCellSheet(HSSFWorkbook workbook)
    {
        var targetSheetName = "Tong hop";
        var sheet = workbook.GetSheet(targetSheetName);
        if(sheet == null)
        {
            return;
        }   
        IFormulaEvaluator formulaEvaluator = workbook.GetCreationHelper().CreateFormulaEvaluator();
        foreach (IRow row in sheet)
        {
            foreach (ICell cell in row)
            {
                if (cell.CellType == CellType.Formula)
                {
                    try
                    {
                        formulaEvaluator.EvaluateFormulaCell(cell);
                    }
                    catch (Exception)
                    {
                        continue;
                    }
                }
            }
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
                {
                    continue;
                }

                try
                {
                    _homeViewModel.Status = $"Đang xử lý sheet {key.Key} trong file {file.Name}";
                    await HandleSheetAsync(key.Value);
                }
                catch (Exception ex)
                {
                    _homeViewModel.Status = $"Lỗi xử lý sheet {key.Key} trong file {file.Name}";
                    Debug.WriteLine(ex.Message);
                    continue;
                }
            }
        }
    }

    public static int CaculatePageNumberSplit(int totalPages)
    {
        if (totalPages <= 0)
        {
            throw new ArgumentException("Total pages must be greater than zero.");
        }
        if (totalPages <= 40)
        {
            const double split = (double)1 / 3;
            return (int)(totalPages * split);
        }

        if (totalPages > 40)
        {
            const double split = (double)1 / 2;
            var r = (totalPages * split);
            return (int)r;
        }
        return 10;
    }

    private async Task<bool> LoadMappingByStockCode(string stockCode)
    {
        if (_stockCodeFsNoteMapping.ContainsKey(stockCode))
        {
            return true;
        }
        var mappingPath = _userSettings.FileMappingPath;
        var pathWithoutFile = Path.GetDirectoryName(mappingPath);
        var fileName = Path.GetFileNameWithoutExtension(mappingPath);
        var ext = Path.GetExtension(mappingPath);
        var mappingPathStockCode = Path.Combine(pathWithoutFile!, $"{fileName}_{stockCode}{ext}");

        if (!File.Exists(mappingPathStockCode))
        {
            return false;
        }

        return await _mappingService.LoadMappingWithStockCode(mappingPathStockCode, stockCode);
    }

    private async Task HandleSheetAsync(SheetFsNoteModel sheet)
    {
        ArgumentNullException.ThrowIfNullOrEmpty(sheet.FileUrl, "FileUrl is null or empty");

        await LoadMappingByStockCode(sheet.StockCode!);

        var fileName = Path.GetFileName(sheet.FileUrl);
        var sheetMetadata = sheet.Meta = new()
        {
            FilePdfFsPath = Path.Combine(workspaceMetadata.PdfDownloadPath, fileName),
            FileOcrV11Path = Path.Combine(workspaceMetadata.OcrPath, Path.GetFileNameWithoutExtension(fileName) + "_V11.xlsx"),
            FileOcrV14Path = Path.Combine(workspaceMetadata.OcrPath, Path.GetFileNameWithoutExtension(fileName) + "_V14.xlsx"),
            FileOcrV15Path = Path.Combine(workspaceMetadata.OcrPath, Path.GetFileNameWithoutExtension(fileName) + "_V15.xlsx"),
        };
        int remainPage = 0;
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
            bool splitResult = false;
            var caculatePageNumberSplit = CaculatePageNumberSplit(totalPage);
            splitResult = await _pdfService.SplitPdfAsync(sheetMetadata.FilePdfFsPath, caculatePageNumberSplit, totalPage);
            sheetMetadata.IsDownloaded = File.Exists(sheetMetadata.FilePdfFsPath) && splitResult;
            remainPage = totalPage - caculatePageNumberSplit;
        }

        var tasks = new List<Task>();

        var versions = SheetFsNoteModel.AbbyyVersionsEnable;

        foreach (var version in versions)
        {
            var path = sheetMetadata.GetPathByVersion(version)?.GetValue(sheetMetadata)?.ToString();
            var isCreatedProps = sheetMetadata.GetIsCreatedByVersion(version);
            var abbyyProcessPath = _userSettings.GetAbbyyProcessByVersion(version)?.GetValue(_userSettings)?.ToString();
            if (path == null || abbyyProcessPath == null || isCreatedProps == null)
            {
                continue;
            }

            if (File.Exists(path))
            {
                isCreatedProps?.SetValue(sheetMetadata, true);
            }
            else
            {
                var abbyyString = new AbbyyCmdString.Builder()
                    .SetAbbyyPath(abbyyProcessPath)
                    .SetInputPath(sheetMetadata.FilePdfFsPath)
                    .SetOutputPath(path)
                    .SetQuitOnDone(true)
                    .UseVietnameseLanguge()
                    .Build();
                var p = new AbbyyCmdManager(abbyyString).StartAbbyyProcess();
                var t = p.WaitForExitAsync();
                tasks.Add(t);
            }
        }

        //if (File.Exists(sheetMetadata.FileOcrV11Path))
        //{
        //    sheetMetadata.IsFileOcrV11Created = true;
        //}
        //else
        //{
        //    /// ABBYY 11
        //    var abbyy11String = new AbbyyCmdString.Builder()
        //        .SetAbbyyPath(_userSettings.Abbyy11Path!)
        //        .SetInputPath(sheetMetadata.FilePdfFsPath)
        //        .SetOutputPath(sheetMetadata.FileOcrV11Path)
        //        .SetQuitOnDone(true)
        //        .UseVietnameseLanguge()
        //        .Build();
        //    var p11 = new AbbyyCmdManager(abbyy11String).StartAbbyyProcess();
        //    var t11 = p11.WaitForExitAsync();
        //    tasks.Add(t11);
        //}

        //if (File.Exists(sheetMetadata.FileOcrV14Path))
        //{
        //    sheetMetadata.IsFileOcrV14Created = true;
        //}
        //else
        //{
        //    /// ABBYY 14
        //    var abbyy14String = new AbbyyCmdString.Builder()
        //        .SetAbbyyPath(_userSettings.Abbyy14Path!)
        //        .SetInputPath(sheetMetadata.FilePdfFsPath)
        //        .SetOutputPath(sheetMetadata.FileOcrV14Path)
        //        .SetQuitOnDone(true)
        //        .UseVietnameseLanguge()
        //        .Build();
        //    var p14 = new AbbyyCmdManager(abbyy14String).StartAbbyyProcess();
        //    var t14 = p14.WaitForExitAsync();
        //    tasks.Add(t14);
        //}

        //if (File.Exists(sheetMetadata.FileOcrV15Path))
        //{
        //    sheetMetadata.IsFileOcrV15Created = true;
        //}
        //else
        //{
        //    /// ABBYY 15
        //    var abbyy15String = new AbbyyCmdString.Builder()
        //        .SetAbbyyPath(_userSettings.Abbyy15Path!)
        //        .SetInputPath(sheetMetadata.FilePdfFsPath)
        //        .SetOutputPath(sheetMetadata.FileOcrV15Path)
        //        .SetQuitOnDone(true)
        //        .UseVietnameseLanguge()
        //        .Build();
        //    var p15 = new AbbyyCmdManager(abbyy15String).StartAbbyyProcess();
        //    var t15 = p15.WaitForExitAsync();
        //    tasks.Add(t15);
        //}

        if (tasks.Count > 0)
        {
            var tokenTimeout = TimeSpan.FromSeconds(remainPage * SecondPerPageDelay) + TimeSpan.FromMinutes(3);
            var cts = new CancellationTokenSource();
            cts.CancelAfter(tokenTimeout);
            tasks.Add(InspectAllAbbyySuccess(sheet, versions, cts.Token));

            _homeViewModel.Status = $"Đang OCR file {fileName} (11)(14)(15)";
            await Task.WhenAll(tasks);

            //sheetMetadata.IsFileOcrV11Created = File.Exists(sheetMetadata.FileOcrV11Path);
            //sheetMetadata.IsFileOcrV14Created = File.Exists(sheetMetadata.FileOcrV14Path);
            //sheetMetadata.IsFileOcrV15Created = File.Exists(sheetMetadata.FileOcrV15Path);

            foreach (var version in versions)
            {
                var path = sheetMetadata.GetPathByVersion(version)?.GetValue(sheetMetadata)?.ToString();
                if (File.Exists(path))
                {
                    sheetMetadata.GetIsCreatedByVersion(version)?.SetValue(sheetMetadata, true);
                }
            }

        }
        await HandleMultiTaskAsync(sheet);

        // Dispose tất cả UowAbbyy
        sheet.AllAbbyyUow.ToList().ForEach(x => x?.Dispose());

        _homeViewModel.Status = $"Hoàn tất {fileName}";
    }

    public static async Task InspectAllAbbyySuccess(SheetFsNoteModel sheet, List<string> versions, CancellationToken cancellation = default)
    {
        // exit if cancel
        try
        {
            cancellation.ThrowIfCancellationRequested();
            if (sheet.Meta == null)
            {
                throw new Exception("Sheet metadata is null");
            }
            var sheetMetadata = sheet.Meta;

            while (true)
            {
                cancellation.ThrowIfCancellationRequested();
                var allVersionSucess = true;
                var messgae = "";
                foreach (var version in versions)
                {

                    var path = sheetMetadata.GetPathByVersion(version)?.GetValue(sheetMetadata)?.ToString();
                    messgae += $"{version} - {File.Exists(path)}";
                    if (!File.Exists(path))
                    {
                        allVersionSucess = false;
                        break;
                    }
                }
                Debug.WriteLine(messgae);
                if (allVersionSucess)
                {
                    Debug.WriteLine("Kết thúc");
                    AbbyyService.ExitAllAbbyy();
                    break;
                }
                Debug.WriteLine("Chưa nữa nè");
                await Task.Delay(3000, cancellation);
            }
        }
        catch (OperationCanceledException)
        {
            Debug.WriteLine("Task bị hủy do hết hạn");
            throw;
        }
        catch (Exception)
        {
            Debug.WriteLine("Có lỗi xảy ra khi kiểm tra tất cả các tiến trình ABBYY");
            throw;
        }

    }

    public async Task HandleMultiTaskAsync(SheetFsNoteModel sheet)
    {
        var metadata = sheet.Meta ?? throw new Exception("Sheet metadata is null");

        //var versions = new[]
        //{
        //    new { IsCreated = metadata.IsFileOcrV15Created, Path = metadata.FileOcrV15Path, PropertyName = nameof(sheet.UowAbbyy15), Version = "V15" },
        //    new { IsCreated = metadata.IsFileOcrV14Created, Path = metadata.FileOcrV14Path, PropertyName = nameof(sheet.UowAbbyy14), Version = "V14" },
        //    new { IsCreated = metadata.IsFileOcrV11Created, Path = metadata.FileOcrV11Path, PropertyName = nameof(sheet.UowAbbyy11), Version = "V11" }
        //};
        var sheetData = sheet.Meta;
        var versions = SheetFsNoteModel.AbbyyVersionsEnable.Select(v =>
        {
            var isCreated = metadata.GetIsCreatedByVersion(v)?.GetValue(metadata) as bool? ?? false;
            var path = metadata.GetPathByVersion(v)?.GetValue(metadata)?.ToString();
            return new { IsCreated = isCreated, Path = path, PropertyName = sheet.GetType().GetProperty($"UowAbbyy{v}")?.Name, Version = v };
        }).ToList();

        var tasks = new List<Task>();
        foreach (var version in versions)
        {
            if (!version.IsCreated || version.Path == null)
            {
                //throw new Exception($"File Ocr {version.Version} is not created");
                continue;
            }

            var property = sheet.GetType().GetProperty(version.PropertyName ?? "") ?? throw new Exception($"Property {version.PropertyName} is not found");

            var uow = new UnitOfWorkModel()
            {
                StockCode = sheet.StockCode!
            };
            uow.FsNoteParentModels.Clear();
            uow.FsNoteParentModels.AddRange(sheet.RawDataImport.DeepClone());
            property.SetValue(sheet, uow);

            var task = HandleSingleAsync(version.Path, uow, version.Version);
            await task;
            //tasks.Add(task);
        }

        var startWatch = Stopwatch.StartNew();

        //await Task.WhenAll(tasks);

        var dict = sheet.Data.Where(x => !x.IsParent).ToDictionary(x => x.Id, x => x);
        var finalData = _workspaceService.CombineDataUnitOfWorks(sheet);

        foreach (var parent in finalData)
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

        sheet.RawDataImport.Clear();
        sheet.RawDataImport.AddRange(finalData);

        startWatch.Stop();
        Debug.WriteLine($"(1) Time elapsed: {startWatch.ElapsedMilliseconds} ms");
    }

    public async Task HandleSingleAsync(string ocrPath, UnitOfWorkModel uow, string v)
    {
        Debug.WriteLine($"{v}");
        await using var fsOcr = new FileStream(ocrPath, FileMode.Open, FileAccess.Read);
        var workbookOcr = await Task.Run(() => new XSSFWorkbook(fsOcr));
        uow.OcrWorkbook = workbookOcr;
        {
            fsOcr.Close();
            fsOcr.Dispose();
        }
        var reqDetectData = new DetectDataRequest(ref uow);
        var taskDetectData = await _mediator.Send(reqDetectData);
        if (!taskDetectData)
        {
            throw new Exception("Detect data failed");
        }

        var calculationUnit = SpecifyCalculationUnit(uow);
        uow.CalculationUnit = calculationUnit;

        foreach (var parent in uow.FsNoteParentModels)
        {
            parent.Value = Math.Truncate(parent.Value / calculationUnit.ToInt32());
        }

        await Task.Run(() => _detectService.StartDetectFsNotesAsync(uow));
        Debug.WriteLine($"Done {v}");
    }

    /// <summary>
    /// Xác định đơn vị tính của tài liệu
    /// </summary>
    /// <param name="uow"></param>
    /// <returns></returns>
    private static CalculationUnit SpecifyCalculationUnit(UnitOfWorkModel uow)
    {
        var moneys = uow.MoneyCellModels;
        var moneyValues = moneys.Select(x => x.Value).ToList();
        moneyValues.Sort((x, y) => x.CompareTo(y));
        var countOne = 0;
        var countThousand = 0;
        var countMillion = 0;
        foreach (var parent in uow.FsNoteParentModels)
        {
            var search = parent.Value;
            if (search == 0)
            {
                continue;
            }
            var searchOne = Math.Truncate(search / CalculationUnit.One.ToInt32());
            var searchThousand = Math.Truncate(search / CalculationUnit.Thousand.ToInt32());
            var searchMillion = Math.Truncate(search / CalculationUnit.Million.ToInt32());

            var indexOne = moneyValues.BinarySearch(searchOne);
            var indexThousand = moneyValues.BinarySearch(searchThousand);
            var indexMillion = moneyValues.BinarySearch(searchMillion);

            if (indexOne >= 0)
            {
                countOne++;
            }

            if (indexThousand >= 0)
            {
                countThousand++;
            }

            if (indexMillion >= 0)
            {
                countMillion++;
            }
        }
        List<int> lst = [countOne, countThousand, countMillion];
        var max = lst.Max();
        if (max == countOne)
        {
            return CalculationUnit.One;
        }
        if (max == countThousand)
        {
            return CalculationUnit.Thousand;
        }
        return CalculationUnit.Million;
    }
    #endregion
}
