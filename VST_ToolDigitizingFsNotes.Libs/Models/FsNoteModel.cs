
namespace VST_ToolDigitizingFsNotes.Libs.Models
{
    /// <summary>
    /// Đại diện cho các thuyết minh BCTC trong file excel cần được import dữ liệu từ file pdf BCTC
    /// </summary>
    public class FsNoteModel
    {
        public int FsNoteId { get; set; }
        public string? Name { get; set; }
        public double Value { get; set; }
        public int ParentId { get; set; }
        public bool IsParent { get; set; }
        public int Group { get; set; }
        public string? CellAddress { get; set; }
        public Tuple<int, int>? Cell { get; set; }
        public List<double>? Values { get; set; }
    }

    /// <summary>
    /// Đại diện cho các thuyết minh BCTC (cha) trong file excel cần được import dữ liệu từ file pdf BCTC
    /// </summary>
    public class FsNoteParentModel : FsNoteModel
    {
        public List<FsNoteModel> Children { get; set; } = [];
    }
}
