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

                newHeadingContent = newHeadingContent.RemoveSign4VietnameseString().RemoveSpecialCharacters();
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

        public void GroupFsNoteDataRange(UnitOfWorkModel model)
        {
            model.FsNoteParentModels.ForEach(parent =>
            {
                List<(MoneyCellModel, HeadingCellModel?)> test = new();
                var moneys = model.MoneyCellModels.Where(money => money.Value == parent.Value).ToList();
                // loop in model.HeadingCellModels to find nearest heading with money address
                moneys.ForEach(money =>
                {
                    var row = money.Row;
                    var col = money.Col;
                    double minDistance = double.MaxValue;
                    HeadingCellModel? nearestHeading = null;

                    model.HeadingCellModels.ForEach(heading =>
                    {
                        if (row < heading.Row)
                        {
                            return;
                        }
                        var distance = CoreUtils.EuclideanDistance(row, col, heading.Row, heading.Col);
                        if (distance < minDistance)
                        {
                            minDistance = distance;
                            nearestHeading = heading;
                        }
                    });
                    test.Add((money, nearestHeading));
                });
                int maxIndex = int.MinValue;
                (MoneyCellModel, HeadingCellModel?)? moneySelected = null;

                foreach (var (money, heading) in test)
                {
                    var moneyRow = money.Row;
                    var moneyCol = money.Col;
                    var matrixIndex = moneyRow + moneyCol;
                    if (matrixIndex > maxIndex)
                    {
                        maxIndex = matrixIndex;
                        moneySelected = (money, heading);
                    }
                }
                var d = moneySelected;
            });
        }
    }
}
