using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;
using NPOI.HSSF.UserModel;
using NPOI.XSSF.UserModel;
using Ookii.Dialogs.Wpf;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;
using VST_ToolDigitizingFsNotes.Libs.Handlers;
using VST_ToolDigitizingFsNotes.Libs.Models;
using VST_ToolDigitizingFsNotes.Libs.Services;

namespace VST_ToolDigitizingFsNotes.AppMain.ViewModels;

public partial class TestMapDataViewModel : ObservableObject
{
    #region Services

    private readonly IMediator _mediator;
    private readonly IDetectService _detectService;
    private readonly IMappingService _mappingService;

    #endregion

    [ObservableProperty] private string _fileInputPath = @"C:\\Users\\trungnamth\\Downloads\\sohoa_BCTC\\1_NHAP_TM_CTCP_1321_VSM_VSM_VTB_VTB.xls";
    [ObservableProperty] private string _fileOcr14Path = string.Empty;
    [ObservableProperty] private string _fileOcr15Path = @"D:\\TMBCTC_Workspace\\SoHoa_20240530_101731_58501019\\OCR\\VSM_Baocaotaichinh_Q3_2022_Hopnhat_V15.xlsx";

    public TestMapDataViewModel(IMediator mediator, IMappingService mappingService, IDetectService detectService)
    {
        _mediator = mediator;
        _mappingService = mappingService;
        _detectService = detectService;
    }
}

public partial class TestMapDataViewModel
{
    #region RelayCommands

    [RelayCommand]
    private void SelectFileInput()
    {
        var dialog = new VistaOpenFileDialog()
        {
            // xls, xlsx
            Filter = "Excel Files (*.xls, *.xlsx)|*.xls;*.xlsx",
        };
        if (dialog.ShowDialog() == true)
        {
            FileInputPath = dialog.FileName;
        }
    }

    [RelayCommand]
    private void SelectFileOcr14()
    {
        var dialog = new VistaOpenFileDialog()
        {
            Filter = "Excel Files (*.xlsx)|*.xlsx",
        };
        if (dialog.ShowDialog() == true)
        {
            FileOcr14Path = dialog.FileName;
        }
    }

    [RelayCommand]
    private void SelectFileOcr15()
    {
        var dialog = new VistaOpenFileDialog()
        {
            Filter = "Excel Files (*.xlsx)|*.xlsx",
        };
        if (dialog.ShowDialog() == true)
        {
            FileOcr15Path = dialog.FileName;
        }
    }

    [RelayCommand]
    private async Task RunAsync()
    {
        try
        {
            ArgumentNullException.ThrowIfNullOrWhiteSpace(FileOcr15Path, nameof(FileOcr15Path));
            ArgumentNullException.ThrowIfNullOrWhiteSpace(FileInputPath, nameof(FileInputPath));

            var watch = Stopwatch.StartNew();

            await using var fsFileInput = new FileStream(FileInputPath, FileMode.Open, FileAccess.Read);
            await using var fsFileOcr15 = new FileStream(FileOcr15Path, FileMode.Open, FileAccess.Read);
            var t1 = Task.Run(() => new HSSFWorkbook(fsFileInput));
            var t2 = Task.Run(() => new XSSFWorkbook(fsFileOcr15));
            var t3 = _mappingService.LoadMapping();

            await Task.WhenAll(t1, t2, t3);
            var workbookInput = await t1;
            var workbookOcr15 = await t2;
            await t3;

            fsFileInput.Close();
            fsFileOcr15.Close();

            using var uow = new UnitOfWorkModel()
            {
                OcrWorkbook = workbookOcr15
            };

            await HandleUnitOfWorkAsync(uow, workbookInput);

            watch.Stop();
            Debug.WriteLine($"Time elapsed: {watch.ElapsedMilliseconds} ms");

            workbookInput.Close();
            workbookInput.Dispose();
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
            MessageBox.Show(ex.Message, "Có lỗi xảy ra", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    #endregion
}
public partial class TestMapDataViewModel
{
    private async Task HandleUnitOfWorkAsync(UnitOfWorkModel uow, HSSFWorkbook workbookInput)
    {
        var moneys = uow.MoneyCellModels;
        var headings = uow.HeadingCellModels;
        var reqDetectData = new DetectDataRequest(ref uow);
        var taskDetectData = _mediator.Send(reqDetectData);

        var reqLoadInputData = new LoadReferenceFsNoteDataRequest(workbookInput, "TM1", ref uow);
        var taskLoadInputData = _mediator.Send(reqLoadInputData);
        await Task.WhenAll(taskDetectData, taskLoadInputData);
        await taskDetectData;
        await taskLoadInputData;

        _detectService.GroupFsNoteDataRange(uow);
    }

}