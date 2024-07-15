using Force.DeepCloner;
using NPOI.SS.UserModel;
using System.Diagnostics;
using System.Text.RegularExpressions;
using VST_ToolDigitizingFsNotes.Libs.Chains;
using VST_ToolDigitizingFsNotes.Libs.Common.Enums;
using VST_ToolDigitizingFsNotes.Libs.Models;
using VST_ToolDigitizingFsNotes.Libs.Services;
using VST_ToolDigitizingFsNotes.Libs.Utils;

namespace VST_ToolDigitizingFsNotes.AppMain.Services;

/// <summary>
/// Dịch vụ xác định các thông tin trong file báo cáo
/// </summary>
public partial class DetectService : IDetectService
{
    private readonly FsNoteMapping _mapping;
    private readonly IMappingService _mappingService;
    public DetectService(FsNoteMapping mapping, IMappingService mappingService)
    {
        _mapping = mapping;
        _mappingService = mappingService;
    }
    public void DetectHeadings(string cellValue, ICell cell, ref List<HeadingCellModel> headings)
    {
        const string Dot = ".";
        // regex to match heading string
        var match = DetectUtils.HeadingRegex003().Match(cellValue);
        MatrixCellModel? combineCell = null;
        if (match.Success == false && cell.ColumnIndex == 0)
        {
            const string SPACE = " ";
            // combine 2 cells lại để xác định heading vì ocr có thể bị lỗi
            var cellValueCombine = cellValue.TrimEnd() + SPACE + cell.Row.GetCell(cell.ColumnIndex + 1)?.ToString()?.TrimStart() ?? "";
            match = DetectUtils.HeadingRegex003().Match(cellValueCombine);
            if (match.Success)
            {
                combineCell = new MatrixCellModel
                {
                    Row = cell.RowIndex,
                    Col = cell.ColumnIndex + 1,
                    CellValue = cellValueCombine
                };
            }
        }

        if (match.Success == false)
        {
            return;
        }

        const string SYMBOL_GROUP = "h_symbol";
        const string CONTENT_GROUP = "h_content";

        var headingSymbol = match.Groups[SYMBOL_GROUP].Value;
        var headingContent = match.Groups[CONTENT_GROUP].Value;

        if (headingSymbol.Trim().Equals(Dot))
        {
            /// Tạm thời fix lỗi regex (h_roman dấu . vẫn match) khi headingSymbol = "."
            return;
        }

        // remove duplicate space and special characters first to end up with a clean string
        var matches2 = DetectUtils.ManyDuplicateSpaceRegex().Matches(headingContent);
        var newHeadingContent = headingContent;

        foreach (Match match2 in matches2.Cast<Match>())
        {
            if (match2.Index > match.Groups[CONTENT_GROUP].Index)
            {
                newHeadingContent = newHeadingContent.Remove(match2.Index, newHeadingContent.Length - match2.Index);
                break;
            }
        }

        newHeadingContent = newHeadingContent.ToSimilarityCompareString();
        var heading = new HeadingCellModel
        {
            ContentSection = newHeadingContent,
            SymbolSection = headingSymbol,
            Row = cell.RowIndex,
            Col = cell.ColumnIndex,
            CellValue = $"{headingSymbol}{newHeadingContent}",
            CombineWithCell = combineCell
        };
        headings.Add(heading);
    }
    public void DetectMoneys(string cellValue, ICell cell, ref List<MoneyCellModel> moneys)
    {
        // regex to match money string 1
        var money1Matches = DetectUtils.MoneyRegex001().Matches(cellValue);
        var matchIndex1 = 0;
        var list1 = new List<MoneyCellModel>();
        foreach (Match match in money1Matches.Cast<Match>())
        {
            var money = new MoneyCellModel
            {
                Row = cell.RowIndex,
                Col = cell.ColumnIndex,
                CellValue = match.Value,
                IndexInCell = matchIndex1++
            };
            money.ConvertRawValueToValue();
            list1.Add(money);
        }

        // regex to match money string 2
        var money2Matches = DetectUtils.MoneyRegex002().Matches(cellValue);
        var matchIndex2 = 0;
        var list2 = new List<MoneyCellModel>();
        foreach (Match match in money2Matches.Cast<Match>())
        {
            var money = new MoneyCellModel
            {
                Row = cell.RowIndex,
                Col = cell.ColumnIndex,
                CellValue = match.Value,
                IndexInCell = matchIndex2++
            };
            money.ConvertRawValueToValue();
            list2.Add(money);
        }

        // regex to match money string 3 soft
        var money3Matches = DetectUtils.MoneySoftRegex001().Matches(cellValue);
        var matchIndex3 = 0;
        var list3 = new List<MoneyCellModel>();
        foreach (Match match in money3Matches.Cast<Match>())
        {
            var money = new MoneyCellModel
            {
                Row = cell.RowIndex,
                Col = cell.ColumnIndex,
                CellValue = match.Value,
                IndexInCell = matchIndex3++
            };
            money.ConvertRawValueToValue();
            list3.Add(money);
        }

        var newList = list1.Concat(list2).Concat(list3)
            .Distinct(new CompareMoneyCellModel())
            .ToList();
        moneys.AddRange(newList);
    }
    //private static TextCellSuggestModel? Test(List<FsNoteMappingModel> childMappings, string text, int i, int j)
    //{

