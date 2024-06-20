using System.Diagnostics;
using VST_ToolDigitizingFsNotes.Libs.Chains;
using VST_ToolDigitizingFsNotes.Libs.Common.Enums;

namespace VST_ToolDigitizingFsNotes.Libs.Models;

public class FsNoteDataMap
{
    public int FsNoteId { get; set; }
    public int Group { get; set; }
    public List<RangeDetectFsNote>? RangeDetectFsNotes { get; set; }
    public required FsNoteParentModel FsNoteParentModel { get; set; }
    public MapFsNoteStatus MapStatus { get; set; } = MapFsNoteStatus.NotYetMapped;
}

public class RangeDetectFsNote
{
    public required MatrixCellModel Start { get; set; }
    public required MatrixCellModel End { get; set; }
    public required MoneyCellModel MoneyCellModel { get; set; }
    
    public SpecifyMoneyResult? MoneyResults { get; set; }
    public List<TextCellSuggestModel>? ListTextCellSuggestModels { get; set; }

    
    public DetectRangeStatus DetectRangeStatus { get; set; } = DetectRangeStatus.NotYetDetected;
    public DetectStartRangeStatus DetectStartRangeStatus { get; set; } = DetectStartRangeStatus.SkipStringSimilarity;

    public override string ToString()
    {
        return Start.CellValue ?? "";
    }

    public bool IsMoneyInThisRange(MoneyCellModel money)
    {
        return money.Row >= Start.Row && money.Row <= End.Row;
    }

    public void UpdateRange(MatrixCellModel start, MatrixCellModel end)
    {
        Start = start;
        End = end;
    }
}

public class SpecifyMoneyResult
{
    public List<List<MoneyCellModel>> DataRows { get; set; } = [];
    public List<List<MoneyCellModel>> DataCols { get; set; } = [];
    public bool HasDataRows => DataRows.Count > 0;
    public bool HasDataCols => DataCols.Count > 0;

    
}

public static class RangeExtensions
{
    public static void LogToDebug(this SpecifyMoneyResult money)
    {
        Debug.WriteLine("Số tiền quét được bằng tổng");
        if (money.HasDataRows)
        {
            Debug.WriteLine("Tiền theo dòng\n");
            foreach (var row in money.DataRows)
            {
                Debug.WriteLine(string.Join("\t", row.Select(x => x.CellValue)));
            }
        }

        if(money.HasDataCols)
        {
            Debug.WriteLine("Tiền theo cột\n");
            foreach (var col in money.DataCols)
            {
                Debug.WriteLine(string.Join("\t", col.Select(x => x.CellValue)));
            }
        }
        Debug.WriteLine("");
    }

    public static void LogToDebug(this List<TextCellSuggestModel> models)
    {
        Debug.WriteLine("Danh sách các chỉ tiêu con tìm thấy");
        foreach (var model in models)
        {
            Debug.WriteLine($"\t{model}");
        }
        Debug.WriteLine("");
    }
}