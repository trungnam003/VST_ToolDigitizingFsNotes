using NPOI.XSSF.UserModel;

namespace VST_ToolDigitizingFsNotes.Libs.Models
{
    /// <summary>
    /// Class đại diện cho một đơn vị làm việc với 1 file bctc trên 1 luồng
    /// Lưu trữ dữ liệu tiền, heading, heading group ... sau giai đoạn tiền xử lý hoàn tất
    /// </summary>
    public sealed class UnitOfWorkModel : IDisposable
    {
        public object lockObject = new();
        public List<HeadingCellModel> HeadingCellModels { get; }
        public List<MoneyCellModel> MoneyCellModels { get; }
        public List<FsNoteParentModel> FsNoteParentModels { get; }
        public XSSFWorkbook? OcrWorkbook { get; set; }
        public HashSet<SpecifiedRange> SpecifiedRanges { get; } = [];

        private bool _disposed;

        public UnitOfWorkModel()
        {
            HeadingCellModels = [];
            MoneyCellModels = [];
            FsNoteParentModels = [];
        }

        private void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    using (OcrWorkbook) OcrWorkbook?.Close();
                }
                _disposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public void AddToSpecifiedRanges(int startRow, int endRow, int fsNoteId)
        {
            SpecifiedRanges.Add(new()
            {
                StartRow = startRow,
                EndRow = endRow,
                FsNoteId = fsNoteId
            });
        }

        public bool CheckContainSpecifiedRanges(int row, int fsNoteId, out int oEndRow)
        {
            oEndRow = -1;
            foreach (var item in SpecifiedRanges)
            {
                if ((row >= item.StartRow && row <= item.EndRow))
                {
                    if (item.FsNoteId == fsNoteId) continue;
                    oEndRow = item.EndRow;
                    return true;
                }
            }
            return false;
        }
    }

    public sealed class SpecifiedRange
    {
        public int StartRow { get; set; }
        public int EndRow { get; set; }
        public int FsNoteId { get; set; }


        public override int GetHashCode()
        {
            return HashCode.Combine(StartRow, EndRow, FsNoteId);
        }

        public override bool Equals(object? obj)
        {
            if (obj is SpecifiedRange other)
            {
                return StartRow == other.StartRow && EndRow == other.EndRow && FsNoteId == other.FsNoteId;
            }
            return false;
        }
    }
}
