using Force.DeepCloner;
using NPOI.SS.UserModel;
using System.Text.RegularExpressions;
using VST_ToolDigitizingFsNotes.Libs.Chains;
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
        public DetectService(Dictionary<int, FsNoteParentMappingModel> mapping)
        {
            _mapping = mapping;
        }
        public void DetectHeadings(string cellValue, ICell cell, ref List<HeadingCellModel> headings)
        {
            const string Dot = ".";
            // regex to match heading string
            var match = DetectUtils.HeadingRegex003().Match(cellValue);

            if (match.Success == false && cell.ColumnIndex == 0)
            {
                var cellValueCombine = cellValue + cell.Row.GetCell(cell.ColumnIndex + 1)?.ToString() ?? "";
                match = DetectUtils.HeadingRegex003().Match(cellValueCombine);
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

            newHeadingContent = newHeadingContent.RemoveSign4VietnameseString().RemoveSpecialCharacters().ToLower();
            var heading = new HeadingCellModel
            {
                ContentSection = newHeadingContent,
                SymbolSection = headingSymbol,
                Row = cell.RowIndex,
                Col = cell.ColumnIndex,
                CellValue = $"{headingSymbol}{newHeadingContent}"
            };
            headings.Add(heading);
        }

        public void DetectMoneys(string cellValue, ICell cell, ref List<MoneyCellModel> moneys)
        {
            // regex to match money string
            var moneyMatches = DetectUtils.MoneyRegex001().Matches(cellValue);
            // append to moneys list
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
        /// Thực hiện xác định khu vực phù hợp nhất cho các chỉ tiêu
        /// </summary>
        /// <param name="moneys"></param>
        /// <param name="uow"></param>
        /// <param name="parent"></param>
        private List<RangeDetectFsNote> ProcessingDeterminesTheMostSuitableRange(List<MoneyCellModel> moneys, UnitOfWorkModel uow, FsNoteParentModel parent)
        {
            var rs = new List<RangeDetectFsNote>();
            _mapping.TryGetValue(parent.FsNoteId, out var mapParentData);
            // tạm thời bỏ qua các chỉ tiêu bị disable
            if (mapParentData == null || mapParentData.IsDisabled)
            {
                return rs;
            }
            moneys.Reverse(); // số cuối cùng thường là số của chỉ tiêu
            foreach (var money in moneys)
            {
                DetectChainRequest req = new(uow, parent, money);
                var handler1 = new DetectUsingHeadingHandler(mapParentData);
                var handler2 = new DetectUsingSimilartyStringHanlder(mapParentData);
                handler1.SetNext(handler2);
                handler1.Handle(req);

                var isSuccess = req.Handled;
                var result = req.Result;
               
                if (result != null && isSuccess)
                {
                   rs.Add(result);
                }
            }
            return rs;
        }

        /// <summary>
        /// Map dữ liệu đã khoanh vùng với tiền
        /// </summary>
        /// <param name="uow"></param>
        /// <param name="ranges"></param>
        /// <param name="parent"></param>
        private void MapFsNoteChildWithMoney(UnitOfWorkModel uow, List<FsNoteDataMap> maps)
        {
            foreach(var dataMap in maps)
            {
                var lastRange = dataMap.rangeDetectFsNotes?.FirstOrDefault();
                var parent = uow.FsNoteParentModels.FirstOrDefault(x => x.FsNoteId == dataMap.FsNoteId && x.Group == dataMap.Group);
                if (lastRange == null)
                {
                    continue;
                }

                var moneyCellTarget = lastRange.MoneyCellModel;
                // get money in range with out target money

                var moneysInRange = uow.MoneyCellModels
                    .Where(x => x.Row >= lastRange.Start.Row && x.Row <= lastRange.End.Row)
                    .Where(x => !(x.Row == moneyCellTarget.Row && x.Col == moneyCellTarget.Col))
                    .ToList();

                // group moneys by row
                var groupByRow = moneysInRange.GroupBy(x => x.Row).ToDictionary(x => x.Key, x => x.ToList());
                // group moneys by col
                var groupByCol = moneysInRange.GroupBy(x => x.Col).ToDictionary(x => x.Key, x => x.ToList());

                groupByCol.TryGetValue(moneyCellTarget.Col, out var moneysCol);
                groupByRow.TryGetValue(moneyCellTarget.Row, out var moneysRow);
                // phải vét hết chứ không cộng tổng vì có nhiều bctc chưa chỉ tiêu con mô tả không liên quan
                var moneys1 = DetectUtils.FindAllSubsetSums(moneysRow ?? [], Math.Abs(parent!.Value), x => (x.Value));
                var moneys2 = DetectUtils.FindAllSubsetSums(moneysCol ?? [], Math.Abs(parent!.Value), x => (x.Value));
            }
        }

        /// <summary>
        /// Gom nhóm dữ liệu tiền, đánh giá và sắp xếp khu vực phù hợp nhất
        /// </summary>
        /// <param name="uow"></param>
        public void GroupFsNoteDataRange(UnitOfWorkModel uow)
        {
            var lst = new List<FsNoteDataMap>();
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
                var rs = ProcessingDeterminesTheMostSuitableRange(moneys, uow, parent);
                if (rs != null)
                {
                    fsNoteDataMap.rangeDetectFsNotes = rs;
                }
                lst.Add(fsNoteDataMap);
            }

            MapFsNoteChildWithMoney(uow, lst);
            //var a = uow.FsNoteParentModels.Where(x => x.Value !=0).ToList();
            //var d = lst;

            //var rate = $"{d.Count}/{a.Count} => { ((double)(d.Count / a.Count) * 100).ToString() }";
            //var ddd = rate;
        }
    }
}

