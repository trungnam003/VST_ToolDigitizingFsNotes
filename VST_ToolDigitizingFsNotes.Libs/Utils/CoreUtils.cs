using System.Drawing;

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
        public static byte DetermineDirection(List<(int, int)> elements)
        {
            List<int> rows = elements.Select(e => e.Item1).ToList();
            List<int> cols = elements.Select(e => e.Item2).ToList();
            double rowVariance = CalculateVariance(rows);
            double colVariance = CalculateVariance(cols);

            if (rowVariance < colVariance)
            {
                return 1;
            }
            else if (colVariance < rowVariance)
            {
                return 2;
            }
            else
            {
                return 0;
            }
        }
    }
}
