using Force.DeepCloner;
using System.Diagnostics;
using VST_ToolDigitizingFsNotes.Libs.Models;
using VST_ToolDigitizingFsNotes.Libs.Utils;

namespace VST_ToolDigitizingFsNotes.Libs.Chains;

public class MapFsNoteWithMoneyChainRequest : ChainBaseRequest<MapEvaluators>
{
    public List<TextCellSuggestModel> ListTextCellSuggests { get; init; }
    public List<MoneyCellModel> ListMoneyCells { get; init; }
    public MapFsNoteWithMoneyChainRequest(List<TextCellSuggestModel> listTextCellSuggests, List<MoneyCellModel> listMoneyCells)
    {
        ListTextCellSuggests = listTextCellSuggests;
        ListMoneyCells = listMoneyCells;
    }
}

public enum MapEvaluatorType
{
    None,
    MappedInRow,
    MappedInColumn
}

public class MapEvaluator
{
    public TextCellSuggestModel textCellSuggest { get; init; }
    public MoneyCellModel moneyCell { get; init; }

    public MapEvaluatorType MapEvaluatorType { get; set; } = MapEvaluatorType.None;

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
    public List<MoneyCellModel>? RemainMoneys { get; set; }
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

        // Map trên cùng 1 hàng
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
                MapEvaluatorType = MapEvaluatorType.MappedInRow,
            };
            evaluators.ListMapEvaluators.Add(evaluator);
            evaluators.MoneyCellMapped.Add(money);
            evaluators.TextCellMapped.Add(rowWithRow);
            Debug.WriteLine($">> {evaluator.textCellSuggest.CellValue} - {evaluator.moneyCell.Value}");
        }

        // Map trên cùng 1 hàng khi có các cell bị merge
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
                MapEvaluatorType = MapEvaluatorType.MappedInRow,
            };
            evaluators.ListMapEvaluators.Add(evaluator);
            evaluators.MoneyCellMapped.Add(money);
            evaluators.TextCellMapped.Add(rowWithRowMerge);
            Debug.WriteLine($"M>> {evaluator.textCellSuggest.CellValue} - {evaluator.moneyCell.Value}");
        }

        // Map trên cùng 1 hàng khi có các cell bị merge và có nhiều cell
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
                        MapEvaluatorType = MapEvaluatorType.MappedInRow,
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
                                MapEvaluatorType = MapEvaluatorType.MappedInRow,
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

        if (evaluators.ListMapEvaluators.Count == 0)
        {
            request.Result = null;
            _nextChain?.Handle(request);
        }
        else
        {
            request.SetHandled(true);
            request.Result.RemainMoneys = request.ListMoneyCells.Except(evaluators.MoneyCellMapped).ToList();
        }
    }
}
/// <summary>
/// Map khi có lỗi xuất hiện khi OCR line break
/// </summary>
public class MapWhenOcrLineBreakErrorHandler : HandleChainBase<MapFsNoteWithMoneyChainRequest>
{
    public UnitOfWorkModel Uow { get; init; }
    public RangeDetectFsNote Range { get; init; }
    public FsNoteDataMap DataMap { get; init; }
    public MapWhenOcrLineBreakErrorHandler(UnitOfWorkModel uow, RangeDetectFsNote range, FsNoteDataMap dataMap)
    {
        Uow = uow;
        Range = range;
        DataMap = dataMap;
    }
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
                    MapEvaluatorType = MapEvaluatorType.MappedInRow,
                };
                evaluators.ListMapEvaluators.Add(evaluator);
                evaluators.MoneyCellMapped.Add(request.ListMoneyCells[i]);
                evaluators.TextCellMapped.Add(request.ListTextCellSuggests[i]);
                Debug.WriteLine($"(e1)>> {evaluator.textCellSuggest.CellValue} - {evaluator.moneyCell.Value}");
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

            var rs = CountFsNoteNameWithInRange(start, end, request);
            if (rs.Count > 0)
            {
                draftCells.AddRange(rs);
            }
            draftCells.Sort(TextCellSuggestModel.Comparer);
            if (request.ListMoneyCells.Count == draftCells.Count)
            {
                for (int i = 0; i < request.ListMoneyCells.Count; i++)
                {
                    var evaluator = new MapEvaluator(draftCells[i], request.ListMoneyCells[i])
                    {
                        MapEvaluatorType = MapEvaluatorType.MappedInRow,
                    };
                    evaluators.ListMapEvaluators.Add(evaluator);
                    evaluators.MoneyCellMapped.Add(request.ListMoneyCells[i]);
                    evaluators.TextCellMapped.Add(draftCells[i]);
                    Debug.WriteLine($"(e2)>> {evaluator.textCellSuggest.CellValue} - {evaluator.moneyCell.Value}");
                }
            }
            else if (request.ListMoneyCells.Count > draftCells.Count)
            {
                // Mở rộng vùng để map
                var remain = request.ListMoneyCells.Count - draftCells.Count;

                // tìm xuống dưới
                int startBottom = draftCells.Last().Row + 1;
                int endBottom = Range.End.Row;
                var cellBottom = SeekBottomToGetCellSuggest(startBottom, endBottom, 0);
                if (cellBottom != null)
                {
                    draftCells.Add(cellBottom);
                    remain -= 1;
                }

                if (remain == 0)
                {
                    draftCells.Sort(TextCellSuggestModel.Comparer);
                    for (int i = 0; i < request.ListMoneyCells.Count; i++)
                    {
                        var evaluator = new MapEvaluator(draftCells[i], request.ListMoneyCells[i])
                        {
                            MapEvaluatorType = MapEvaluatorType.MappedInRow,
                        };
                        evaluators.ListMapEvaluators.Add(evaluator);
                        evaluators.MoneyCellMapped.Add(request.ListMoneyCells[i]);
                        evaluators.TextCellMapped.Add(draftCells[i]);
                        Debug.WriteLine($"(ee2)>> {evaluator.textCellSuggest.CellValue} - {evaluator.moneyCell.Value}");
                    }
                    if (evaluators.ListMapEvaluators.Count == 0)
                    {
                        request.Result = null;
                        request.SetHandled(false);
                    }
                    else if (evaluators.ListMapEvaluators.Count == request.ListMoneyCells.Count)
                    {
                        request.SetHandled(true);
                        return;
                    }
                    return;
                }

                // tìm lên trên
                int startTop = draftCells.First().Row - 1;
                int endTop = Range.Start.Row;
                var cellTop = SeekTopToGetCellSuggest(startTop, endTop, 0);

                if (cellTop != null)
                {
                    draftCells.Add(cellTop);
                    remain -= 1;
                }
                if (remain == 0)
                {
                    draftCells.Sort(TextCellSuggestModel.Comparer);
                    for (int i = 0; i < request.ListMoneyCells.Count; i++)
                    {
                        var evaluator = new MapEvaluator(draftCells[i], request.ListMoneyCells[i])
                        {
                            MapEvaluatorType = MapEvaluatorType.MappedInRow,
                        };
                        evaluators.ListMapEvaluators.Add(evaluator);
                        evaluators.MoneyCellMapped.Add(request.ListMoneyCells[i]);
                        evaluators.TextCellMapped.Add(draftCells[i]);
                        Debug.WriteLine($"(ee2)>> {evaluator.textCellSuggest.CellValue} - {evaluator.moneyCell.Value}");
                    }
                    if (evaluators.ListMapEvaluators.Count == 0)
                    {
                        request.Result = null;
                        request.SetHandled(false);
                    }
                    else if (evaluators.ListMapEvaluators.Count == request.ListMoneyCells.Count)
                    {
                        request.SetHandled(true);
                        return;
                    }
                    return;
                }
            }
        }

        if (evaluators.ListMapEvaluators.Count == 0)
        {
            request.Result = null;
            request.SetHandled(false);
        }
        else if (evaluators.ListMapEvaluators.Count == request.ListMoneyCells.Count)
        {
            request.SetHandled(true);
            return;
        }
    }

    private TextCellSuggestModel? SeekTopToGetCellSuggest(int startIndex, int endIndex, int col)
    {
        const int maxSeed = 2;
        var sheet = Uow.GetOcrSheet() ?? throw new ArgumentNullException();
        int count = 0;

        for (int i = startIndex; i >= endIndex; i--)
        {
            if (count >= maxSeed)
            {
                return null;
            }
            var cell = sheet.GetRow(i)?.GetCell(col);
            var isValidCell = CoreUtils.TryGetCellValue(cell, out string cellValue);
            if (isValidCell == false)
            {
                count++;
                continue;
            }

            var hasCombine = StringUtils.StartWithLower(cellValue);
            if (hasCombine)
            {
                var aboveCell = sheet.GetRow(i - 1)?.GetCell(col);
                var isValidAboveCell = CoreUtils.TryGetCellValue(aboveCell, out string cellAboveValue);
                hasCombine = hasCombine && isValidAboveCell && StringUtils.StartWithUpper(cellAboveValue);

                if (hasCombine)
                {
                    var combineCell = CoreUtils.TryGetCombineCell(aboveCell!, cell);
                    return combineCell;
                }
            }
            else
            {
                var normalCell = new TextCellSuggestModel()
                {
                    Row = i,
                    Col = col,
                    CellValue = cellValue,
                    CellStatus = CellStatus.Default
                };
                return normalCell;
            }
        }
        return null;
    }

    private TextCellSuggestModel? SeekBottomToGetCellSuggest(int startIndex, int endIndex, int col)
    {
        const int maxSeed = 2;
        int count = 0;
        var sheet = Uow.GetOcrSheet() ?? throw new ArgumentNullException();
        for (int i = startIndex; i <= endIndex; i++)
        {
            if (count >= maxSeed)
            {
                return null;
            }
            var cell = sheet.GetRow(i)?.GetCell(col);
            var isValidCell = CoreUtils.TryGetCellValue(cell, out string cellValue);
            if (isValidCell == false)
            {
                count++;
                continue;
            }
            var combineCell = CoreUtils.TryGetCombineCell(cell!, sheet.GetRow(i + 1)?.GetCell(col));
            if (combineCell != null)
            {
                return combineCell;
            }

            var normalCell = new TextCellSuggestModel()
            {
                Row = i,
                Col = col,
                CellValue = cellValue,
                CellStatus = CellStatus.Default
            };
            return normalCell;
        }
        return null;
    }

    private List<TextCellSuggestModel> CountFsNoteNameWithInRange(int startRow, int endRow, MapFsNoteWithMoneyChainRequest req)
    {
        var rowDetected = new HashSet<int>();
        var cols = new List<int>();
        foreach (var item in req.ListTextCellSuggests)
        {
            cols.Add(item.Col);
            rowDetected.Add(item.Row);
            if (item.CombineWithCell != null)
            {
                rowDetected.Add(item.CombineWithCell.Row);
            }
        }
        var colIndex = GetMostFreq(cols);
        var workbook = Uow.OcrWorkbook ?? throw new ArgumentNullException();
        // Kiểm tra bên trong có thể có chỉ tiêu khác hay không
        var newList = new List<TextCellSuggestModel>();
        for (int i = startRow; i <= endRow; i++)
        {
            if (rowDetected.Contains(i))
                continue;

            var sheet = workbook.GetSheetAt(0) ?? throw new ArgumentNullException();
            var row = sheet.GetRow(i);
            var cell = row?.GetCell(colIndex);
            if (row == null || cell == null || string.IsNullOrEmpty(cell?.ToString()))
                continue;

            var mergeCells = CoreUtils.TryGetMergeCell(cell);
            if (mergeCells != null)
            {
                newList.AddRange(mergeCells);
                continue;
            }

            var combineCell = CoreUtils.TryGetCombineCell(cell, sheet?.GetRow(i + 1)?.GetCell(colIndex));
            if (combineCell != null)
            {
                newList.Add(combineCell);
                i++;
                continue;
            }

            var normalCell = new TextCellSuggestModel()
            {
                Row = i,
                Col = colIndex,
                CellValue = cell.ToString(),
                CellStatus = CellStatus.Default
            };
            newList.Add(normalCell);
        }

        return newList;
    }

    private static int GetMostFreq(List<int> list)
    {
        var elementCounts = new Dictionary<int, int>();

        foreach (var element in list)
        {
            if (elementCounts.TryGetValue(element, out int value))
            {
                elementCounts[element] = ++value;
            }
            else
            {
                elementCounts[element] = 1;
            }
        }
        int maxCount = 0;
        int mostFrequentElement = list[0];

        foreach (var pair in elementCounts)
        {
            if (pair.Value > maxCount)
            {
                maxCount = pair.Value;
                mostFrequentElement = pair.Key;
            }
        }

        return mostFrequentElement;
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
                MapEvaluatorType = MapEvaluatorType.MappedInColumn,
            };
            evaluators.ListMapEvaluators.Add(evaluator);
            evaluators.MoneyCellMapped.Add(money);
            evaluators.TextCellMapped.Add(suggestInColFirst);
            Debug.WriteLine($">> {evaluator.textCellSuggest.CellValue} - {evaluator.moneyCell.Value}");
        }

        if (evaluators.ListMapEvaluators.Count != 0)
        {
            request.Result = evaluators;
            request.SetHandled(true);
            request.Result.RemainMoneys = request.ListMoneyCells.Except(evaluators.MoneyCellMapped).ToList();
            if (evaluators.ListMapEvaluators.Count == request.ListMoneyCells.Count)
            {
                //request.SetHandled(true);
                return;
            }
        }
    }
}
#endregion