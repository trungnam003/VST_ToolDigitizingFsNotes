using System.Text.RegularExpressions;
using VST_ToolDigitizingFsNotes.Libs.Utils;

namespace VST_ToolDigitizingFsNotes.ConsoleApp;

internal class Program
{
    static void Main(string[] args)
    {
        var str1 = "56.958.465.910.163 3.257.277.347.350 50.208.997.764 7.939,153.750 205.021.157.713";

        var matches01 = DetectUtils.MoneyRegex001().Matches(str1);
        var matches02 = DetectUtils.MoneyRegex002().Matches(str1);
        var matches03 = DetectUtils.MoneySoftRegex001().Matches(str1);

        // log all matches
        Console.WriteLine("MoneyRegex001");
        foreach (Match match in matches01)
        {
            Console.WriteLine(match.Value);
        }

        Console.WriteLine("MoneyRegex002");
        foreach (Match match in matches02)
        {
            Console.WriteLine(match.Value);
        }

        Console.WriteLine("MoneySoftRegex001");
        foreach (Match match in matches03) 
        {
            Console.WriteLine(match.Value);
        }
        var a = new List<int>();

    }
}