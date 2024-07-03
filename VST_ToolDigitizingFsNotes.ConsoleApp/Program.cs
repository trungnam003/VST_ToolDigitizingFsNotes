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
namespace VST_ToolDigitizingFsNotes.ConsoleApp;

internal class Program
{
    static async Task Main(string[] args)
    {
        var t1 = Run("t1");
        var t2 = Run("t2");
        var t3 = Run("t3");
        var start = Stopwatch.StartNew();
        await Task.WhenAll(t1, t2, t3);
        await t1;
        await t2;
        await t3;
        start.Stop();
        Console.WriteLine($"Time: {start.ElapsedMilliseconds}");
    }
    static async Task Run(string msg)
    {
        for(int i = 0; i < 10; i++)
        {
            Console.WriteLine($"{msg} {i}");
            await Task.Delay(1000);
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