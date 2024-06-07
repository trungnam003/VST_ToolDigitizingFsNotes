using VST_ToolDigitizingFsNotes.Libs.Chains;

namespace VST_ToolDigitizingFsNotes.Libs.Models;

public class FsNoteDataMap
{
    public int FsNoteId { get; set; }
    public int Group { get; set; }
    public List<RangeDetectFsNote>? RangeDetectFsNotes { get; set; }
    public required FsNoteParentModel FsNoteParentModel { get; set; }

    public SpecifyMoneyResult? MoneyResults { get; set; }
}

public class RangeDetectFsNote
{
    public required MatrixCellModel Start { get; set; }
    public required MatrixCellModel End { get; set; }

    public required MoneyCellModel MoneyCellModel { get; set; }

    public override string ToString()
    {
        return Start.CellValue ?? "";
    }

    public bool IsMoneyInThisRange(MoneyCellModel money)
    {
        return money.Row >= Start.Row && money.Row <= End.Row;
    }
}