
using VST_ToolDigitizingFsNotes.Libs.Models;

namespace VST_ToolDigitizingFsNotes.Libs.Chains;

public class SpecifyMoneyInRangeEqualWithParentRequest : ChainBaseRequest<object>
{
    public UnitOfWorkModel UnitOfWork { get; init; }
    public FsNoteDataMap DataMap { get; init; }

    public SpecifyMoneyInRangeEqualWithParentRequest(UnitOfWorkModel unitOfWork, FsNoteDataMap dataMap)
    {
        UnitOfWork = unitOfWork;
        DataMap = dataMap;
    }
}