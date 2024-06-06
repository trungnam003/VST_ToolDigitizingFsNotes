
using NPOI.POIFS.Properties;
using VST_ToolDigitizingFsNotes.Libs.Models;
using VST_ToolDigitizingFsNotes.Libs.Utils;

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

/// <summary>
/// Xác định tiền theo cột hoặc dòng bằng với giá trị của parent
/// </summary>
public class SpecifyMoneyInRangeEqualWithParentHandle : HandleChainBase<SpecifyMoneyInRangeEqualWithParentRequest>
{
    public List<MoneyCellModel> MoneysInRange { get; init; }
    public MoneyCellModel Target { get; init; }
    public SpecifyMoneyInRangeEqualWithParentHandle(List<MoneyCellModel> moneyCells, MoneyCellModel target)
    {
        MoneysInRange = moneyCells;
        Target = target;
    }
    public override void Handle(SpecifyMoneyInRangeEqualWithParentRequest request)
    {
        var uow = request.UnitOfWork;
        var dataMap = request.DataMap;
        var parent = dataMap.FsNoteParentModel;
        /// group moneys theo dòng
        var groupByRow = MoneysInRange.GroupBy(x => x.Row).ToDictionary(x => x.Key, x => x.ToList());
        /// group moneys theo cột
        var groupByCol = MoneysInRange.GroupBy(x => x.Col).ToDictionary(x => x.Key, x => x.ToList());
        groupByCol.TryGetValue(Target.Col, out var moneysCol);
        groupByRow.TryGetValue(Target.Row, out var moneysRow);
        var moneyRow = DetectUtils.FindAllSubsetSums(moneysRow ?? [], Math.Abs(parent!.Value), x => (x.Value));
        var moneyCol = DetectUtils.FindAllSubsetSums(moneysCol ?? [], Math.Abs(parent!.Value), x => (x.Value));

        /// tiếp tục xử lý
    }
}