    //    const double ZERO = 0.0;
    //    double maxSimilarity = ZERO;
    //    var cell = new TextCellSuggestModel
    //    {
    //        Row = i,
    //        Col = j,
    //        Similarity = ZERO,
    //        CellValue = text
    //    };
    //    foreach (var childMapping in childMappings)
    //    {
    //        var keywords = childMapping.Keywords;
    //        foreach (var keyword in keywords)
    //        {
    //            double currentSimilarity = StringSimilarityUtils.CalculateSimilarity(keyword, text);
    //            if (currentSimilarity > maxSimilarity)
    //            {
    //                maxSimilarity = currentSimilarity;
    //            }
    //            if (maxSimilarity >= StringSimilarityUtils.AcceptableSimilarity)
    //            {
    //                break;
    //            }
    //        }
    //        if (maxSimilarity >= StringSimilarityUtils.AcceptableSimilarity && maxSimilarity > cell.Similarity)
    //        {
    //            cell.Similarity = maxSimilarity;
    //            cell.NoteId = childMapping.Id;
    //            cell.NoteName = childMapping.Name;
    //        }
    //    }
    //    return cell.Similarity == ZERO || cell.Similarity < StringSimilarityUtils.AcceptableSimilarity ? null : cell;
    //}

    private static bool TryCheckIsFsNote(TextCellSuggestModel model, List<FsNoteMappingModel> childMappings, out double similarity)
    {
        const double ZERO = 0.0;
        similarity = ZERO;
        if (string.IsNullOrWhiteSpace(model.CellValue))
        {
            return false;
        }
        double maxSimilarity = similarity;
        int noteId = -1;
        string noteName = string.Empty;
        foreach (var childMapping in childMappings)
        {
            var keywords = childMapping.Keywords;
            foreach (var keyword in keywords)
            {
                double currentSimilarity = StringSimilarityUtils.CalculateSimilarity(keyword, model.CellValue);
                if (currentSimilarity > maxSimilarity)
                {
                    maxSimilarity = currentSimilarity;
                }
                if (maxSimilarity >= StringSimilarityUtils.AcceptableSimilarity)
                {
                    break;
                }
            }
            if (maxSimilarity >= StringSimilarityUtils.AcceptableSimilarity && maxSimilarity > similarity)
            {
                similarity = maxSimilarity;
                noteId = childMapping.Id;
                noteName = childMapping.Name;
            }
        }
        var isValid = similarity != ZERO && similarity >= StringSimilarityUtils.AcceptableSimilarity;
        if (isValid)
        {
            // set giá trị cho model
            model.NoteId = noteId;
            model.NoteName = noteName;
        }
        return isValid;
    }
}


/// <summary>
/// V2 test
/// </summary>
public partial class DetectService
{
    public void StartDetectFsNotesAsync(UnitOfWorkModel uow, CancellationToken cancellationToken = default)
    {
        foreach (var parent in uow.FsNoteParentModels)
        {
            Debug.WriteLine("============================ Bắt đầu ==============================");
            Debug.WriteLine($"Xử lý chỉ tiêu: {parent.Name}");

            var parentValue = (parent.Value);
            List<MoneyCellModel> listMoneysEqualParentValue = uow.MoneyCellModels
               .Where(money => Math.Abs(money.Value) == Math.Abs(parent.Value))
               .Reverse() // số cuối cùng thường là số của chỉ tiêu, vì đọc từ trên xuống dưới nên cần đảo ngược lại
               .ToList();

            var fsNoteDataMap = new FsNoteDataMap
            {
                FsNoteId = parent.FsNoteId,
                Group = parent.Group,
                FsNoteParentModel = parent,
            };

            HandleDetectFsNoteParentAsync(uow, parent, fsNoteDataMap, listMoneysEqualParentValue);

            _mappingService.MapFsNoteWithMoney(uow, fsNoteDataMap);
            Debug.WriteLine("============================ Kết thúc ============================\n");

            parent.Children.ForEach(x =>
            {
                fsNoteDataMap.Result.TryGetValue(x.FsNoteId, out var fsNote);
                if (fsNote != null)
                {
                    x.Value = fsNote.Value;
                    x.Values.AddRange(fsNote.Values);
                }
            });
        }
    }

