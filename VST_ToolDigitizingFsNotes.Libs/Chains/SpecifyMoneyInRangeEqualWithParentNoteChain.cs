using System.Diagnostics;
using VST_ToolDigitizingFsNotes.Libs.Models;
using VST_ToolDigitizingFsNotes.Libs.Utils;

namespace VST_ToolDigitizingFsNotes.Libs.Chains;

public class SpecifyMoneyInRangeEqualWithParentRequest : ChainBaseRequest<SpecifyMoneyResult>
{
    /// <summary>
    /// Số này phù hợp nhất, nếu lớn hơn thì xử lý rất lâu
    /// </summary>
    public const int AllowListMoneyLength = 24;
    public UnitOfWorkModel UnitOfWork { get; init; }
    public FsNoteDataMap DataMap { get; init; }

    //public bool IgnoreNextSpecifyCol { get; set; }
    //public bool IgnoreNextSpecifyRow { get; set; }
    public HashSet<int> IgnoreCols { get; set; } = [];
    public HashSet<int> IgnoreRows { get; set; } = [];

    public SpecifyMoneyInRangeEqualWithParentRequest(UnitOfWorkModel unitOfWork, FsNoteDataMap dataMap)
    {
        UnitOfWork = unitOfWork;
        DataMap = dataMap;
    }

