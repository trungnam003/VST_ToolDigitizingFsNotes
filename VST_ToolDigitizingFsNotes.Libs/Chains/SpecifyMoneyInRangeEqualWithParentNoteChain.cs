using VST_ToolDigitizingFsNotes.Libs.Models;
using VST_ToolDigitizingFsNotes.Libs.Utils;

namespace VST_ToolDigitizingFsNotes.Libs.Chains;

public class SpecifyMoneyInRangeEqualWithParentRequest : ChainBaseRequest<SpecifyMoneyResult>
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
        var result = new SpecifyMoneyResult();

        groupByCol.TryGetValue(Target.Col, out var moneysCol);
        if (moneysCol != null && moneysCol.Count > 0)
        {
            var list = DetectUtils.FindAllSubsetSums(moneysCol, Math.Abs(parent!.Value), x => (x.Value));
            result.DataCols.AddRange(list);
        }
        groupByRow.TryGetValue(Target.Row, out var moneysRow);
        if (moneysRow != null && moneysRow.Count > 0)
        {
            var list = DetectUtils.FindAllSubsetSums(moneysRow, Math.Abs(parent!.Value), x => (x.Value));
            result.DataRows.AddRange(list);
        }
        if (result.HasDataCols || result.HasDataRows)
        {
            request.Result = result;
            request.SetHandled(true);
            return;
        }
        else
        {
            _nextChain?.Handle(request);
        }
    }
}

public class SpecifyAllMoneyInRangeHandle : HandleChainBase<SpecifyMoneyInRangeEqualWithParentRequest>
{
    public List<MoneyCellModel> MoneysInRange { get; init; }
    public MoneyCellModel Target { get; init; }

    public SpecifyAllMoneyInRangeHandle(List<MoneyCellModel> moneyCells, MoneyCellModel target)
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
        var result = new SpecifyMoneyResult();
        // find all row
        foreach (var rowKeys in groupByRow.Keys)
        {
            var moneyRows = DetectUtils.FindAllSubsetSums(groupByRow[rowKeys], Math.Abs(parent!.Value), x => (x.Value));
            result.DataRows.AddRange(moneyRows);
        }

        // find all col
        foreach (var colKeys in groupByCol.Keys)
        {
            var moneyCols = DetectUtils.FindAllSubsetSums(groupByCol[colKeys], Math.Abs(parent!.Value), x => (x.Value));
            result.DataCols.AddRange(moneyCols);
        }

        if (result.HasDataCols || result.HasDataRows)
        {
            request.Result = result;
            request.SetHandled(true);
            return;
        }
        else
        {
            _nextChain?.Handle(request);
        }

    }
}