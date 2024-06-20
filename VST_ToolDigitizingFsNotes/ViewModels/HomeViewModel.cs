using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;
using Newtonsoft.Json;
using NPOI.HPSF;
using NPOI.HSSF.UserModel;
using NPOI.SS.UserModel;
using Ookii.Dialogs.Wpf;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows;
using VST_ToolDigitizingFsNotes.Libs.Common;
using VST_ToolDigitizingFsNotes.Libs.Handlers;
using VST_ToolDigitizingFsNotes.Libs.Models;
using VST_ToolDigitizingFsNotes.Libs.Services;

namespace VST_ToolDigitizingFsNotes.AppMain.ViewModels
{
    public partial class HomeViewModel : ObservableObject
    {
        private readonly IMediator _mediator;
        private readonly IMappingService _mappingService;
        private readonly IServiceProvider _serviceProvider;
        private readonly DataReaderSheetSetting _dataReaderSheetSetting;
        private readonly MetaDataReaderSheetSetting _metaDataReaderSheetSetting;

        public HomeViewModel(
            IMediator mediator,
            IMappingService mappingService,
            IServiceProvider serviceProvider,
            DataReaderSheetSetting dataReaderSheetSetting,
            MetaDataReaderSheetSetting metaDataReaderSheetSetting)
        {
            _mediator = mediator;
            _mappingService = mappingService;
            _serviceProvider = serviceProvider;
            _dataReaderSheetSetting = dataReaderSheetSetting;
            _metaDataReaderSheetSetting = metaDataReaderSheetSetting;
        }


