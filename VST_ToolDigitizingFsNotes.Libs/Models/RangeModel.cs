﻿using VST_ToolDigitizingFsNotes.Libs.Chains;
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
    public DetectRangeStatus DetectRangeStatus { get; set; } = DetectRangeStatus.NotYetDetected;
    public SpecifyMoneyResult? MoneyResults { get; set; }
    public List<TextCellSuggestModel>? ListTextCellSuggestModels { get; set; }

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

