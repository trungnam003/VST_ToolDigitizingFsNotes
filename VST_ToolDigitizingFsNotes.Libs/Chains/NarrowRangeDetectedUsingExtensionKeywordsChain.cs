using VST_ToolDigitizingFsNotes.Libs.Models;

namespace VST_ToolDigitizingFsNotes.Libs.Chains;

public class NarrowRangeDetectedUsingExtensionKeywordsRequest : ChainBaseRequest<RangeDetectFsNote>
{
    public UnitOfWorkModel UnitOfWork { get; init; }
    public FsNoteParentModel Parent { get; init; }

    public NarrowRangeDetectedUsingExtensionKeywordsRequest(UnitOfWorkModel unitOfWork, FsNoteParentModel parent)
    {
        UnitOfWork = unitOfWork;
        Parent = parent;
    }
}

public class NarrowRangeDetectedUsingExtensionKeywordsHandler : HandleChainBase<NarrowRangeDetectedUsingExtensionKeywordsRequest>
{
    public FsNoteParentMappingModel MapParentData { get; init; }

    public NarrowRangeDetectedUsingExtensionKeywordsHandler(FsNoteParentMappingModel mapParentData)
    {
        MapParentData = mapParentData;
    }

    public override void Handle(NarrowRangeDetectedUsingExtensionKeywordsRequest request)
    {
        throw new NotImplementedException();
    }
}