        private async Task LoadMapping()
        {
            IsLoading = true;
            try
            {
                await _mappingService.LoadMapping2();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        [ObservableProperty]
        private bool _isLoading = false;

        [ObservableProperty]
        private string _status = string.Empty;

        partial void OnIsLoadingChanged(bool value)
        {
            IsNoBlock = !value;
        }

        [ObservableProperty]
        private bool _isNoBlock = true;

        [ObservableProperty]
        private WorkspaceViewModel? _workspaceViewModel;

        [RelayCommand(CanExecute = nameof(IsNoBlock))]
        private async Task Test()
        {
            await LoadMapping();
            //var moneys = _unitOfWork.MoneyCellModels;
            //var headings = _unitOfWork.HeadingCellModels;
            //try
            //{
            //    var fileName = "VSM_Baocaotaichinh_Q3_2022_Hopnhat_14_2.xlsx";
            //    var fullPath = Path.Combine(Properties.Settings.Default.WorkspaceFolderPath, fileName);
            //    IsLoading = true;

            //    var request = new DetectDataRequest(fullPath, ref _unitOfWork);
            //    var t1 = _mediator.Send(request);

            //    var fileName2 = "1_NHAP_TM_CTCP_1321_VSM_VSM_VTB_VTB.xls";
            //    var fullPath2 = Path.Combine(Properties.Settings.Default.WorkspaceFolderPath, fileName2);

            //    var request2 = new LoadReferenceFsNoteDataRequest(fullPath2, "TM1", ref _unitOfWork);
            //    var t2 = _mediator.Send(request2);

            //    await Task.WhenAll(t1, t2);
            //    await t1;
            //    await t2;

            //    await LoadMapping();

            //    //print to debug money and heading list
            //    Debug.WriteLine($"{string.Join("\n", moneys)}");
            //    Debug.WriteLine("\n\n");
            //    Debug.WriteLine($"{string.Join("\n", headings)}");
            //}
            //catch (Exception)
            //{
            //    throw;
            //}
            //finally
            //{
            //    IsLoading = false;
            //}
        }


        [RelayCommand(CanExecute = nameof(IsNoBlock))]
        private async Task SelectFiles()
        {
            try
            {
                var dialog = new VistaOpenFileDialog
                {
                    Multiselect = true,
                    Filter = "Excel files (*.xls;*.xlsx)|*.xls;*.xlsx|All files (*.*)|*.*",
                    Title = "Chọn các file cần số hóa"
                };
                if (dialog.ShowDialog() == true)
                {
                    var files = dialog.FileNames;
                    IsLoading = true;
                    Status = "Đang khởi tạo dữ liệu từ file số hóa...";
                    await HandleReadAllExcelFiles(files);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            finally
            {
                Status = string.Empty;
                IsLoading = false;
            }
        }

        [RelayCommand]
        private async Task LoadWorkspace()
        {
            try
            {
                var dialog = new VistaOpenFileDialog
                {
                    // json file
                    Filter = "Workspace files (*.json)|*.json",
                    Title = "Chọn file workspace"
                };

                if (dialog.ShowDialog() == true)
                {
                    var model = JsonConvert.DeserializeObject<WorkspaceModel>(File.ReadAllText(dialog.FileName)) ?? throw new Exception("Không thể đọc file workspace");
                    IsLoading = true;
                    Status = "Đang khởi tạo dữ liệu từ file số hóa...";
                    // get directory of workspace file
                    var directory = Path.GetDirectoryName(dialog.FileName);
                    await HandleReadWorkspace(model, directory!);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            finally
            {
                Status = string.Empty;
                IsLoading = false;
            }
        }

        private async Task HandleReadWorkspace(WorkspaceModel model, string dir)
        {
            try
            {
                WorkspaceViewModel = new(_serviceProvider, dir)
                {
                    Name = model.Name,
                    WorkspaceInitStatus = WorkspaceInitStatus.ReadFromJson,
                };

                var mutex = new Mutex();
                var tasks = new List<Task>();

                var fileImports = WorkspaceViewModel.FileImportFsNoteModels;
                foreach(var item in model.FileImports)
                {
                    if (!CheckValidFileName(item.SourcePath))
                    {
                        continue;
                    }
                    tasks.Add(Task.Run(async () =>
                    {
                        var file = new FileInfo(item.SourcePath);
                        try
                        {
                            await using var fs = file.OpenRead();
                            var workbook = await Task.Run(() => new HSSFWorkbook(fs));
                            var fileImport = new FileImportFsNoteModel()
                            {
                                Name = file.Name,
                                SourcePath = file.FullName,
                            };
                            await LoadDataFromImportWorkbook(workbook, fileImport);

                            mutex.WaitOne();
                            try
                            {
                                fileImports.Add(fileImport);
                            }
                            finally
                            {
                                mutex.ReleaseMutex();
                            }
                        }
                        catch (Exception)
                        {
                            return;
                        }
                    }));
                }
                await Task.WhenAll(tasks);
                WorkspaceViewModel.FileImportFsNoteModels = [];
                foreach (var item in fileImports)
                {
                    WorkspaceViewModel.FileImportFsNoteModels.Add(item);
                }
                MessageBox.Show("Đã đọc xong tất cả các file", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private async Task HandleReadAllExcelFiles(string[] fileNames)
        {
            try
            {
                WorkspaceViewModel = new(_serviceProvider);

                var fileImports = WorkspaceViewModel.FileImportFsNoteModels;

                foreach (var fileName in fileNames)
                {
                    if (!CheckValidFileName(fileName))
                    {
                        continue;
                    }
                    var file = new FileInfo(fileName);
                    try
                    {
                        await using var fs = file.OpenRead();
                        var workbook = await Task.Run(() => new HSSFWorkbook(fs));
                        var fileImport = new FileImportFsNoteModel()
                        {
                            Name = file.Name,
                            SourcePath = file.FullName,
                        };
                        await LoadDataFromImportWorkbook(workbook, fileImport);
                        fileImports.Add(fileImport);
                    }
                    catch (Exception)
                    {
                        continue;
                    }
                }
                MessageBox.Show("Đã đọc xong tất cả các file", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }

        }

        private static bool CheckValidFileName(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
            {
                return false;
            }

            if (!File.Exists(fileName))
            {
                return false;
            }

            const string validExtension = ".xls|.xlsx";
            var extension = Path.GetExtension(fileName);
            if (!validExtension.Contains(extension))
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Đọc dữ liệu workbook theo cấu trúc file số hóa
        /// </summary>
        /// <param name="workbook"></param>
        /// <param name="fileImports"></param>
        private async Task LoadDataFromImportWorkbook(HSSFWorkbook workbook, FileImportFsNoteModel fileImport)
        {
            var sheetRegex = new Regex(@"^TM[1-6]$");
            fileImport.FsNoteSheets = [];
            // loop sheet in workbook and match with sheetRegex
            for (var i = 0; i < workbook.NumberOfSheets; i++)
            {
                var sheet = workbook.GetSheetAt(i);
                var sheetName = sheet.SheetName;
                if (!sheetRegex.IsMatch(sheetName))
                {
                    continue;
                }
                var sheetModel = new SheetFsNoteModel()
                {
                    SheetName = sheetName,
                };
                try
                {
                    await LoadDataFromSheet(sheet, workbook, sheetModel);
                    //fileImports.FsNoteSheets.Add(sheetName, sheetModel);
                }
                catch (Exception ex)
                {
                    sheetModel.ErrorMessage = ex.Message;
                    //continue;
                    fileImport.WarningMessage += sheetName + ";";
                }
                finally
                {
                    fileImport.FsNoteSheets.Add(sheetName, sheetModel);
                }

            }
        }

        /// <summary>
        /// Đọc dữ liệu từ 1 sheet trong workbook theo cấu trúc file số hóa
        /// </summary>
        /// <param name="sheet"></param>
        /// <param name="sheetFsNoteModel"></param>
        private async Task LoadDataFromSheet(ISheet sheet, HSSFWorkbook workbook, SheetFsNoteModel sheetFsNoteModel)
        {
            LoadSheetInfo(sheet, ref sheetFsNoteModel);

            sheetFsNoteModel.Data = [];
            int currentParentId = 0;
            for (int i = _dataReaderSheetSetting.CheckParentAddress.Row; i <= sheet.LastRowNum; i++)
            {
                try
                {
                    var row = sheet.GetRow(i);
                    if (row == null) continue;
                    if (!LoadReferenceFsNoteDataHandler.IsValidCell(row, _dataReaderSheetSetting.NameAddress.Col, _dataReaderSheetSetting.NoteIdAddress.Col))
                    {
                        continue;
                    }
                    int noteId = (int)row.GetCell(_dataReaderSheetSetting.NoteIdAddress.Col).NumericCellValue;
                    string name = row.GetCell(_dataReaderSheetSetting.NameAddress.Col).ToString()!;

                    var cellCheckParent = row.GetCell(_dataReaderSheetSetting.CheckParentAddress.Col);

                    if (cellCheckParent == null)
                    {
                        continue;
                    }
                    var model = new SheetFsNoteDataModel
                    {
                        Id = noteId,
                        Name = name,
                        TotalValue = 0,
                        Values = [],
                    };
                    if (!string.IsNullOrEmpty(cellCheckParent.ToString()))
                    {
                        currentParentId = noteId;
                        model.IsParent = true;
                    }
                    else
                    {
                        model.IsParent = false;
                    }
                    sheetFsNoteModel.Data.Add(model);
                }
                catch (Exception)
                {
                    throw;
                }
            }
            var rawData = sheetFsNoteModel.RawDataImport;
            var reqLoadInputData = new LoadReferenceFsNoteDataRequest(workbook, sheetFsNoteModel.SheetName!, ref rawData);
            await _mediator.Send(reqLoadInputData);
        }

        /// <summary>
        /// Đọc thông tin sheet MCK, Kỳ báo cáo, Năm, Trạng thái kiểm toán, Loại báo cáo, Đơn vị
        /// </summary>
        /// <param name="sheet"></param>
        /// <param name="sheetFsNoteModel"></param>
        private void LoadSheetInfo(ISheet sheet, ref SheetFsNoteModel sheetFsNoteModel)
        {
            sheetFsNoteModel.StockCode =
                sheet.GetRow(_metaDataReaderSheetSetting.StockCodeAddress.Row)
                .GetCell(_metaDataReaderSheetSetting.StockCodeAddress.Col)?.StringCellValue ?? string.Empty;

            sheetFsNoteModel.ReportTerm =
                sheet.GetRow(_metaDataReaderSheetSetting.ReportTermAddress.Row)
                .GetCell(_metaDataReaderSheetSetting.ReportTermAddress.Col)?.StringCellValue ?? string.Empty;

            sheetFsNoteModel.Year = (int)(sheet.GetRow(_metaDataReaderSheetSetting.YearAddress.Row)
                .GetCell(_metaDataReaderSheetSetting.YearAddress.Col)?.NumericCellValue ?? 0);

            sheetFsNoteModel.AuditedStatus =
                sheet.GetRow(_metaDataReaderSheetSetting.AuditedStatusAddress.Row)
                .GetCell(_metaDataReaderSheetSetting.AuditedStatusAddress.Col)?.StringCellValue ?? string.Empty;

            sheetFsNoteModel.ReportType =
                sheet.GetRow(_metaDataReaderSheetSetting.ReportTypeAddress.Row)
                .GetCell(_metaDataReaderSheetSetting.ReportTypeAddress.Col)?.StringCellValue ?? string.Empty;

            sheetFsNoteModel.Unit =
                sheet.GetRow(_metaDataReaderSheetSetting.UnitAddress.Row)
                .GetCell(_metaDataReaderSheetSetting.UnitAddress.Col)?.StringCellValue ?? string.Empty;

            var fileUrlCell = sheet.GetRow(_metaDataReaderSheetSetting.FileUrlAddress.Row)
                .GetCell(_metaDataReaderSheetSetting.FileUrlAddress.Col);

            if (fileUrlCell?.Hyperlink != null)
            {
                sheetFsNoteModel.FileUrl = fileUrlCell.Hyperlink.Address;
            }
            else
            {
                sheetFsNoteModel.FileUrl = fileUrlCell?.StringCellValue ?? string.Empty;
            }

            if (!ValidateSheetInfo(sheetFsNoteModel))
            {
                throw new Exception("Thông tin báo cáo không hợp lệ");
            }

        }

        private static bool ValidateSheetInfo(SheetFsNoteModel sheetFsNoteModel)
        {
            if (string.IsNullOrEmpty(sheetFsNoteModel.StockCode))
            {
                return false;
            }

            if (string.IsNullOrEmpty(sheetFsNoteModel.ReportTerm))
            {
                return false;
            }

            if (sheetFsNoteModel.Year == 0)
            {
                return false;
            }

            if (string.IsNullOrEmpty(sheetFsNoteModel.AuditedStatus))
            {
                return false;
            }

            if (string.IsNullOrEmpty(sheetFsNoteModel.ReportType))
            {
                return false;
            }

            if (string.IsNullOrEmpty(sheetFsNoteModel.Unit))
            {
                return false;
            }

            if (string.IsNullOrEmpty(sheetFsNoteModel.FileUrl))
            {
                return false;
            }

            return true;
        }

    }
}
