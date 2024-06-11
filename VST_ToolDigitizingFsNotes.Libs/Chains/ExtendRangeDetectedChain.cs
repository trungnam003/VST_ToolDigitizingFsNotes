using VST_ToolDigitizingFsNotes.Libs.Models;

namespace VST_ToolDigitizingFsNotes.Libs.Chains;

public class ExtendRangeDetectedRequest : ChainBaseRequest<object>
{
    public UnitOfWorkModel UnitOfWork { get; init; }
    public FsNoteDataMap DataMap { get; init; }

    public ExtendRangeDetectedRequest(UnitOfWorkModel unitOfWork, FsNoteDataMap dataMap)
    {
        UnitOfWork = unitOfWork;
        DataMap = dataMap;
    }
}

public class ExtendRangeDetectedUsingBottomNearestHeadingHandler : HandleChainBase<ExtendRangeDetectedRequest>
{
    public override void Handle(ExtendRangeDetectedRequest request)
    {
        throw new NotImplementedException();
    }
}