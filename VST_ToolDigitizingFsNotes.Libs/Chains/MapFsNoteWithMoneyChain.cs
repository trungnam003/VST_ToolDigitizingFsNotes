
using Ardalis.SmartEnum;
using Force.DeepCloner;
using System.Diagnostics;
using VST_ToolDigitizingFsNotes.Libs.Models;
using VST_ToolDigitizingFsNotes.Libs.Utils;

namespace VST_ToolDigitizingFsNotes.Libs.Chains;

public class MapFsNoteWithMoneyChainRequest : ChainBaseRequest<MapEvaluators>
{

    public UnitOfWorkModel Uow { get; init; }
    public List<TextCellSuggestModel> ListTextCellSuggests { get; init; }
    public List<MoneyCellModel> ListMoneyCells { get; init; }

    public RangeDetectFsNote Range { get; init; }
    public FsNoteDataMap DataMap { get; init; }

    public MapFsNoteWithMoneyChainRequest(List<TextCellSuggestModel> listTextCellSuggests, List<MoneyCellModel> listMoneyCells, UnitOfWorkModel uow, RangeDetectFsNote range, FsNoteDataMap dataMap)
    {
        ListTextCellSuggests = listTextCellSuggests;
        ListMoneyCells = listMoneyCells;
        Uow = uow;
        Range = range;
        DataMap = dataMap;
    }
}

public class MapEvaluatorStatus : SmartEnum<MapEvaluatorStatus>
{
    public static readonly MapEvaluatorStatus CantMap = new(nameof(CantMap), -1);
    public static readonly MapEvaluatorStatus NotYetMapped = new(nameof(NotYetMapped), 0);
    public static readonly MapEvaluatorStatus Mapped = new(nameof(Mapped), 1);

    public MapEvaluatorStatus(string name, int value) : base(name, value)
    {
    }
}

public class MapType : SmartEnum<MapType>
{
    public static readonly MapType None = new(nameof(None), 0);
    public static readonly MapType MappedInRow = new(nameof(MappedInRow), 1);
    public static readonly MapType MappedInColumn = new(nameof(MappedInColumn), 2);

    public MapType(string name, int value) : base(name, value)
    {
    }
}

public class MapEvaluator
{
    public TextCellSuggestModel textCellSuggest { get; init; }
    public MoneyCellModel moneyCell { get; init; }

    public double Distance { get; set; }
    public double Angle { get; set; }

    public MapEvaluatorStatus Status { get; set; } = MapEvaluatorStatus.NotYetMapped;
    public MapType MapType { get; set; } = MapType.None;

    public MapEvaluator(TextCellSuggestModel textCellSuggest, MoneyCellModel moneyCell)
    {
        this.textCellSuggest = textCellSuggest;
        this.moneyCell = moneyCell;
    }
}

public class MapEvaluators
{
    public List<MapEvaluator> ListMapEvaluators { get; init; } = [];
    public HashSet<MoneyCellModel> MoneyCellMapped { get; init; } = [];
    public HashSet<TextCellSuggestModel> TextCellMapped { get; init; } = [];
}


