using NPOI.HSSF.UserModel;
using System.Diagnostics;

namespace VST_ToolDigitizingFsNotes.Libs.Models
{
    /// <summary>
    /// Class đại diện cho một đơn vị làm việc với 1 file bctc trên 1 luồng
    /// Lưu trữ dữ liệu tiền, heading, heading group ... sau giai đoạn tiền xử lý hoàn tất
    /// </summary>
    public class UnitOfWorkModel
    {
        public object lockObject = new();
        public List<HeadingCellModel> HeadingCellModels { get; }
        public List<MoneyCellModel> MoneyCellModels { get; }
        public List<FsNoteParentModel> FsNoteParentModels { get; }
        public HSSFWorkbook? Workbook { get; set; }

        public UnitOfWorkModel
            (
            //List<HeadingCellModel> headingCellModels,
            //List<MoneyCellModel> moneyCellModels,
            //List<FsNoteParentModel> fsNoteParentModels
            )
        {
            HeadingCellModels = [];
            MoneyCellModels = [];
            FsNoteParentModels = [];
        }

        public void PrintHeadingGroup()
        {
            foreach (var headingCellModel in HeadingCellModels)
            {
                foreach (var headingGroup in headingCellModel.MetaData.BackData)
                {
                    Debug.WriteLine($"{headingCellModel.Row}:{headingCellModel.Col} - {headingCellModel.CellValue} - {headingGroup}");
                }
            }
        }
    }
}
