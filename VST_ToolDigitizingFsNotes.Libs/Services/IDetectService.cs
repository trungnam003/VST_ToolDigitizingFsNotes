using NPOI.SS.UserModel;
using VST_ToolDigitizingFsNotes.Libs.Models;

namespace VST_ToolDigitizingFsNotes.Libs.Services
{
    public interface IDetectService
    {
        void DetectHeadings(string cellValue, ICell cell, ref List<HeadingCellModel> headings);
        void DetectMoneys(string cellValue, ICell cell, ref List<MoneyCellModel> moneys);

        //void GroupFsNoteDataRange(UnitOfWorkModel model);

        void StartDetectFsNotesAsync(UnitOfWorkModel uow, CancellationToken cancellationToken = default);
    }
}
