using System.Drawing;

namespace VST_ToolDigitizingFsNotes.Libs.Utils
{
    public static class CoreUtils
    {
        public static double EuclideanDistance(int x1, int y1, int x2, int y2)
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

    }
}
