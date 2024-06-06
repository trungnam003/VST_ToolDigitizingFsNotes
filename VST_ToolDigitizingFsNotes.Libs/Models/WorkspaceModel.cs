namespace VST_ToolDigitizingFsNotes.Libs.Models
{
    public class WorkspaceMetadata
    {
        public static readonly string ocrFolderName = "OCR";
        public static readonly string outputFolderName = "Output";
        public static readonly string pdfDownloadFolderName = "PdfDownload";
        public static readonly string DataFolderName = "Data";


        public string Path { get; set; } = string.Empty;
        public required string Name { get; set; }

        public string OcrPath
        {
            get
            {
                return System.IO.Path.Combine(Path, ocrFolderName);
            }
        }
        public string OutputPath
        {
            get
            {
                return System.IO.Path.Combine(Path, outputFolderName);
            }
        }
        public string PdfDownloadPath
        {
            get
            {
                return System.IO.Path.Combine(Path, pdfDownloadFolderName);
            }
        }
        public string DataPath
        {
            get
            {
                return System.IO.Path.Combine(Path, DataFolderName);
            }
        }
    }

    /// <summary>
    /// Đại diện cho một file import
    /// </summary>
    public class FileImportFsNoteModel
    {
        public string Name { get; set; }
        public string SourcePath { get; set; }
        public string DestinationPath { get; set; }
        public string ErrorMessage { get; set; }
        public string WarningMessage { get; set; }
        public Dictionary<string, SheetFsNoteModel> FsNoteSheets { get; set; }
    }

    /// <summary>
    /// Đại diện cho một sheet trong file import
    /// 1 UnitOfWork tương ứng với thực hiện trên 1 sheet
    /// </summary>
    public class SheetFsNoteModel
    {
        public static readonly string None = "None";
        public class MetaData
        {

            public static readonly int MetaDataColIndex = 5; // F

            public static readonly int StockRowIndex = 2; // F3
            public static readonly int ReportTermRowIndex = 3; // F4
            public static readonly int YearRowIndex = 4; // F5
            public static readonly int AuditedStatusRowIndex = 5; // F6
            public static readonly int ReportTypeRowIndex = 6; // F7
            public static readonly int UnitRowIndex = 9; // F10

            // file pdf url F1
            public static readonly int FileUrlRowIndex = 0;
            public static readonly int FileUrlColIndex = 5;

            public string? FilePdfFsPath { get; set; }
            public string? FileOcrV11Path { get; set; }
            public string? FileOcrV14Path { get; set; }
            public string? FileOcrV15Path { get; set; }

            public bool IsDownloaded { get; set; }
            public bool IsFileOcrV11Created { get; set; }
            public bool IsFileOcrV14Created { get; set; }
            public bool IsFileOcrV15Created { get; set; }
        }

        public string? SheetName { get; set; }
        public string? StockCode { get; set; }
        public string? ReportTerm { get; set; }
        public int Year { get; set; }
        public string? AuditedStatus { get; set; }
        public string? ReportType { get; set; }
        public string? Unit { get; set; }
        public string? FileUrl { get; set; }
        public List<SheetFsNoteDataModel> Data { get; set; } = [];
        public string? ErrorMessage { get; set; }
        public SheetFsNoteModel.MetaData? Meta { get; set; }

        public string Information
        {
            get
            {
                return $"Mã CK: {StockCode ?? None}; Kỳ: {ReportTerm ?? None}; Năm: {Year}; TTKD: {AuditedStatus ?? None}; Loại BC: {ReportType ?? None}; ĐVT: {Unit ?? None}";
            }
        }
    }
    /// <summary>
    /// Đại diện cho dữ liệu của một sheet
    /// </summary>
    public class SheetFsNoteDataModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public double TotalValue { get; set; }
        public List<double> Values { get; set; }
        public bool IsParent { get; set; }
    }
}