    public static bool IsContinuousListMoney(List<MoneyCellModel> lst, Func<MoneyCellModel, int> selector, double threshold = 0.8)
    {
        if(lst.Count == 0)
        {
            return false;
        }
        if(lst.Count == 1)
        {
            return true;
        }
        lst.Sort((x, y) => selector(x).CompareTo(selector(y)));
        var lstDistances = new List<int>();
        for(int i = 1; i< lst.Count; i++)
        {
            lstDistances.Add(selector(lst[i]) - selector(lst[i-1]));
        }
        var std = CoreUtils.CalculateStandardDeviation(lstDistances);
        return std < threshold;
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

        using var cts = new CancellationTokenSource();
        //cts.CancelAfter(3333);
        var ctsToken = cts.Token;

        if (moneysCol != null && moneysCol.Count > 0)
        {
            try
            {
                var data = moneysCol;
                if (moneysCol.Count > SpecifyMoneyInRangeEqualWithParentRequest.AllowListMoneyLength)
                {
                    // split list
                    data = moneysCol.Take(SpecifyMoneyInRangeEqualWithParentRequest.AllowListMoneyLength).ToList();
                }

                var list = DetectUtils.FindAllSubsetSums(data, Math.Abs(parent!.Value), x => (x.Value),
                    SpecifyMoneyInRangeEqualWithParentRequest.AllowListMoneyLength, ctsToken);

                if(list.Count > 0)
                {
                    request.IgnoreCols.Add(Target.Col);
                    foreach (var moneyCol in list)
                    {
                        var check = SpecifyMoneyInRangeEqualWithParentRequest.IsContinuousListMoney(moneyCol, x => x.Row);
                        if (check)
                        {
                            result.DataCols.Add(moneyCol);
                        }
                    }
                    
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(request.DataMap.FsNoteParentModel.Name);
                Debug.WriteLine(ex.Message);
            }
        }
        groupByRow.TryGetValue(Target.Row, out var moneysRow);
        if (moneysRow != null && moneysRow.Count > 0)
        {
            try
            {
                var data = moneysRow;
                if (moneysRow.Count > SpecifyMoneyInRangeEqualWithParentRequest.AllowListMoneyLength)
                {
                    // split list
                    data = moneysRow.Take(SpecifyMoneyInRangeEqualWithParentRequest.AllowListMoneyLength).ToList();
                }
                var list = DetectUtils.FindAllSubsetSums(data, Math.Abs(parent!.Value), x => (x.Value),
                    SpecifyMoneyInRangeEqualWithParentRequest.AllowListMoneyLength, ctsToken);

                if (list.Count > 0)
                {
                    request.IgnoreRows.Add(Target.Row);
                    foreach (var moneyRow in list)
                    {
                        var check = SpecifyMoneyInRangeEqualWithParentRequest.IsContinuousListMoney(moneyRow, x => x.Col);
                        if (check )
                        {
                            result.DataRows.Add(moneyRow);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(request.DataMap.FsNoteParentModel.Name);
                Debug.WriteLine(ex.Message);
            }
        }
        if (result.HasDataCols || result.HasDataRows)
        {
            request.Result = result;
            //request.IgnoreNextSpecifyCol = result.HasDataCols;
            //request.IgnoreNextSpecifyRow = result.HasDataRows;
            request.SetHandled(_nextChain == null);
            _nextChain?.Handle(request);
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
        var groupByRow = MoneysInRange.Where(x => !request.IgnoreRows.Contains(x.Row)).GroupBy(x => x.Row).ToDictionary(x => x.Key, x => x.ToList());
        /// group moneys theo cột
        var groupByCol = MoneysInRange.Where(x => !request.IgnoreCols.Contains(x.Col)).GroupBy(x => x.Col).ToDictionary(x => x.Key, x => x.ToList());
        var result = request.Result ?? new SpecifyMoneyResult();
        // find all row
        using var cts = new CancellationTokenSource();
        var ctsToken = cts.Token;
        foreach (var rowKeys in groupByRow.Keys)
        {
            try
            {
                var moneysRow = groupByRow[rowKeys];
                var data = moneysRow;
                if (moneysRow.Count > SpecifyMoneyInRangeEqualWithParentRequest.AllowListMoneyLength)
                {
                    // split list
                    data = moneysRow.Take(SpecifyMoneyInRangeEqualWithParentRequest.AllowListMoneyLength).ToList();
                }
                var moneyRows = DetectUtils.FindAllSubsetSums(data, Math.Abs(parent!.Value), x => (x.Value),
                    SpecifyMoneyInRangeEqualWithParentRequest.AllowListMoneyLength, ctsToken);

                if(moneyRows.Count > 0)
                {
                    foreach (var moneyRow in moneyRows)
                    {
                        var check = SpecifyMoneyInRangeEqualWithParentRequest.IsContinuousListMoney(moneyRow, x => x.Col);
                        if (check)
                        {
                            result.DataRows.Add(moneyRow);
                        }
                    }
                }    
               
            }
            catch (Exception ex)
            {
                Debug.WriteLine(request.DataMap.FsNoteParentModel.Name);
                Debug.WriteLine(ex.Message);
            }
        }

        // find all col
        foreach (var colKeys in groupByCol.Keys)
        {
            try
            {
                var moneysCol = groupByCol[colKeys];
                var data = moneysCol;

                if (moneysCol.Count > SpecifyMoneyInRangeEqualWithParentRequest.AllowListMoneyLength)
                {
                    // split list
                    data = moneysCol.Take(SpecifyMoneyInRangeEqualWithParentRequest.AllowListMoneyLength).ToList();
                }

                var moneyCols = DetectUtils.FindAllSubsetSums(data, Math.Abs(parent!.Value), x => (x.Value),
                    SpecifyMoneyInRangeEqualWithParentRequest.AllowListMoneyLength, ctsToken);

                if (moneyCols.Count > 0)
                {
                    foreach (var moneyCol in moneyCols)
                    {
                        var check = SpecifyMoneyInRangeEqualWithParentRequest.IsContinuousListMoney(moneyCol, x => x.Row);
                        if (check)
                        {
                            result.DataCols.Add(moneyCol);
                        }
                    }
                }
                
            }
            catch (Exception ex)
            {
                Debug.WriteLine(request.DataMap.FsNoteParentModel.Name);
                Debug.WriteLine(ex.Message);
            }
        }

        if (result.HasDataCols || result.HasDataRows)
        {
            request.Result = result;
            request.SetHandled(_nextChain == null);
            _nextChain?.Handle(request);
        }
        else
        {
            _nextChain?.Handle(request);
        }
    }
}