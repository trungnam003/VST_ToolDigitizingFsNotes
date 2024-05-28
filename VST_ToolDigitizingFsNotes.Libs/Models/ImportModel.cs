namespace VST_ToolDigitizingFsNotes.Libs.Models
{
    public class ImportModel
    {
        public string? FileName { get; set; }
        public List<string> FsNoteSheets { get; set; } = [];
        public List<FsNoteSheetModel> FsNoteSheetModels { get; set; } = [];

    }

    public class FsNoteSheetModel
    {
        public string StockCode { get; set; } = string.Empty;
        public string ReportTerm { get; set; } = string.Empty;
        public List<FsNoteModel> FsNoteModels { get; set; } = [];
    }

}
