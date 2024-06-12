using NPOI.SS.UserModel;
using System.Text.RegularExpressions;
using VST_ToolDigitizingFsNotes.Libs.Chains;
using VST_ToolDigitizingFsNotes.Libs.Common.Enums;
using VST_ToolDigitizingFsNotes.Libs.Models;
using VST_ToolDigitizingFsNotes.Libs.Services;
using VST_ToolDigitizingFsNotes.Libs.Utils;

namespace VST_ToolDigitizingFsNotes.AppMain.Services
{
    /// <summary>
    /// Dịch vụ xác định các thông tin trong file báo cáo
    /// </summary>
    public class DetectService : IDetectService
    {
        private readonly Dictionary<int, FsNoteParentMappingModel> _mapping;
        private readonly IMappingService _mappingService;
        public DetectService(Dictionary<int, FsNoteParentMappingModel> mapping, IMappingService mappingService)
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

            newHeadingContent = newHeadingContent.ToSystemNomalizeString();
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
            // regex to match money string
            var moneyMatches = DetectUtils.MoneyRegex001().Matches(cellValue);
            foreach (Match match in moneyMatches.Cast<Match>())
            {
                var money = new MoneyCellModel
                {
                    Row = cell.RowIndex,
                    Col = cell.ColumnIndex,
                    CellValue = (match.Value)
                };
                money.ConvertRawValueToValue();
                moneys.Add(money);
            }
        }

        /// <summary>
        /// Thực hiện xác định khu vực phù hợp nhất cho 1 chỉ tiêu
        /// </summary>
        /// <param name="moneys"></param>
        /// <param name="uow"></param>
        /// <param name="parent"></param>
        private (List<RangeDetectFsNote>?, MapFsNoteStatus) ProcessingDeterminesTheMostSuitableRange(List<MoneyCellModel> moneys, UnitOfWorkModel uow, FsNoteParentModel parent)
        {
            var rs = new List<RangeDetectFsNote>();
            _mapping.TryGetValue(parent.FsNoteId, out var mapParentData);
            // tạm thời bỏ qua các chỉ tiêu bị disable trong file mapping
            if (mapParentData == null || mapParentData.IsDisabled)
            {
                return (null, MapFsNoteStatus.IgnoreMap);
            }
            // số cuối cùng thường là số của chỉ tiêu, vì đọc từ trên xuống dưới nên cần đảo ngược lại
            moneys.Reverse();
            RangeDetectFsNote? prevRangeSpecified = null;
            foreach (var money in moneys)
            {
                // check current money is already in prev range
                // giảm bớt các khu vực trùng lặp trong nhau
                if (prevRangeSpecified != null && prevRangeSpecified.IsMoneyInThisRange(money))
                {
                    continue;
                }

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
                //var hasExtensionKeywords = mapParentData.KeywordExtensions.Count > 0;
                //if (hasExtensionKeywords)
                //{
                //    var narrowRequest = new NarrowRangeDetectedUsingExtensionKeywordsRequest(uow, parent);
                //    var narrowHandler1 = new NarrowRangeDetectedUsingExtensionKeywordsHandler(mapParentData);

                //    narrowHandler1.Handle(narrowRequest);
                //    if (narrowRequest.Result != null && narrowRequest.Handled)
                //    {
                //        request.Result.UpdateRange(narrowRequest.Result.Start, narrowRequest.Result.End);
                //    }
                //}
                request.Result.DetectRangeStatus = DetectRangeStatus.AllowNextHandle;
                rs.Add(request.Result);
                prevRangeSpecified = request.Result;
            }

            return rs.Count == 0 ? (null, MapFsNoteStatus.NotYetMapped) : (rs, MapFsNoteStatus.CanMap);
        }

