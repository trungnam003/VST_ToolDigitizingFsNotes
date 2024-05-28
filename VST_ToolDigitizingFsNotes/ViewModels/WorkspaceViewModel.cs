using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.ObjectModel;
using System.Windows;
using VST_ToolDigitizingFsNotes.Libs.Services;

namespace VST_ToolDigitizingFsNotes.AppMain.ViewModels;

public partial class WorkspaceViewModel : ObservableObject
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IWorkspaceService _workspaceService;
    private readonly HomeViewModel homeViewModel;

    public WorkspaceViewModel(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
        _workspaceService = _serviceProvider.GetRequiredService<IWorkspaceService>();
        homeViewModel = _serviceProvider.GetRequiredService<HomeViewModel>();
        Name = _workspaceService.GenerateName();
    }

    [ObservableProperty]
    private string _name;

    [ObservableProperty]
    private ObservableCollection<FileImportFsNoteModel> _fileImportFsNoteModels;

    [ObservableProperty]
    private FileImportFsNoteModel _selectedFileImport;

    [RelayCommand]
    private void SelectFileImport(FileImportFsNoteModel selected)
    {
    }
    [RelayCommand]
    private void Start()
    {
        MessageBox.Show("Start");
    }


}

public class FileImportFsNoteModel
{
    public string Name { get; set; }
    public string SourcePath { get; set; }
    public string DestinationPath { get; set; }
    public string ErrorMessage { get; set; }
    public string WarningMessage { get; set; }
    public Dictionary<string, SheetFsNoteModel> FsNoteSheets { get; set; }
}


public class SheetFsNoteModel
{
    public static readonly string None = "None";
    public static class MetaData
    {

        public static readonly int MetaDataColIndex = 5; // F

        public static readonly int StockRowIndex = 2; // F3
        public static readonly int ReportTermRowIndex = 3; // F4
        public static readonly int YearRowIndex = 4; // F5
        public static readonly int AuditedStatusRowIndex = 5; // F6
        public static readonly int ReportTypeRowIndex = 6; // F7
        public static readonly int UnitRowIndex = 9; // F10

        // file pdf url I1
        public static readonly int FileUrlRowIndex = 0; 
        public static readonly int FileUrlColIndex = 8;
    }

    public string SheetName { get; set; }
    public string StockCode { get; set; }
    public string ReportTerm { get; set; }
    public int Year { get; set; }
    public string AuditedStatus { get; set; }
    public string ReportType { get; set; }
    public string Unit { get; set; }
    public string FileUrl { get; set; }
    public List<SheetFsNoteDataModel> Data { get; set; }
    public string ErrorMessage { get; set; }

    public string Information
    {
        get
        {
            return $"Mã CK: {StockCode ?? None}; Kỳ: {ReportTerm ?? None}; Năm: {Year}; TTKD: {AuditedStatus ?? None}; Loại BC: {ReportType ?? None}; ĐVT: {Unit ?? None}";
        }
    }
}

public class SheetFsNoteDataModel
{
    public int Id { get; set; }
    public string Name { get; set; }
    public double TotalValue { get; set; }
    public List<double> Values { get; set; }
    public bool IsParent { get; set; }
}