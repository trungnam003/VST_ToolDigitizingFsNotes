

namespace VST_ToolDigitizingFsNotes.Libs.Models
{
    /// <summary>
    /// Base Class cho dữ liệu của một ô trong bảng
    /// </summary>
    public class MatrixCellModel
    {
        public int Row { get; set; }
        public int Col { get; set; }
        public string? CellValue { get; set; } = string.Empty;
        public virtual MatrixCellType CellType { get; set; } = MatrixCellType.Default;

        public override string ToString()
        {
            return $"{Row}:{Col} - {CellValue}";
        }
    }

    /// <summary>
    /// Class thể hiện giá trị tiền tệ của một ô trong bảng
    /// </summary>
    public class MoneyCellModel : MatrixCellModel, IEquatable<MoneyCellModel>
    {
        /// <summary>
        /// Giá trị được chuyển đổi từ RawValue
        /// </summary>
        public double Value { get; set; }
        public override MatrixCellType CellType { get; set; } = MatrixCellType.Money;

        public bool Equals(MoneyCellModel? other)
        {
            return other != null && Value == other.Value && Col == other.Col && Row == other.Row;
        }

        public override string ToString()
        {
            return $"{Row}:{Col} - {CellValue} ({Value})";
        }

        public static bool operator ==(MoneyCellModel? left, MoneyCellModel? right)
        {
            return left?.Equals(right) ?? right is null;
        }

        public static bool operator !=(MoneyCellModel? left, MoneyCellModel? right)
        {
            return !(left == right);
        }

        public override bool Equals(object? obj)
        {
            return Equals(obj as MoneyCellModel);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Value, Row, Col);
        }
    }
    /// <summary>
    /// Class giữ thông tin của ô là Heading
    /// </summary>
    public class HeadingCellModel : MatrixCellModel
    {
        public string ContentSection { get; set; } = string.Empty;
        public string SymbolSection { get; set; } = string.Empty;
        public HeadingCellMetaData MetaData { get; } = new();
        public override MatrixCellType CellType { get; set; } = MatrixCellType.Heading;

        public override string ToString()
        {
            string frontData = string.Join(", ", MetaData.FrontData.Select(x => x.ToString()));
            string backData = string.Join(", ", MetaData.BackData.Select(x => x.ToString()));
            var str = $"{Row}:{Col} - {CellValue}";
            if (!string.IsNullOrEmpty(frontData))
            {
                str += $" || <<{frontData}>>";
            }
            if (!string.IsNullOrEmpty(backData))
            {
                str += $" || <<{backData}>>";
            }
            return str;
        }
    }


    /// <summary>
    /// 
    /// </summary>
    public class TextCellSuggestModel : MatrixCellModel
    {
        public int NoteId { get; set; }
        /// <summary>
        /// Độ tương đồng với chỉ tiêu TM BCTC
        /// </summary>
        public double Similarity { get; set; } = 0.0;

        public override MatrixCellType CellType { get; set; } = MatrixCellType.Text;
    }

    /// <summary>
    /// 
    /// </summary>
    public class FsNoteCellModel : MatrixCellModel
    {
        public int NoteId { get; set; }
        public int ParentId { get; set; }

        public override MatrixCellType CellType { get; set; } = MatrixCellType.FsNote;
    }

    public class HeadingCellMetaData
    {
        public List<MatrixCellModel> FrontData { get; } = [];
        public List<MatrixCellModel> BackData { get; } = [];
    }


    public static class CellModelExtensions
    {
        /// <summary>
        /// Chuyển đổi giá trị RawValue sang Value
        /// </summary>
        /// <param name="cell"></param>
        public static void ConvertRawValueToValue(this MoneyCellModel cell)
        {
            if (string.IsNullOrEmpty(cell.CellValue))
            {
                cell.Value = 0;
                return;
            }
            var negative = cell.CellValue.StartsWith("(") && cell.CellValue.EndsWith(")") ? -1 : 1;
            var rawMoney = cell.CellValue.Replace(",", string.Empty).Replace(".", string.Empty).Replace(" ", string.Empty);
            rawMoney = rawMoney.Trim('(', ')');
            if (double.TryParse(rawMoney, out var money))
            {
                cell.Value = money * negative;
            }
        }
    }

    public enum MatrixCellType
    {
        Default,
        Money,
        Heading,
        HeadingGroup,
        Text,
        FsNote
    }
}
