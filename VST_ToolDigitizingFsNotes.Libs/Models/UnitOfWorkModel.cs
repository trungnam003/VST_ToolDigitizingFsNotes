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
    }
}