    /// <summary>
    /// Tìm kiếm các khu vực phù hợp cho chỉ tiêu cha để xác định các chỉ tiêu con
    /// 1. Tìm khu vực phù hợp nhất cho chỉ tiêu cha
    /// 2. Tìm các số tiền
    /// 3. Tìm các chỉ tiêu con
    /// => Đưa dữ liệu vào dataMap, để tiến hành map dữ liệu
    /// </summary>
    /// <param name="uow"></param>
    /// <param name="parent"></param>
    /// <param name="dataMap"></param>
    /// <param name="moneys"></param>
    private void HandleDetectFsNoteParentAsync(UnitOfWorkModel uow, FsNoteParentModel parent, FsNoteDataMap dataMap, List<MoneyCellModel> moneys)
    {
        var results = new List<RangeDetectFsNote>();
        _mapping.TryGetValue(parent.FsNoteId, out var mapParentData);
        if (mapParentData == null || mapParentData.IsDisabled)
        {
            Debug.WriteLine("=> bỏ qua xử lý");
            return;
        }

        parent.Children.ForEach(x =>
        {
            dataMap.Result.Add(x.FsNoteId, x.DeepClone());
        });

        var otherAll = _mapping[parent.FsNoteId].Children[parent.Group - 1]
            .Where(x => x.OtherType == MappingOtherType.All)
            .FirstOrDefault();
        dataMap.OtherFsNoteId = otherAll == null ? -1 : otherAll.Id;

        if (!dataMap.HasOtherFsNoteId)
        {
            var otherPos = _mapping[parent.FsNoteId].Children[parent.Group - 1]
                .Where(x => x.OtherType == MappingOtherType.Positive)
                .FirstOrDefault();

            dataMap.PosOtherFsNoteId = otherPos == null ? -1 : otherPos.Id;

            var otherNeg = _mapping[parent.FsNoteId].Children[parent.Group - 1]
                .Where(x => x.OtherType == MappingOtherType.Negative)
                .FirstOrDefault();

            dataMap.NegOtherFsNoteId = otherNeg == null ? -1 : otherNeg.Id;
        }

        RangeDetectFsNote? prevRangeSpecified = null;
        foreach (var money in moneys)
        {
            //check current money is already in prev range
            //var isMoneyNear = prevRangeSpecified != null && Math.Abs(prevRangeSpecified.MoneyCellModel.Row - money.Row ) < 3; // khoảng cách giữa 2 số tiền liên tiếp gần nhau
            //if (isMoneyNear && (prevRangeSpecified != null && prevRangeSpecified.IsMoneyInThisRange(money)))
            //{
            //    continue;
            //}
            DetectRangeChainRequest request = new(uow, parent, money);
            // Xác định khu vực phù hợp nhất cho chỉ tiêu dựa trên heading
            var handler1 = new DetectUsingHeadingHandler(mapParentData);
            // Xác định khu vực phù hợp nhất cho chỉ tiêu dựa trên sự tương đồng
            var handler2 = new DetectUsingSimilartyStringHanlder(mapParentData);
            // Xác định khu vực khi chỉ tiêu cha bị OCR lỗi gộp vào 1 ô
            var handler3 = new DetectUsingDiffMatchPatchStringHandler(mapParentData);

            handler1.SetNext(handler2);
            handler2.SetNext(handler3);
            handler1.Handle(request);

            if (request.Result == null || !request.Handled)
            {
                continue;
            }
            var result = request.Result;
            result.DetectRangeStatus = DetectRangeStatus.AllowNextHandle;
            results.Add(result);
            prevRangeSpecified = result;
        }

        dataMap.RangeDetectFsNotes = results.Count > 0 ? results : null;
        dataMap.MapStatus = results.Count > 0 ? MapFsNoteStatus.CanMap : MapFsNoteStatus.NotYetMapped;

        if (dataMap.RangeDetectFsNotes != null && dataMap.RangeDetectFsNotes.Count > 0 && dataMap.MapStatus == MapFsNoteStatus.CanMap)
        {
            foreach (var range in dataMap.RangeDetectFsNotes)
            {
                ProcessingDetectMoneyInRange(range, dataMap, uow);
                ProcessingDetectChildrentFsNotesInRange(range, dataMap, uow);
            }
            if (dataMap.RangeDetectFsNotes.Count == dataMap.RangeDetectFsNotes.Count(x => x.DetectRangeStatus == DetectRangeStatus.RequireDetectAgain))
            {
                dataMap.MapStatus = MapFsNoteStatus.RequireMapAgain;
            }

            //_mappingService.MapFsNoteWithMoney(uow, dataMap);
        }
    }

