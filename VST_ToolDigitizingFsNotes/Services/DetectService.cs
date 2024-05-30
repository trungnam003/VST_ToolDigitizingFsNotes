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
            // regex to match heading string
            var headingMatches = DetectUtils.HeadingRegex003().Matches(cellValue);
            // append to heading list
            foreach (Match match in headingMatches.Cast<Match>())
            {
                // group name của regex heading 003
                const string SYMBOL_GROUP = "h_symbol";
                const string CONTENT_GROUP = "h_content";

                var headingSymbol = match.Groups[SYMBOL_GROUP].Value;
                var headingContent = match.Groups[CONTENT_GROUP].Value;

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

                // loop front and back data from heading cell and append to metadata
                for (int j = 0; j < cell.Row.LastCellNum; j++)
                {
                    if (j != cell.ColumnIndex)
                    {
                        var rawCellValue = cell.Row.GetCell(j)?.ToString()?.Trim();

                        if (string.IsNullOrEmpty(rawCellValue))
                        {
                            continue;
                        }
                        var model = new MatrixCellModel
                        {
                            Row = cell.RowIndex,
                            Col = j,
                            CellValue = rawCellValue
                        };
                        if (DetectUtils.MoneyRegexHard().IsMatch(rawCellValue))
                        {
                            model.CellType = MatrixCellType.Money;
                            heading.MetaData.FrontData.Add(model);
                        }
                        else if (DetectUtils.HeadingGroupRegex().IsMatch(rawCellValue))
                        {
                            model.CellType = MatrixCellType.HeadingGroup;
                            heading.MetaData.BackData.Add(model);
                        }
                    }
                }

                headings.Add(heading);
            }
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

        private void ProcessingDeterminesTheMostSuitableRange(List<MoneyCellModel> moneys, UnitOfWorkModel uow, FsNoteParentModel parent)
        {
            /// Vì nếu cột của dòng trong excel < 2 thì không thể chưa đủ số liệu
            const int MinimalAllowColumn = 2;
            const double AcceptableSimilarity = 0.6868;
            var mapParentData = _mapping[parent.FsNoteId];
            moneys.Reverse();
            foreach (var money in moneys)
            {
                var col = money.Col;
                var row = money.Row;
                var cell = uow.OcrWorkbook?.GetSheetAt(0).GetRow(row).GetCell(col);
                var lastCellNum = cell?.Row.PhysicalNumberOfCells ?? 0;
                if (lastCellNum < MinimalAllowColumn)
                {
                    continue;
                }
                var queue = FindNearestHeading(money, uow);
                if(queue.Count == 0)
                {
                    continue;
                }
                var maxSimilarity = double.MinValue;
                HeadingCellModel? nearestHeading = null;
                // dequeue heading and calculate similarity
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
                    if (maxSimilarity >= AcceptableSimilarity)
                    {
                        nearestHeading = heading;
                        goto endOfLimitRange;
                    }
                }
                var d = maxSimilarity;

                endOfLimitRange: 
                {
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
                        break;
                    }
                }
                
            }
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
            foreach (var parent in uow.FsNoteParentModels)
            {
                List<MoneyCellModel> moneys = uow.MoneyCellModels
                    .Where(money => money.Value == parent.Value)
                    .ToList();
                ProcessingDeterminesTheMostSuitableRange(moneys, uow, parent);
            }

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
    public MatrixCellModel Start { get; set; }
    public MatrixCellModel End { get; set; }

    public List<MoneyCellModel> MoneyInRange { get; set; } = [];
    public List<MatrixCellModel> TextCellInRange { get; set; } = [];
}