#region Các chỉ tiêu theo dòng
/// <summary>
/// Map trên cùng 1 hàng
/// </summary>
public class MapInRowHandler : HandleChainBase<MapFsNoteWithMoneyChainRequest>
{
    public override void Handle(MapFsNoteWithMoneyChainRequest request)
    {
        if (request.Handled)
        {
            return;
        }
        // là root nên không cần kiểm tra null và khởi tạo mới giá trị
        var evaluators = new MapEvaluators();
        request.Result = evaluators;

        foreach (var money in request.ListMoneyCells)
        {
            var row = money.Row;
            var predicate = new Func<TextCellSuggestModel, bool>(
               x => (x.Row == row || (x.CombineWithCell != null && x.CombineWithCell.Row == row))
                && x.CellStatus == CellStatus.Default);

            var rowWithRow = request.ListTextCellSuggests
                .Except(evaluators.TextCellMapped)
                .FirstOrDefault(predicate);

            if (rowWithRow == null)
            {
                continue;
            }

            var evaluator = new MapEvaluator(rowWithRow, money)
            {
                MapType = MapType.MappedInRow,
                Status = MapEvaluatorStatus.Mapped
            };
            evaluators.ListMapEvaluators.Add(evaluator);
            evaluators.MoneyCellMapped.Add(money);
            evaluators.TextCellMapped.Add(rowWithRow);
            Debug.WriteLine($">> {evaluator.textCellSuggest.CellValue} - {evaluator.moneyCell.Value}");
        }

        foreach (var money in request.ListMoneyCells.Except(evaluators.MoneyCellMapped))
        {
            var row = money.Row;
            var predicate = new Func<TextCellSuggestModel, bool>(
                              x => x.CellStatus == CellStatus.Merge && x.RetriveCell != null && x.RetriveCell.Row == row);

            var rowWithRowMerge = request.ListTextCellSuggests
                .Except(evaluators.TextCellMapped)
                .FirstOrDefault(predicate);

            if (rowWithRowMerge == null)
            {
                continue;
            }
            var evaluator = new MapEvaluator(rowWithRowMerge, money)
            {
                MapType = MapType.MappedInRow,
                Status = MapEvaluatorStatus.Mapped
            };
            evaluators.ListMapEvaluators.Add(evaluator);
            evaluators.MoneyCellMapped.Add(money);
            evaluators.TextCellMapped.Add(rowWithRowMerge);
            Debug.WriteLine($"M>> {evaluator.textCellSuggest.CellValue} - {evaluator.moneyCell.Value}");
        }

        foreach (var money in request.ListMoneyCells.Except(evaluators.MoneyCellMapped).GroupBy(x => x.Row).ToDictionary(x => x.Key, x => x.ToList()))
        {
            var row = money.Key;
            var predicate = new Func<TextCellSuggestModel, bool>(
                             x => x.CellStatus == CellStatus.Merge && x.Row == row);
            var rowWithRowMerges = request.ListTextCellSuggests
                .Except(evaluators.TextCellMapped)
                .Where(predicate).ToList();

            if (rowWithRowMerges.Count == money.Value.Count)
            {
                for (int i = 0; i < rowWithRowMerges.Count; i++)
                {
                    var item = rowWithRowMerges[i];
                    var evaluator = new MapEvaluator(item, money.Value[i])
                    {
                        MapType = MapType.MappedInRow,
                        Status = MapEvaluatorStatus.Mapped
                    };
                    evaluators.ListMapEvaluators.Add(evaluator);
                    evaluators.MoneyCellMapped.Add(money.Value[i]);
                    evaluators.TextCellMapped.Add(item);
                    Debug.WriteLine($"M2>> {evaluator.textCellSuggest.CellValue} - {evaluator.moneyCell.Value}");
                }
            }
        }

        if (evaluators.ListMapEvaluators.Count != 0)
        {
            if (evaluators.ListMapEvaluators.Count == request.ListMoneyCells.Count)
            {
                request.SetHandled(true);
                return;
            }
            else
            {
                var moneyNotMapped = request.ListMoneyCells.Except(evaluators.MoneyCellMapped).ToList();
                foreach (var money in moneyNotMapped)
                {
                    var textNotMapped = request.ListTextCellSuggests.Except(evaluators.TextCellMapped).ToList();
                    var minDistance = double.MaxValue;
                    TextCellSuggestModel? textCellMinDistance = null;
                    foreach (var text in textNotMapped)
                    {
                        var distance = CoreUtils.EuclideanDistance(text.Row + text.IndexInCell, text.Col, money.Row, money.Col);
                        if (distance < minDistance)
                        {
                            minDistance = distance;
                            textCellMinDistance = text;
                        }
                    }

                    const int acceptRowDistance = 1;
                    if (textCellMinDistance != null)
                    {
                        if (Math.Abs(textCellMinDistance.Row - money.Row) == acceptRowDistance)
                        {
                            var evaluator = new MapEvaluator(textCellMinDistance, money)
                            {
                                MapType = MapType.MappedInRow,
                                Status = MapEvaluatorStatus.Mapped
                            };
                            evaluators.ListMapEvaluators.Add(evaluator);
                            evaluators.MoneyCellMapped.Add(money);
                            evaluators.TextCellMapped.Add(textCellMinDistance);
                            Debug.WriteLine($">> {evaluator.textCellSuggest.CellValue} - {evaluator.moneyCell.Value}");
                        }
                    }
                }
                if (evaluators.ListMapEvaluators.Count == request.ListMoneyCells.Count)
                {
                    request.SetHandled(true);
                    return;
                }
            }
        }
        _nextChain?.Handle(request);
    }
}

