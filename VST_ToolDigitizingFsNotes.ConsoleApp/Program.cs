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
using System.Diagnostics;
using VST_ToolDigitizingFsNotes.AppMain.ViewModels;
namespace VST_ToolDigitizingFsNotes.ConsoleApp;

internal class Program
{
    static void Main(string[] args)
    {
        var lst = new List<int>
        {
            1,2,3,4,5,6,2,1,3
        };
        int sum = 6;
        var result = DetectUtils.FindAllSubsetSums(lst, (double)sum, x=>x);
        foreach (var item in result)
        {
            Console.WriteLine(string.Join(",", item));
        }
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