        /// <summary>
        /// Tìm các số tiền trong các khu vực đã xác định có tổng bằng chỉ tiêu cha
        /// </summary>
        /// <param name="uow"></param>
        /// <param name="ranges"></param>
        /// <param name="parent"></param>
        private void ProcessingDeterminesMoneysInRange(UnitOfWorkModel uow, FsNoteDataMap dataMap)
        {
            var noteId = dataMap.FsNoteId;
            var group = dataMap.Group;
            var parent = uow.FsNoteParentModels.FirstOrDefault(x => x.FsNoteId == noteId && x.Group == group);

            if (parent == null)
            {
                return;
            }

            if (dataMap.RangeDetectFsNotes == null || dataMap.RangeDetectFsNotes.Count == 0)
            {
                return;
            }
            foreach (var range in dataMap.RangeDetectFsNotes)
            {
                var moneyCellTarget = range.MoneyCellModel;
                var moneysInRange = uow.MoneyCellModels
                    .Where(x => x.Row >= range.Start.Row && x.Row <= range.End.Row)
                    // không duyệt số tiền của chỉ tiêu cha, operator != đã được custom lại
                    .Where(x => x != moneyCellTarget)
                    .ToList();

                var request = new SpecifyMoneyInRangeEqualWithParentRequest(uow, dataMap);
                var handler1 = new SpecifyMoneyInRangeEqualWithParentHandle(moneysInRange, moneyCellTarget);
                var handler2 = new SpecifyAllMoneyInRangeHandle(moneysInRange, moneyCellTarget);

                handler1.SetNext(handler2);
                handler1.Handle(request);

                if (request.Result != null && request.Handled)
                {
                    range.MoneyResults = request.Result;
                }
                else
                {
                    range.MoneyResults = null;
                    range.DetectRangeStatus = DetectRangeStatus.RequireDetectAgain;
                }
            }
            if (dataMap.RangeDetectFsNotes.Count == dataMap.RangeDetectFsNotes.Count(x => x.DetectRangeStatus == DetectRangeStatus.RequireDetectAgain))
            {
                dataMap.MapStatus = MapFsNoteStatus.RequireMapAgain;
            }
        }

        private void ProcessingDetectChildrentFsNotesInRange(UnitOfWorkModel uow, FsNoteDataMap dataMap)
        {
            _mapping.TryGetValue(dataMap.FsNoteId, out var currentMapping);
            if (currentMapping == null)
            {
                return;
            }
            var childrentMappings = currentMapping.Children[dataMap!.Group - 1];

            if (dataMap.RangeDetectFsNotes == null || dataMap.RangeDetectFsNotes.Count == 0)
            {
                return;
            }

            var workbook = uow.OcrWorkbook;
            foreach (var range in dataMap.RangeDetectFsNotes)
            {
                List<TextCellSuggestModel> textCellSuggestModels = [];
                if (range == null)
                {
                    continue;
                }

                if (range.DetectRangeStatus != DetectRangeStatus.AllowNextHandle)
                {
                    continue;
                }
                var startRow = range.Start.Row;
                var endRow = range.End.Row;
                //Debug.WriteLine(currentMapping.Name);
                //Debug.WriteLine($"{startRow} : {endRow}");
                var ignoreCell = new HashSet<string>();
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
                        var ignoreStartCell = (i == startRow && j == range.Start.Col)
                            && range.DetectStartRangeStatus == DetectStartRangeStatus.SkipStringSimilarity;

                        // Xử lý loại bỏ heading hoặc heading đã được kết hợp với ô khác
                        var isHeadingAndHasCombineCell = range.Start.GetType() == typeof(HeadingCellModel)
                            && ((HeadingCellModel)range.Start).CombineWithCell != null;

                        var currentCellIsCombineCell = isHeadingAndHasCombineCell
                            && ((HeadingCellModel)range.Start).CombineWithCell?.Row == i
                            && ((HeadingCellModel)range.Start).CombineWithCell?.Col == j;

                        if (ignoreStartCell || currentCellIsCombineCell)
                        {
                            continue;
                        }

                        if (ignoreCell.Contains($"{i}:{j}"))
                        {
                            continue;
                        }

                        var cell = row.GetCell(j);
                        if (cell == null)
                        {
                            continue;
                        }
                        var cellValue = cell.ToString()?.ToSimilarityCompareString();

                        if (string.IsNullOrWhiteSpace(cellValue))
                        {
                            continue;
                        }
                        var bottomCell = bottomRow?.GetCell(j) ?? null;
                        var bottomCellValue = bottomCell?.ToString();
                        var cellValueCombineWithCellValueBottom = cellValue;
                        bool allowCombine = false;
                        // Kết hợp với text phía dưới để tăng khả năng nhận diện chính xác hơn vì có chỉ tiêu xuống dòng
                        if (!string.IsNullOrWhiteSpace(bottomCellValue) && StringUtils.StartWithLower(bottomCellValue))
                        {
                            var bottomCellValueNormalize = bottomCellValue.ToSimilarityCompareString();
                            cellValueCombineWithCellValueBottom = cellValue + " " + bottomCellValueNormalize;
                            allowCombine = true;
                        }

                        var cellSuggest = Test(childrentMappings, cellValue, i, j);
                        var cellSuggestCombine = allowCombine ? Test(childrentMappings, cellValueCombineWithCellValueBottom, i, j) : null;
                        if (cellSuggestCombine != null)
                        {
                            cellSuggestCombine.CombineWithCell = new()
                            {
                                Row = i + 1,
                                Col = j,
                                CellValue = bottomCellValue ?? ""
                            };
                        }

                        var hasSuggest = cellSuggest != null || cellSuggestCombine != null;

                        if (hasSuggest)
                        {
                            if (cellSuggest != null && cellSuggestCombine != null)
                            {
                                if (cellSuggest.Similarity > cellSuggestCombine.Similarity)
                                {
                                    //Debug.WriteLine(cellSuggest);
                                    textCellSuggestModels.Add(cellSuggest);
                                }
                                else
                                {
                                    //Debug.WriteLine(cellSuggestCombine);
                                    ignoreCell.Add($"{i + 1}:{j}");
                                    textCellSuggestModels.Add(cellSuggestCombine);
                                }
                            }
                            else if (cellSuggestCombine != null)
                            {
                                //Debug.WriteLine(cellSuggestCombine);
                                ignoreCell.Add($"{i + 1}:{j}");
                                textCellSuggestModels.Add(cellSuggestCombine);
                            }
                            else if (cellSuggest != null)
                            {
                                //Debug.WriteLine(cellSuggest);
                                textCellSuggestModels.Add(cellSuggest);
                            }
                        }
                    }
                }
                //Debug.WriteLine("===================================");

