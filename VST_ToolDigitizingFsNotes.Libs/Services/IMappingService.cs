using VST_ToolDigitizingFsNotes.Libs.Models;

namespace VST_ToolDigitizingFsNotes.Libs.Services
{
    public interface IMappingService
    {
        Task LoadMapping();
        Task LoadMapping2();
        Task<bool> LoadMappingWithStockCode(string path, string stockCode);


        void MapFsNoteWithMoney(UnitOfWorkModel uow, FsNoteDataMap dataMap);

        void CombineUnitOfWorks(UnitOfWorkModel uow);

        List<FsNoteMappingModel>? GetChildrenMappingList(int id, int group, string stockCode);
    }
}