    private void ProcessingDetectMoneyInRange(RangeDetectFsNote range, FsNoteDataMap dataMap, UnitOfWorkModel uow)
    {
        var moneyCellTarget = range.MoneyCellModel;
        var moneysInRange = uow.MoneyCellModels
                .Where(x => x.Row >= range.Start.Row && x.Row <= range.End.Row)
                // không duyệt số tiền của chỉ tiêu cha, operator != đã được custom lại
                .Where(x => x != moneyCellTarget)
                .ToList();

        if(uow.CalculationUnit == CalculationUnit.Million)
        {
            moneysInRange.AddRange(AddMoreMillionMoney(range.Start.Row, range.End.Row, uow));
        }

        var request = new SpecifyMoneyInRangeEqualWithParentRequest(uow, dataMap);
        var handler1 = new SpecifyMoneyInRangeEqualWithParentHandle(moneysInRange, moneyCellTarget);
        var handler2 = new SpecifyAllMoneyInRangeHandle(moneysInRange, moneyCellTarget);

        handler1.SetNext(handler2);
        handler1.Handle(request);

        if (request.Result != null && request.Handled)
        {
            range.MoneyResults = request.Result;
            // debug log

            range.MoneyResults.LogToDebug();
        }
        else
        {
            range.MoneyResults = null;
            range.DetectRangeStatus = DetectRangeStatus.RequireDetectAgain;
        }
    }

    private List<MoneyCellModel> AddMoreMillionMoney(int start, int end, UnitOfWorkModel uow)
    {
        var results = new List<MoneyCellModel>();

        var sheet = uow.GetOcrSheet();
        if (sheet == null)
        {
            return results;
        }
        // loop through all rows in range
        for (int i = start; i < end; i++)
        {
            var row = sheet.GetRow(i);
            if (row == null)
            {
                continue;
            }
            // loop through all cells in row
            for (int j = 0; j < row.LastCellNum; j++)
            {
                var cell = row.GetCell(j);
                if (cell == null)
                {
                    continue;
                }
                var cellValue = cell.ToString();
                if (string.IsNullOrWhiteSpace(cellValue))
                {
                    continue;
                }
                var match = DetectUtils.MoneyLessThan1000Regex().Match(cellValue);
                var indexInCell = 0;

                if (match.Success == false)
                {
                    continue;
                }

                var money = new MoneyCellModel
                {
                    Row = i,
                    Col = j,
                    CellValue = match.Value,
                    IndexInCell = indexInCell++,
                };
                money.ConvertRawValueToValue();
                results.Add(money);

                //foreach (Match item in match)
                //{
                //    var money = new MoneyCellModel
                //    {
                //        Row = i,
                //        Col = j,
                //        CellValue = item.Value,
                //        IndexInCell = indexInCell ++,
                //    };
                //    money.ConvertRawValueToValue();
                //    results.Add(money);
                //}
            }
        }

        return results;
    }