                if (textCellSuggestModels.Count > 0)
                {
                    range.ListTextCellSuggestModels = textCellSuggestModels;
                }
                else
                {
                    range.DetectRangeStatus = DetectRangeStatus.RequireDetectAgain;
                }
            }
            if (dataMap.RangeDetectFsNotes.Count == dataMap.RangeDetectFsNotes.Count(x => x.DetectRangeStatus == DetectRangeStatus.RequireDetectAgain))
            {
                dataMap.MapStatus = MapFsNoteStatus.RequireMapAgain;
            }
        }

        private static TextCellSuggestModel? Test(List<FsNoteMappingModel> childMappings, string text, int i, int j)
        {

            const double ZERO = 0.0;
            double maxSimilarity = ZERO;
            var cell = new TextCellSuggestModel
            {
                Row = i,
                Col = j,
                Similarity = ZERO,
                CellValue = text
            };
            foreach (var childMapping in childMappings)
            {
                var keywords = childMapping.Keywords;
                foreach (var keyword in keywords)
                {
                    double currentSimilarity = StringSimilarityUtils.CalculateSimilarity(keyword, text);
                    if (currentSimilarity > maxSimilarity)
                    {
                        maxSimilarity = currentSimilarity;
                    }
                    if (maxSimilarity >= StringSimilarityUtils.AcceptableSimilarity)
                    {
                        break;
                    }
                }
                if (maxSimilarity >= StringSimilarityUtils.AcceptableSimilarity && maxSimilarity > cell.Similarity)
                {
                    cell.Similarity = maxSimilarity;
                    cell.NoteId = childMapping.Id;
                    cell.NoteName = childMapping.Name;
                }
            }
            return cell.Similarity == ZERO || cell.Similarity < StringSimilarityUtils.AcceptableSimilarity ? null : cell;
        }

        /// <summary>
        /// Gom nhóm dữ liệu tiền, đánh giá và sắp xếp khu vực phù hợp nhất
        /// </summary>
        /// <param name="uow"></param>
        public void GroupFsNoteDataRange(UnitOfWorkModel uow)
        {
            var listDataMap = new List<FsNoteDataMap>();
            foreach (var parent in uow.FsNoteParentModels)
            {
                var d2 = parent.Value;
                var fsNoteDataMap = new FsNoteDataMap
                {
                    FsNoteId = parent.FsNoteId,
                    Group = parent.Group,
                    FsNoteParentModel = parent
                };
                /// Tạm thời tìm kiếm dùng giá trị tuyệt đối để tìm kiếm số tiền, nếu có lỗi thì sẽ sửa lại
                List<MoneyCellModel> moneys = uow.MoneyCellModels
                    .Where(money => Math.Abs(money.Value) == Math.Abs(parent.Value))
                    .ToList();

                var (result, status) = ProcessingDeterminesTheMostSuitableRange(moneys, uow, parent);

                if (result != null)
                {
                    fsNoteDataMap.RangeDetectFsNotes = result;
                }
                fsNoteDataMap.MapStatus = status;
                listDataMap.Add(fsNoteDataMap);
            }

            foreach (var dataMap in listDataMap)
            {
                if (dataMap.RangeDetectFsNotes != null
                    && dataMap.RangeDetectFsNotes.Count > 0
                    && dataMap.MapStatus == MapFsNoteStatus.CanMap)
                {
                    ProcessingDeterminesMoneysInRange(uow, dataMap);
                    if (dataMap.MapStatus == MapFsNoteStatus.CanMap)
                    {
                        ProcessingDetectChildrentFsNotesInRange(uow, dataMap);
                    }
                }
            }

            var canMapList = listDataMap.Where(x => x.MapStatus == MapFsNoteStatus.CanMap).ToList();
            foreach (var dataMap in canMapList)
            {
                _mappingService.MapFsNoteWithMoney(uow, dataMap);
            }
        }
    }
}

