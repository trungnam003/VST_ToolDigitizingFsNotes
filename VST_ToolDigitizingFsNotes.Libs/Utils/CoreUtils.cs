using NPOI.SS.UserModel;
using System.Drawing;
using VST_ToolDigitizingFsNotes.Libs.Models;

namespace VST_ToolDigitizingFsNotes.Libs.Utils
{
    public static class CoreUtils
    {
        public static double EuclideanDistance(int x1, int y1, int x2, int y2)
        {
            return Math.Sqrt(Math.Pow(x2 - x1, 2) + Math.Pow(y2 - y1, 2));
        }

        public static double EuclideanDistance(double x1, double y1, double x2, double y2)
        {
            return Math.Sqrt(Math.Pow(x2 - x1, 2) + Math.Pow(y2 - y1, 2));
        }

        public static int ManhattanDistance(int x1, int y1, int x2, int y2)
        {
            return Math.Abs(x2 - x1) + Math.Abs(y2 - y1);
        }
        public static int ChebyshevDistance(int x1, int y1, int x2, int y2)
        {
            return Math.Max(Math.Abs(x2 - x1), Math.Abs(y2 - y1));
        }
        /// <summary>
        /// Tìm số gần đúng nhất với số target trong list
        /// </summary>
        /// <typeparam name="T">Kiểu dữ liệu numeric</typeparam>
        /// <param name="target"></param>
        /// <param name="list"></param>
        /// <param name="diff">Sự khác nhau tôi thiểu</param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        //public static (T, bool) FindClosestNumber<T>(T target, List<T> list, T diff)
        //    where T : IComparable<T>
        //{
        //    if (IsNumericType(typeof(T)) == false)
        //        throw new ArgumentException("Type must be numeric type.");

        //    T closestNumber = list[0];
        //    bool isFound = false;
        //    dynamic minDifference = Math.Abs(Convert.ToDouble(list[0]) - Convert.ToDouble(target));

        //    foreach (var number in list)
        //    {
        //        dynamic difference = Math.Abs(Convert.ToDouble(number) - Convert.ToDouble(target));
        //        if (difference < minDifference && difference <= diff)
        //        {
        //            minDifference = difference;
        //            closestNumber = number;
        //            isFound = true;
        //        }
        //    }
        //    return (closestNumber, isFound);
        //}

        public static (double, bool) FindClosestNumber(double target, List<double> list, double diff)
        {
            double closestNumber = list[0];
            bool isFound = false;
            double minDifference = Math.Abs(list[0] - target);

            foreach (var number in list)
            {
                double difference = Math.Abs(number - target);
                if (difference < minDifference && difference <= diff)
                {
                    minDifference = difference;
                    closestNumber = number;
                    isFound = true;
                }
            }

            if (!isFound && closestNumber == list[0] && minDifference < diff)
            {
                isFound = true;
            }

            return (closestNumber, isFound);
        }

        public static (long, bool) FindClosestNumber(long target, List<long> list, long diff)
        {
            long closestNumber = list[0];
            bool isFound = false;
            long minDifference = Math.Abs(list[0] - target);

            foreach (var number in list)
            {
                long difference = Math.Abs(number - target);
                if (difference < minDifference && difference <= diff)
                {
                    minDifference = difference;
                    closestNumber = number;
                    isFound = true;
                }
            }
            return (closestNumber, isFound);
        }

        public static bool IsNumericType(Type type)
        {
            return type == typeof(int) || type == typeof(long) || type == typeof(float) || type == typeof(double) || type == typeof(decimal);
        }

        // create a method check color in range of color
        public static bool IsColorInRange(Color color, Color targetColor, int rangeRed, int rangeBlue, int rangeGreen)
        {
            return Math.Abs(color.R - targetColor.R) <= rangeRed
                && Math.Abs(color.G - targetColor.G) <= rangeGreen
                && Math.Abs(color.B - targetColor.B) <= rangeBlue;
        }

        // in range red with default range of blue and green
        public static bool IsColorInRangeRed(Color targetColor, int rangeRed = 200)
        {
            return IsColorInRange(Color.Red, targetColor, rangeRed, 50, 50);
        }

        /// <summary>
        /// Tính phương sai của một list số nguyên
        /// </summary>
        /// <param name="numbers"></param>
        /// <returns></returns>
        public static double CalculateVariance(List<int> numbers)
        {
            double mean = numbers.Average();
            return numbers.Average(v => Math.Pow(v - mean, 2));
        }

        /// <summary>
        /// Xác định các phần tử có hướng theo hàng hay cột dựa vào phương sai tọa độ hàng và cột
        /// </summary>
        /// <param name="elements"></param>
        /// <returns>1 theo hàng; 2 theo cột; 0 không biết</returns>
        public static Direction DetermineDirection(List<(int, int)> elements)
        {
            List<int> rows = elements.Select(e => e.Item1).ToList();
            List<int> cols = elements.Select(e => e.Item2).ToList();
            double rowVariance = CalculateVariance(rows);
            double colVariance = CalculateVariance(cols);

            if (rowVariance < colVariance)
            {
                return Direction.Row;
            }
            else if (colVariance < rowVariance)
            {
                return Direction.Column;
            }
            else
            {
                return Direction.Unknown;
            }
        }