    private void ProcessingDetectChildrentFsNotesInRange(RangeDetectFsNote range, FsNoteDataMap dataMap, UnitOfWorkModel uow)
    {
        _mapping.TryGetValue(dataMap.FsNoteId, out var currentMapping);
        if (currentMapping == null)
        {
            return;
        }
        var childrentMappings = _mappingService.GetChildrenMappingList(dataMap.FsNoteId, dataMap.Group, uow.StockCode);

        if(childrentMappings == null || childrentMappings.Count == 0)
        {
            Debug.WriteLine("=> Không có chỉ tiêu trong file mapping");
            return;
        }

        if (dataMap.RangeDetectFsNotes == null || dataMap.RangeDetectFsNotes.Count == 0)
        {
            return;
        }
        var workbook = uow.OcrWorkbook;
        List<TextCellSuggestModel> textCellSuggestModels = [];
        if (range.DetectRangeStatus != DetectRangeStatus.AllowNextHandle)
        {
            return;
        }

        var startRow = range.Start.Row;
        var endRow = range.End.Row;

        for (int i = startRow; i <= endRow; i++)
        {
            IRow? row = workbook?.GetSheetAt(0).GetRow(i);
            IRow? bottomRow = i < endRow ? workbook?.GetSheetAt(0).GetRow(i + 1) : null;
            if (row == null)
            {
                continue;
            }

            for (int j = 0; j < row.LastCellNum; j++)
            {

                var cell = row.GetCell(j);
                var cellValue = cell?.ToString()?.ToSimilarityCompareString();
                var isIgnoreCell = range.IsIgnoreCell(i, j);

                if (isIgnoreCell || cell == null || string.IsNullOrWhiteSpace(cellValue))
                {
                    range.AddCellToIgnore(i, j);
                    continue;
                }

                var mergeCells = TryCheckCellChildIsFsNote2_v2(cell, childrentMappings);
                if (mergeCells != null && mergeCells.Count > 0)
                {
                    textCellSuggestModels.AddRange(mergeCells);
                    range.AddCellToIgnore(i, j);
                    continue;
                }

                var cellSuggest = TryCheckCellChildIsFsNote1_v2(bottomRow, cell, childrentMappings);
                if (cellSuggest != null)
                {
                    textCellSuggestModels.Add(cellSuggest);
                    range.AddCellToIgnore(cellSuggest.Row, cellSuggest.Col);

                    if (cellSuggest.CombineWithCell != null)
                    {
                        range.AddCellToIgnore(cellSuggest.CombineWithCell.Row, cellSuggest.CombineWithCell.Col);
                    }
                    continue;
                }
            }
        }
        if (textCellSuggestModels.Count > 0)
        {
            range.ListTextCellSuggestModels = textCellSuggestModels;
            // log to debug
            range.ListTextCellSuggestModels.LogToDebug();
        }
        else
        {
            range.DetectRangeStatus = DetectRangeStatus.RequireDetectAgain;
        }
    }
    private static TextCellSuggestModel? TryCheckCellChildIsFsNote1_v2(IRow? bottomRow, ICell cell, List<FsNoteMappingModel> childrentMappings)
    {
        TextCellSuggestModel? result = null;
        var bottomCell = bottomRow?.GetCell(cell.ColumnIndex) ?? null;
        var combineCell = CoreUtils.TryGetCombineCell(cell, bottomCell);
        var currCellSuggest = new TextCellSuggestModel
        {
            Row = cell.RowIndex,
            Col = cell.ColumnIndex,
            CellValue = cell.ToString()?.ToSimilarityCompareString() ?? "",
        };

        var isValidFsNote = TryCheckIsFsNote(currCellSuggest, childrentMappings, out var similarity);
        double similarityCombine = 0.0;
        var isValidFsNoteCombine = false;
        if (combineCell != null)
        {
            isValidFsNoteCombine = TryCheckIsFsNote(combineCell, childrentMappings, out var _similarity);
            similarityCombine = _similarity;
        }

        if (isValidFsNote && isValidFsNoteCombine)
        {
            if (similarity > similarityCombine)
            {
                result = currCellSuggest;
                result.Similarity = similarity;
            }
            else
            {
                result = combineCell!;
                result.CellStatus = CellStatus.Combine;
                result.Similarity = similarityCombine;
            }
        }
        else if (isValidFsNote)
        {
            result = currCellSuggest;
            result.Similarity = similarity;
        }
        else if (isValidFsNoteCombine)
        {
            result = combineCell!;
            result.CellStatus = CellStatus.Combine;
            result.Similarity = similarityCombine;
        }

        return result;
    }
    private static List<TextCellSuggestModel>? TryCheckCellChildIsFsNote2_v2(ICell cell, List<FsNoteMappingModel> childrentMappings)
    {
        var mergeCells = CoreUtils.TryGetMergeCell(cell);
        if (mergeCells == null)
            return null;
        var results = new List<TextCellSuggestModel>();

        foreach (var mergeCell in mergeCells)
        {
            // check merge cell value at least 1 space
            if (string.IsNullOrWhiteSpace(mergeCell.CellValue) || !mergeCell.CellValue.Contains(' '))
            {
                break;
            }
            var isFsNote = TryCheckIsFsNote(mergeCell, childrentMappings, out var similarity);
            if (isFsNote)
            {
                mergeCell.Similarity = similarity;
                results.Add(mergeCell);
            }
        }
        return results;

    }
   
}