public class MapWhenOcrLineBreakErrorHandler : HandleChainBase<MapFsNoteWithMoneyChainRequest>
{
    public override void Handle(MapFsNoteWithMoneyChainRequest request)
    {
        if (request.Handled)
        {
            return;
        }
        request.Result ??= new MapEvaluators();
        var evaluators = request.Result;
        if (request.ListMoneyCells.Count == request.ListTextCellSuggests.Count)
        {
            for (int i = 0; i < request.ListMoneyCells.Count; i++)
            {
                var evaluator = new MapEvaluator(request.ListTextCellSuggests[i], request.ListMoneyCells[i])
                {
                    MapType = MapType.MappedInRow,
                    Status = MapEvaluatorStatus.Mapped
                };
                evaluators.ListMapEvaluators.Add(evaluator);
                evaluators.MoneyCellMapped.Add(request.ListMoneyCells[i]);
                evaluators.TextCellMapped.Add(request.ListTextCellSuggests[i]);
                Debug.WriteLine($"(e)>> {evaluator.textCellSuggest.CellValue} - {evaluator.moneyCell.Value}");

            }
        }
        else
        {
            var count = request.ListMoneyCells.Count;
            var draftCells = request.ListTextCellSuggests.DeepClone();
            draftCells.Sort(TextCellSuggestModel.Comparer);

            var start = draftCells.First().Row;
            var end = draftCells.Last().Row;
            // Xử lý
        }

        if (evaluators.ListMapEvaluators.Count == request.ListMoneyCells.Count)
        {
            request.SetHandled(true);
            return;
        }
    }
}
#endregion


#region Các chỉ tiêu theo cột
public class MapInColHandler : HandleChainBase<MapFsNoteWithMoneyChainRequest>
{
    public override void Handle(MapFsNoteWithMoneyChainRequest request)
    {
        if (request.Handled)
        {
            return;
        }
        // là root nên không cần kiểm tra null và khởi tạo mới giá trị
        var evaluators = new MapEvaluators();

        foreach (var money in request.ListMoneyCells)
        {
            var col = money.Col;
            var suggestInColFirst = request.ListTextCellSuggests
                .Except(evaluators.TextCellMapped)
                .FirstOrDefault(x => x.Col == col);

            if (suggestInColFirst == null)
            {
                continue;
            }

            var evaluator = new MapEvaluator(suggestInColFirst, money)
            {
                MapType = MapType.MappedInColumn,
                Status = MapEvaluatorStatus.Mapped
            };
            evaluators.ListMapEvaluators.Add(evaluator);
            evaluators.MoneyCellMapped.Add(money);
            evaluators.TextCellMapped.Add(suggestInColFirst);
            Debug.WriteLine($">> {evaluator.textCellSuggest.CellValue} - {evaluator.moneyCell.Value}");
        }

        if (evaluators.ListMapEvaluators.Count != 0)
        {
            request.Result = evaluators;
            if (evaluators.ListMapEvaluators.Count == request.ListMoneyCells.Count)
            {
                request.SetHandled(true);
                return;
            }
        }
    }
}
#endregion