        public enum Direction
        {
            Row = 1,
            Column = 2,
            Unknown = 0
        }

        /// <summary>
        /// Tính góc giữa 2 điểm với góc 0 là trục x dương
        /// </summary>
        /// <param name="x1"></param>
        /// <param name="y1"></param>
        /// <param name="x2"></param>
        /// <param name="y2"></param>
        /// <returns></returns>
        public static double CalculateAngle(int x1, int y1, int x2, int y2)
        {
            // Tính các thành phần của vector
            int dx = x2 - x1;
            int dy = y2 - y1;

            // Tính góc bằng arctan và chuyển từ radian sang độ
            double angleRad = Math.Atan2(dy, dx);
            double angleDeg = angleRad * (180.0 / Math.PI);

            return angleDeg;
        }


        public static List<ICell>? GetListCellInMergeCell(this ICell cell)
        {
            if (cell.IsMergedCell)
            {
                var sheet = cell.Sheet;
                var regions = sheet.MergedRegions;
                List<ICell> mergedCells = [];
                foreach (var region in regions)
                {
                    if (region.IsInRange(cell.RowIndex, cell.ColumnIndex))
                    {
                        for (int row = region.FirstRow; row <= region.LastRow; row++)
                        {
                            IRow sheetRow = sheet.GetRow(row);
                            if (sheetRow != null)
                            {
                                for (int col = region.FirstColumn; col <= region.LastColumn; col++)
                                {
                                    ICell _cell = sheetRow.GetCell(col);
                                    if (_cell != null)
                                    {
                                        mergedCells.Add(_cell);
                                    }
                                }
                            }
                        }
                    }
                   
                }
                return mergedCells;
            }
            return null;
        }

        public static TextCellSuggestModel? TryGetCombineCell(ICell currCell, ICell? cellNext)
        {
            if (cellNext == null || string.IsNullOrWhiteSpace(cellNext.ToString()))
                return null;

            var cellNextValue = cellNext?.ToString()?.ToSimilarityCompareString();

            if (!string.IsNullOrWhiteSpace(cellNextValue) && StringUtils.StartWithLower(cellNextValue) && cellNext != null)
            {
                TextCellSuggestModel model = new()
                {
                    Row = currCell.RowIndex,
                    Col = currCell.ColumnIndex,
                    CellValue = currCell.ToString()?.ToSimilarityCompareString() + " " + cellNextValue,
                    CellStatus = CellStatus.Combine,
                    CombineWithCell = new MatrixCellModel()
                    {
                        Row = cellNext.RowIndex,
                        Col = cellNext.ColumnIndex,
                    }
                };
                return model;
            }
            return null;
        }

        public static List<TextCellSuggestModel>? TryGetMergeCell(ICell cell)
        {
            var results = new List<TextCellSuggestModel>();
            if (string.IsNullOrWhiteSpace(cell?.ToString()))
            {
                return null;
            }
            var cellValue = cell.ToString()!;
            var is2OrMoreSentenceCase = StringUtils.Has2OrMoreSentenceCase(cellValue);
            if (!is2OrMoreSentenceCase)
            {
                return null;
            }
            cellValue = cellValue.RemoveSign4VietnameseString();
            var splited = StringUtils.SplitSentenceCase(cellValue.Trim());
            int countIndex = 0;
            foreach (var splitString in splited)
            {
                var nomarlize = splitString.ToSimilarityCompareString();
                if (string.IsNullOrWhiteSpace(nomarlize))
                {
                    continue;
                }
                var cellSuggest = new TextCellSuggestModel()
                {
                    Row = cell.RowIndex,
                    Col = cell.ColumnIndex,
                    IndexInCell = countIndex++,
                    CombineWithCell = null,
                    CellStatus = CellStatus.Merge,
                    CellValue = nomarlize,
                };
                results.Add(cellSuggest);
            }
            var mergeCell = cell.GetListCellInMergeCell();
            if (mergeCell != null && mergeCell.Count > 0)
            {
                var rows = mergeCell.Select(x => x.RowIndex).Distinct().ToList().Count;
                var cols = mergeCell.Select(x => x.ColumnIndex).Distinct().ToList().Count;
                if (rows > 1 && cols == 1)
                {
                    // is combine multi rows
                    foreach (var item in results)
                    {
                        item.RetriveCell = new MatrixCellModel()
                        {
                            Col = item.Col,
                            Row = item.Row + item.IndexInCell
                        };
                    }
                }
                else if (cols > 1 && rows == 1)
                {
                    // is combine multi cols
                    foreach (var item in results)
                    {
                        item.RetriveCell = new MatrixCellModel()
                        {
                            Col = item.Col + item.IndexInCell,
                            Row = item.Row,
                        };
                    }
                }
            }
            return results.Count == 0 ? null : results;
        }
    }
}
