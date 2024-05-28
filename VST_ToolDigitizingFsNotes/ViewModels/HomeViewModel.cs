using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;
using NPOI.HSSF.UserModel;
using NPOI.SS.UserModel;
using Ookii.Dialogs.Wpf;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows;
using VST_ToolDigitizingFsNotes.Libs.Handlers;
using VST_ToolDigitizingFsNotes.Libs.Models;
using VST_ToolDigitizingFsNotes.Libs.Services;
using VST_ToolDigitizingFsNotes.Libs.Utils;

namespace VST_ToolDigitizingFsNotes.AppMain.ViewModels
{
    public partial class HomeViewModel : ObservableObject
    {
        private UnitOfWorkModel _unitOfWork = new();
        private readonly IMediator _mediator;
        private readonly IMappingService _mappingService;
        private readonly IServiceProvider _serviceProvider;

        public HomeViewModel(IMediator mediator, IMappingService mappingService, IServiceProvider serviceProvider)
        {
            _mediator = mediator;
            _mappingService = mappingService;
            _serviceProvider = serviceProvider;
        }


        private async Task LoadMapping()
        {
            IsLoading = true;
            try
            {
                await _mappingService.LoadMapping();
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
            var moneys = _unitOfWork.MoneyCellModels;
            var headings = _unitOfWork.HeadingCellModels;
            try
            {
                var fileName = "VSM_Baocaotaichinh_Q3_2022_Hopnhat_14_2.xlsx";
                var fullPath = Path.Combine(Properties.Settings.Default.WorkspaceFolderPath, fileName);
                IsLoading = true;

                var request = new DetectDataRequest(fullPath, ref _unitOfWork);
                var t1 = _mediator.Send(request);

                var fileName2 = "1_NHAP_TM_CTCP_1321_VSM_VSM_VTB_VTB.xls";
                var fullPath2 = Path.Combine(Properties.Settings.Default.WorkspaceFolderPath, fileName2);

                var request2 = new LoadReferenceFsNoteDataRequest(fullPath2, "TM1", ref _unitOfWork);
                var t2 = _mediator.Send(request2);

                await Task.WhenAll(t1, t2);
                await t1;
                await t2;

                await LoadMapping();

                //print to debug money and heading list
                Debug.WriteLine($"{string.Join("\n", moneys)}");
                Debug.WriteLine("\n\n");
                Debug.WriteLine($"{string.Join("\n", headings)}");
            }
            catch (Exception)
            {
                throw;
            }
            finally
            {
                IsLoading = false;
            }
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
        private void LoadWorkspace()
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
                    MessageBox.Show("OK");
                }
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
                WorkspaceViewModel.FileImportFsNoteModels = [];

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
                        LoadDataFromImportWorkbook(workbook, ref fileImport);
                        fileImports.Add(fileImport);
                    }
                    catch (Exception ex)
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
        private static void LoadDataFromImportWorkbook(HSSFWorkbook workbook, ref FileImportFsNoteModel fileImports)
        {
            var sheetRegex = new Regex(@"^TM[1-6]$");
            fileImports.FsNoteSheets = new();
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
                    LoadDataFromSheet(sheet, ref sheetModel);
                    //fileImports.FsNoteSheets.Add(sheetName, sheetModel);
                }
                catch (Exception ex)
                {
                    sheetModel.ErrorMessage = ex.Message;
                    //continue;
                }
                finally
                {
                    fileImports.FsNoteSheets.Add(sheetName, sheetModel);
                }

            }
        }

        /// <summary>
        /// Đọc dữ liệu từ 1 sheet trong workbook theo cấu trúc file số hóa
        /// </summary>
        /// <param name="sheet"></param>
        /// <param name="sheetFsNoteModel"></param>
        private static void LoadDataFromSheet(ISheet sheet, ref SheetFsNoteModel sheetFsNoteModel)
        {
            LoadSheetInfo(sheet, ref sheetFsNoteModel);

            const int COL_NOTE_ID = 3;
            const int COL_CHECK_PARENT_NOTE = 2;
            const int COL_NOTE_NAME = 4;
            const int COL_NOTE_PARENT_VALUE = 6;
            const int START_ROW_INDEX = 14;
            const int COL_NOTE_VALUE = 5;
            sheetFsNoteModel.Data = new();
            int currentParentId = 0;
            for (int i = START_ROW_INDEX; i <= sheet.LastRowNum; i++)
            {
                try
                {
                    var row = sheet.GetRow(i);
                    if (row == null) continue;
                    if (!IsValidCell(row, COL_NOTE_NAME, COL_NOTE_ID))
                    {
                        continue;
                    }
                    int noteId = (int)row.GetCell(COL_NOTE_ID).NumericCellValue;
                    string name = row.GetCell(COL_NOTE_NAME).ToString()!;

                    var cellCheckParent = row.GetCell(COL_CHECK_PARENT_NOTE);

                    if (cellCheckParent == null)
                    {
                        continue;
                    }
                    var model = new SheetFsNoteDataModel
                    {
                        Id = noteId,
                        Name = name,
                        TotalValue = 0,
                        Values = []
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
        }

        private static bool IsValidCell(IRow row, int colName, int colNoteId)
        {
            if (row == null)
            {
                return false;
            }

            string? name = row.GetCell(colName).ToString();
            // kiểm tra nếu ô màu đỏ thì bỏ qua
            var cellColor = row.GetCell(colName).CellStyle.FillForegroundColorColor;
            Color colorTarget = Color.FromArgb(cellColor.RGB[0], cellColor.RGB[1], cellColor.RGB[2]);

            bool validName = !string.IsNullOrEmpty(name);
            bool validNoteId = int.TryParse(row.GetCell(colNoteId).ToString(), out int noteId) && noteId != 0;
            bool validColor = !CoreUtils.IsColorInRangeRed(colorTarget);

            return validName && validNoteId && validColor;
        }

        /// <summary>
        /// Đọc thông tin sheet MCK, Kỳ báo cáo, Năm, Trạng thái kiểm toán, Loại báo cáo, Đơn vị
        /// </summary>
        /// <param name="sheet"></param>
        /// <param name="sheetFsNoteModel"></param>
        private static void LoadSheetInfo(ISheet sheet, ref SheetFsNoteModel sheetFsNoteModel)
        {
            sheetFsNoteModel.StockCode =
                sheet.GetRow(SheetFsNoteModel.MetaData.StockRowIndex)
                .GetCell(SheetFsNoteModel.MetaData.MetaDataColIndex)?.StringCellValue ?? string.Empty;

            sheetFsNoteModel.ReportTerm =
                sheet.GetRow(SheetFsNoteModel.MetaData.ReportTermRowIndex)
                .GetCell(SheetFsNoteModel.MetaData.MetaDataColIndex)?.StringCellValue ?? string.Empty;

            sheetFsNoteModel.Year = (int)sheet.GetRow(SheetFsNoteModel.MetaData.YearRowIndex)
                .GetCell(SheetFsNoteModel.MetaData.MetaDataColIndex)?.NumericCellValue;

            sheetFsNoteModel.AuditedStatus =
                sheet.GetRow(SheetFsNoteModel.MetaData.AuditedStatusRowIndex)
                .GetCell(SheetFsNoteModel.MetaData.MetaDataColIndex)?.StringCellValue ?? string.Empty;

            sheetFsNoteModel.ReportType =
                sheet.GetRow(SheetFsNoteModel.MetaData.ReportTypeRowIndex)
                .GetCell(SheetFsNoteModel.MetaData.MetaDataColIndex)?.StringCellValue ?? string.Empty;

            sheetFsNoteModel.Unit =
                sheet.GetRow(SheetFsNoteModel.MetaData.UnitRowIndex)
                .GetCell(SheetFsNoteModel.MetaData.MetaDataColIndex)?.StringCellValue ?? string.Empty;

            var fileUrlCell = sheet.GetRow(SheetFsNoteModel.MetaData.FileUrlRowIndex)
                .GetCell(SheetFsNoteModel.MetaData.FileUrlColIndex);
            
            if(fileUrlCell?.Hyperlink != null)
            {
                sheetFsNoteModel.FileUrl = fileUrlCell.Hyperlink.Address;
            }
            else
            {
                sheetFsNoteModel.FileUrl = fileUrlCell?.StringCellValue ?? string.Empty;
            }
               
        }
    }
}
