using NPOI.SS.UserModel;
using System.Text.RegularExpressions;
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
        private RangeDetectFsNote? ProcessingDeterminesTheMostSuitableRange(List<MoneyCellModel> moneys, UnitOfWorkModel uow, FsNoteParentModel parent)
        {
            /// Vì nếu cột của dòng trong excel < 2 thì không thể chưa đủ số liệu
            const int MinimalAllowColumn = 2;
            const double AcceptableSimilarity = 0.6868;
            _mapping.TryGetValue(parent.FsNoteId, out var mapParentData);
            // tạm thời bỏ qua các chỉ tiêu bị disable
            if (mapParentData == null || mapParentData.IsDisabled)
            {
                return null;
            }

            moneys.Reverse(); // số cuối cùng thường là số của chỉ tiêu

            foreach (var money in moneys)
            {
                var col = money.Col;
                var row = money.Row;
                var cell = uow.OcrWorkbook?.GetSheetAt(0).GetRow(row).GetCell(col);
                var lastCellNum = cell?.Row.PhysicalNumberOfCells ?? 0;
                //if (lastCellNum < MinimalAllowColumn)
                //{
                //    continue;
                //}
                // tìm kiếm heading gần nhất, độ ưu tiên là khoảng cách tăng dần
                var queue = FindNearestHeading(money, uow);
                if(queue.Count == 0)
                {
                    continue;
                }
                var maxSimilarity = double.MinValue;
                HeadingCellModel? nearestHeading = null;
                // duyệt hàng đợi và so sánh độ tương đồng với từ khóa
                while (queue.Count > 0)
                {
                    var heading = queue.Dequeue();
                    foreach (var keyword in mapParentData.Keywords)
                    {
                        var similarity = StringSimilarityUtils.CalculateSimilarity(heading?.ContentSection!, keyword);
                        if (similarity > maxSimilarity)
                        {
                            maxSimilarity = similarity;
                        }
                    }
                    // nếu độ tương đồng lớn hơn ngưỡng chấp nhận thì dừng lại
                    if (maxSimilarity >= AcceptableSimilarity)
                    {
                        nearestHeading = heading;
                        break;
                    }
                }
                var d = maxSimilarity;
                if (nearestHeading != null)
                {
                    var range = new RangeDetectFsNote
                    {
                        Start = nearestHeading,
                        End = new MatrixCellModel
                        {
                            Row = row,
                            Col = lastCellNum
                        }
                    };
                    return range;
                    //break;
                }
            }
            return null;
        }

        private static PriorityQueue<HeadingCellModel, double> FindNearestHeading(MoneyCellModel money, UnitOfWorkModel uow)
        {
            const int MaximumAllowRowRange = 30;
            // độ ưu tiên tăng dần
            var results = new PriorityQueue<HeadingCellModel, double>(Comparer<double>.Create((x, y) => x.CompareTo(y)));

            var row = money.Row;
            var col = money.Col;

            foreach (var heading in uow.HeadingCellModels)
            {
                if (row < heading.Row)
                {
                    break;
                }
                if (row - heading.Row > MaximumAllowRowRange)
                {
                    continue;
                }
                var distance = CoreUtils.EuclideanDistance(row, col, heading.Row, heading.Col);
                results.Enqueue(heading, distance);
            }
            return results;
        }

        /// <summary>
        /// Gom nhóm dữ liệu tiền, đánh giá và sắp xếp khu vực phù hợp nhất
        /// </summary>
        /// <param name="uow"></param>
        public void GroupFsNoteDataRange(UnitOfWorkModel uow)
        {
            var lst = new List<RangeDetectFsNote>();
            foreach (var parent in uow.FsNoteParentModels)
            {
                var d2 = parent.Value;
                /// Tạm thời tìm kiếm dùng giá trị tuyệt đối để tìm kiếm số tiền, nếu có lỗi thì sẽ sửa lại
                List<MoneyCellModel> moneys = uow.MoneyCellModels
                    .Where(money => Math.Abs(money.Value) == Math.Abs(parent.Value))
                    .ToList();
                var rs = ProcessingDeterminesTheMostSuitableRange(moneys, uow, parent);
                if (rs != null)
                {
                    lst.Add(rs);
                }
            }
            var a = uow.FsNoteParentModels.Where(x => x.Value !=0).ToList();
            var d = lst;

            var rate = $"{d.Count}/{a.Count} => { ((double)(d.Count / a.Count) * 100).ToString() }";
            var ddd = rate;
            //uow.FsNoteParentModels.ForEach(parent =>
            //{
            //    List<(MoneyCellModel, HeadingCellModel?)> test = new();
            //    var moneys = uow.MoneyCellModels.Where(money => money.Value == parent.Value).ToList();
            //    // loop in uow.HeadingCellModels to find nearest heading with money address
            //    moneys.ForEach(money =>
            //    {
            //        var row = money.Row;
            //        var col = money.Col;
            //        double minDistance = double.MaxValue;
            //        HeadingCellModel? nearestHeading = null;

            //        uow.HeadingCellModels.ForEach(heading =>
            //        {
            //            if (row < heading.Row)
            //            {
            //                return;
            //            }
            //            var distance = CoreUtils.EuclideanDistance(row, col, heading.Row, heading.Col);
            //            if (distance < minDistance)
            //            {
            //                minDistance = distance;
            //                nearestHeading = heading;
            //            }
            //        });
            //        test.Add((money, nearestHeading));
            //    });
            //    int maxIndex = int.MinValue;
            //    (MoneyCellModel, HeadingCellModel?)? moneySelected = null;

            //    foreach (var (money, heading) in test)
            //    {
            //        var moneyRow = money.Row;
            //        var moneyCol = money.Col;
            //        var matrixIndex = moneyRow + moneyCol;
            //        if (matrixIndex > maxIndex)
            //        {
            //            maxIndex = matrixIndex;
            //            moneySelected = (money, heading);
            //        }
            //    }
            //    var d = moneySelected;
            //});
        }
    }
}

public class RangeDetectFsNote
{
    public required MatrixCellModel Start { get; set; }
    public required MatrixCellModel End { get; set; }

    public List<MoneyCellModel> MoneyInRange { get; set; } = [];
    public List<MatrixCellModel> TextCellInRange { get; set; } = [];

    public override string ToString()
    {
        return Start.CellValue ?? "";
    }
}