namespace VST_ToolDigitizingFsNotes.Libs.Common;

public class DataReaderMapSetting
{
    public required Matrix NoteIdAddress { get; set; }
    public required Matrix NameAddress { get; set; }
    public required Matrix CheckParentAddress { get; set; }
    public required Matrix KeywordsAddress { get; set; }
    public required Matrix KeywordExtensionAddress { get; set; }
    public required Matrix OtherAddress { get; set; }
}

public class DataReaderSheetSetting
{
    public required Matrix NoteIdAddress { get; set; }
    public required Matrix CheckParentAddress { get; set; }
    public required Matrix NameAddress { get; set; }
    public required Matrix ParentValueAddress { get; set; }
    public required Matrix ValueAddress { get; set; }
}

public class MetaDataReaderSheetSetting
{
    public required Matrix StockCodeAddress { get; set; }
    public required Matrix ReportTermAddress { get; set; }
    public required Matrix YearAddress { get; set; }
    public required Matrix AuditedStatusAddress { get; set; }
    public required Matrix ReportTypeAddress { get; set; }
    public required Matrix UnitAddress { get; set; }
    public required Matrix FileUrlAddress { get; set; }
}

public class Matrix
{
    public int Row { get; }
    public int Col { get; }
    public static Matrix StringToMatrix(string matrix)
    {
        var parts = matrix.Split(',');
        return new Matrix(int.Parse(parts[0]), int.Parse(parts[1]));
    }
    public Matrix()
    {

    }

    public Matrix(int r, int c)
    {
        Row = r;
        Col = c;
    }

    public static Matrix FromExcelAddress(string address)
    {
        // A1 -> Col = 0, Row = 0
        // B2 -> Col = 1, Row = 1
        var col = address[0] - 'A';
        var row = int.Parse(address[1..]) - 1;
        return new Matrix(row, col);
    }
}
