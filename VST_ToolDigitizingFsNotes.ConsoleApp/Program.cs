using Force.DeepCloner;
using System.Text.RegularExpressions;
using VST_ToolDigitizingFsNotes.AppMain.Services;
using VST_ToolDigitizingFsNotes.Libs.Models;
using VST_ToolDigitizingFsNotes.Libs.Utils;
using System.Text.Json;
using System.Text.Json.Serialization;
using Newtonsoft.Json;

using MicJsonConvert = System.Text.Json.JsonSerializer;
using NPOI.SS.Formula.Functions;
namespace VST_ToolDigitizingFsNotes.ConsoleApp;

internal class Program
{
    static void Main(string[] args)
    {
        //var str1 = "61 113706.898";

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
        //        Row = 0,
        //        Col = 0,
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

        // random list 27 length 

        //var str = "Tổng doanh thu ■ Bán thành phầm ■ Bán hàng hóa ■ Các dịch vụ khác ■ Cho thuê bất động sản đầu tư ■ Doanh thu khác";
        //var listStr = new List<string>()
        //{
        //    "ban thanh pham", "toi yeu viet nam"
        //};

        //var newStr = str.ToSimilarityCompareString();

        //var ok = StringSimilarityUtils.TryFindStringSimilarityFromPlainText(newStr, listStr[1], out var output);
        //Console.WriteLine(ok + " " + output);


        //var s2 = "Chuyển từ hàng tồn kho Thanh lý/xóa so                     (10.684,285.402)";

        //var s3 = s2.RemoveSpecialCharactersVi();

        //var splited = StringUtils.SplitSentenceCase(s3);

        //var output = "";

        //foreach (var item in splited)
        //{
        //    output += item.ToSimilarityCompareString() + ", ";
        //}
        //JsonConvert.DeserializeObject<object>(output);
        //Console.WriteLine(output);


        //string jsonString = @"{""username"":""dev"",""email"":""thtn.dev@gmail.com"",""roles"":""[\u0022vst_admin_pedia:all:read\u0022,\u0022vst_admin_pedia:all:create\u0022,\u0022vst_admin_pedia:all:update\u0022,\u0022vst_admin_pedia:all:delete\u0022]"",""vst_sso_roles"":""{\u0022ClientId\u0022:\u0022vst_admin_pedia\u0022,\u0022Roles\u0022:{\u0022all\u0022:[\u0022read\u0022,\u0022create\u0022,\u0022update\u0022,\u0022delete\u0022]}}"",""sub"":""dev""}";
        //jsonString.Replace("\\u0022", "\"");
        //JsonDocument document = JsonDocument.Parse(jsonString);
        //JsonElement rootElement = document.RootElement;
        //UserData? o = JsonConvert.DeserializeObject<UserData>(rootElement.GetRawText());

        //var z = o.GetVstSsoRole();
        var cols = new List<int>() { 1, 1, 1, 1, 1, 2,2,2 };

    }
}

//class UserData
//{
//    [JsonProperty("username")]
//    public string Username { get; set; }
//    [JsonProperty("email")]
//    public string Email { get; set; }
//    [JsonProperty("vst_sso_roles")]
//    public VstSsoRole? VstSsoRole { get; set; }

//    //public VstSsoRole? GetVstSsoRole()
//    //{
//    //    // deserialize vst_sso_roles
//    //    return JsonConvert.DeserializeObject<VstSsoRole>(VstSsoRole);
//    //}

//}

//public class VstSsoRole
//{
//    [JsonProperty("ClientId")]
//    public string? ClientId { get; set; }
//    [JsonProperty("Roles")]
//    public Dictionary<string, List<string>>? Roles;
//}

public class ABC
{
    public required List<double> Lst;
}

public class ABCs
{
    public required List<ABC> Lst { get; set; }
}

public class CompareMoneyCellModel2 : IEqualityComparer<MoneyCellModel>
{
    public bool Equals(MoneyCellModel? x, MoneyCellModel? y)
    {
        return x != null && y != null && x.Value == y.Value;
    }

    public int GetHashCode(MoneyCellModel obj)
    {
        return HashCode.Combine(obj.Value);
    }
}