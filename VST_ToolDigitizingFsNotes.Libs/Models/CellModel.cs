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
        public int IndexInCell { get; set; }

        public bool Equals(MoneyCellModel? other)
        {
            return other != null && Value == other.Value && Col == other.Col && Row == other.Row && IndexInCell == other.IndexInCell;
        }

        public override string ToString()
        {
            return $"[{Row}:{Col}] {CellValue} -> {Value} ({IndexInCell})";
        }

        public static bool operator ==(MoneyCellModel? left, MoneyCellModel? right)
        {
            return left?.Equals(right) ?? right is null;
        }

        /// <summary>
        /// So sánh 2 số tiền có khác nhau không dựa trên tọa độ và giá trị
        /// </summary>
        /// <param name="left"></param>
        /// <param name="right"></param>
        /// <returns></returns>
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
            return HashCode.Combine(Value, Row, Col, IndexInCell);
        }

        public static int MoneyCellModelComparer(MoneyCellModel x, MoneyCellModel y)
        {
            if (x.Row == y.Row)
            {
                if (x.Col == y.Col)
                {
                    return x.IndexInCell.CompareTo(y.IndexInCell);
                }
                return x.Col.CompareTo(y.Col);
            }
            return x.Row.CompareTo(y.Row);
        }

        //private int? _cachedHashCode;
        //private IEnumerable<object> GetEqualityComponents()
        //{
        //    yield return Row;
        //    yield return Col;
        //    yield return NoteId;
        //    yield return Similarity;
        //}

        //public override int GetHashCode()
        //{
        //    _cachedHashCode ??= GetEqualityComponents()
        //        .Aggregate(1, (current, obj) =>
        //        {
        //            unchecked
        //            {
        //                return current * 23 + (obj?.GetHashCode() ?? 0);
        //            }
        //        });

        //    return _cachedHashCode.Value;
        //}

        //public override bool Equals(object? obj)
        //{
        //    if (obj == null || obj.GetType() != GetType())
        //    {
        //        return false;
        //    }
        //    var other = (TextCellSuggestModel)obj;
        //    return GetEqualityComponents().SequenceEqual(other.GetEqualityComponents());
        //}
    }
    /// <summary>
    /// Class giữ thông tin của ô là Heading
    /// </summary>
    public class HeadingCellModel : MatrixCellModel
    {
        public string ContentSection { get; set; } = string.Empty;
        public string SymbolSection { get; set; } = string.Empty;
        public override MatrixCellType CellType { get; set; } = MatrixCellType.Heading;
        public MatrixCellModel? CombineWithCell { get; set; }

        public override string ToString()
        {
            var str = $"[{Row}:{Col}] - [{SymbolSection}] {ContentSection}";
            return str;
        }
    }


    /// <summary>
    /// 
    /// </summary>
    public class TextCellSuggestModel : MatrixCellModel, IEquatable<TextCellSuggestModel>
    {
        public int NoteId { get; set; }
        public string NoteName { get; set; } = string.Empty;
        /// <summary>
        /// Độ tương đồng với chỉ tiêu TM BCTC
        /// </summary>
        public double Similarity { get; set; } = 0.0;
        public MatrixCellModel? CombineWithCell { get; set; }
        public MatrixCellModel? RetriveCell { get; set; }

        public int IndexInCell { get; set; } = 0;

        public CellStatus CellStatus { get; set; } = CellStatus.Default;

        public override MatrixCellType CellType { get; set; } = MatrixCellType.Text;

        public bool Equals(TextCellSuggestModel? other)
        {
            return other != null && NoteId == other.NoteId && Col == other.Col && Row == other.Row && Similarity == other.Similarity;
        }

        public override string ToString()
        {
            var indexInCell = IndexInCell > 0 ? IndexInCell+"" : "";
            var retrive = RetriveCell != null ? $"M[{RetriveCell.Row}:{RetriveCell.Col}]" : "";
            var combine = CombineWithCell != null ? $"C[{CombineWithCell.Row}:{CombineWithCell.Col}]" : "";
            return $"[{Row}:{Col}] {CellValue} => {NoteName}; {indexInCell}{retrive}{combine}";
        }

        public override bool Equals(object? obj)
        {
            return Equals(obj as TextCellSuggestModel);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(NoteId, Row, Col, Similarity);
        }
    }
    public enum CellStatus
    {
        Default,
        Combine,
        Merge
    }

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

    public class CompareMoneyCellModel : IEqualityComparer<MoneyCellModel>
    {
        public bool Equals(MoneyCellModel? x, MoneyCellModel? y)
        {
            return x != null && y != null && x.Value == y.Value && x.Col == y.Col && x.Row == y.Row;
        }

        public int GetHashCode(MoneyCellModel obj)
        {
            return HashCode.Combine(obj.Value, obj.Row, obj.Col);
        }
    }
}
