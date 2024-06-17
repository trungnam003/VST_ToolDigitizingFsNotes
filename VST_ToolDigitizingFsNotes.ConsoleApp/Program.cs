using Force.DeepCloner;
using System.Text.RegularExpressions;
using VST_ToolDigitizingFsNotes.AppMain.Services;
using VST_ToolDigitizingFsNotes.Libs.Models;
using VST_ToolDigitizingFsNotes.Libs.Utils;

namespace VST_ToolDigitizingFsNotes.ConsoleApp;

internal class Program
{
    static void Main(string[] args)
    {
        //var str1 = "56.958.465.910.163";

        //var matches01 = DetectUtils.MoneyRegex001().Matches(str1);
        //var matches02 = DetectUtils.MoneyRegex002().Matches(str1);
        //var matches03 = DetectUtils.MoneySoftRegex001().Matches(str1);

        //// log all matches
        //Console.WriteLine("MoneyRegex001");
        //var list1 = new List<MoneyCellModel>();
        //var index1 = 0;
        //foreach (Match match in matches01.Cast<Match>())
        //{
        //    var model = new MoneyCellModel
        //    {
        //        Row = 0, Col = 0,
        //        CellValue = match.Value,
        //        IndexInCell = index1++,
        //    };
        //    model.ConvertRawValueToValue();
        //    list1.Add(model);
        //    Console.WriteLine(model);
        //}

        //Console.WriteLine("MoneyRegex002");
        //var list2 = new List<MoneyCellModel>();
        //var index2 = 0;
        //foreach (Match match in matches02.Cast<Match>())
        //{
        //    var model = new MoneyCellModel
        //    {
        //        Row = 0,
        //        Col = 0,
        //        CellValue = match.Value,
        //        IndexInCell = index2++,
        //    };
        //    model.ConvertRawValueToValue();
        //    list2.Add(model);
        //    Console.WriteLine(model);
        //}

        //Console.WriteLine("MoneySoftRegex001");
        //var list3 = new List<MoneyCellModel>();
        //var index3 = 0;
        //foreach (Match match in matches03.Cast<Match>()) 
        //{
        //    var model = new MoneyCellModel
        //    {
        //        Row = 0,
        //        Col = 0,
        //        CellValue = match.Value,
        //        IndexInCell = index3++,
        //    };
        //    model.ConvertRawValueToValue();
        //    list3.Add(model);
        //    Console.WriteLine(model);
        //}

        //// using linq combine all list, remove duplicates and keep different values

        //Console.WriteLine("Final:");
        //var list = list1.Concat(list2).Concat(list3).Distinct(new CompareMoneyCellModel()).ToList();

        //foreach (var model in list)
        //{
        //    Console.WriteLine(model);
        //}

        //var filePath = "C:\\Users\\trungnamth\\Downloads\\1706606816_BCTC_VNM_Hopnhat_31.12_.2023-VN_.pdf";
        //var pdfService = new PdfService();
        //var task = pdfService.GetPdfPageCountAsync(filePath);
        //task.Wait();
        //var pageCount = task.Result;

        //var task2 = pdfService.SplitPdfAsync(filePath, 1, pageCount, ".", "abc.pdf");
        //task2.Wait();
        //var pageCount2 = task2.Result;

        var abc = new ABC()
        {
            Lst = [1, 2, 3, 4]
        };
        var abc2 = new ABC()
        {
            Lst = [1, 2, 3, 4]
        };

        var abcs = new ABCs()
        {
            Lst = [abc, abc2]
        };

        var abcs2 = abcs.DeepClone();
        abcs2.Lst[0].Lst[0] = 100;

        Console.WriteLine(abcs.Lst[0].Lst[0]);
        Console.WriteLine(abcs2.Lst[0].Lst[0]);
    }
}

public class ABC
{
    public required List<double> Lst;
}

public class ABCs
{
    public required List<ABC> Lst { get; set; }
}

public class CompareMoneyCellModel : IEqualityComparer<MoneyCellModel>
{
    public bool Equals(MoneyCellModel? x, MoneyCellModel? y)
    {
        return x == y;
    }

    public int GetHashCode(MoneyCellModel obj)
    {
        return obj.GetHashCode();
    }
}