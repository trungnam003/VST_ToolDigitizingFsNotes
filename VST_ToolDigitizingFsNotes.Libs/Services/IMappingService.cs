using VST_ToolDigitizingFsNotes.Libs.Models;

namespace VST_ToolDigitizingFsNotes.Libs.Services
{
    public interface IMappingService
    {
        Task LoadMapping();

        void MapFsNoteWithMoney(UnitOfWorkModel uow, FsNoteDataMap dataMap);
    }
}
