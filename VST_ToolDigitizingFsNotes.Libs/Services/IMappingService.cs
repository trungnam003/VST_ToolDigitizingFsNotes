using VST_ToolDigitizingFsNotes.Libs.Models;

namespace VST_ToolDigitizingFsNotes.Libs.Services
{
    public interface IMappingService
    {
        Task LoadMapping();
        Task LoadMapping2();


        void MapFsNoteWithMoney(UnitOfWorkModel uow, FsNoteDataMap dataMap);

        void CombineUnitOfWorks(UnitOfWorkModel uow);
    }
}
