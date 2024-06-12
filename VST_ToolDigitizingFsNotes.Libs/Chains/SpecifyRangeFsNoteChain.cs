using VST_ToolDigitizingFsNotes.Libs.Common.Enums;
using VST_ToolDigitizingFsNotes.Libs.Models;
using VST_ToolDigitizingFsNotes.Libs.Utils;

namespace VST_ToolDigitizingFsNotes.Libs.Chains;

public sealed class DetectRangeChainRequest : ChainBaseRequest<RangeDetectFsNote>
{
    public const int MaximumAllowRowRange = 60;
    public MoneyCellModel MoneyCell { get; init; }
    public UnitOfWorkModel UnitOfWork { get; init; }
    public FsNoteParentModel Parent { get; init; }

    public DetectRangeChainRequest(UnitOfWorkModel unitOfWork, FsNoteParentModel parent, MoneyCellModel money)
    {
        UnitOfWork = unitOfWork;
        Parent = parent;
        MoneyCell = money;
    }

}

//public interface IHandleDetectFsNoteChain
//{
//    void Handle(DetectRangeChainRequest request);
//    void SetNext(IHandleDetectFsNoteChain nextChain);
//}

//public abstract class DetectFsNoteChainBase : IHandleDetectFsNoteChain
//{
//    protected IHandleDetectFsNoteChain? _nextChain;
//    public void SetNext(IHandleDetectFsNoteChain nextChain)
//    {
//        _nextChain = nextChain;
//    }
//    public abstract void Handle(DetectRangeChainRequest request);
//}

/// <summary>
/// Khoanh vùng dùng heading
/// </summary>
public class DetectUsingHeadingHandler : HandleChainBase<DetectRangeChainRequest>
{
    public FsNoteParentMappingModel MapParentData { get; init; }
    public DetectUsingHeadingHandler(FsNoteParentMappingModel mapParentData)
    {
        MapParentData = mapParentData;
    }

    public override void Handle(DetectRangeChainRequest request)
    {
        var col = request.MoneyCell.Col;
        var row = request.MoneyCell.Row;
        var cell = request.UnitOfWork.OcrWorkbook?.GetSheetAt(0).GetRow(row).GetCell(col);
        var lastCellNum = cell?.Row.PhysicalNumberOfCells ?? 0;

        if (cell == null)
        {
            _nextChain?.Handle(request);
            return;
        }

        var queue = FindNearestHeading(request.MoneyCell, request.UnitOfWork.HeadingCellModels);
        if (queue.Count == 0)
        {
            _nextChain?.Handle(request);
            return;
        }
        var maxSimilarity = double.MinValue;
        HeadingCellModel? nearestHeading = null;

        while (queue.Count > 0)
        {
            var heading = queue.Dequeue();
            foreach (var keyword in MapParentData.Keywords)
            {
                var similarity = StringSimilarityUtils.CalculateSimilarity(heading?.ContentSection!, keyword);
                if (similarity > maxSimilarity)
                {
                    maxSimilarity = similarity;
                }
            }
            // nếu độ tương đồng lớn hơn ngưỡng chấp nhận thì dừng lại
            if (maxSimilarity >= StringSimilarityUtils.AcceptableSimilarity)
            {
                nearestHeading = heading;
                break;
            }
        }

        if (nearestHeading != null)
        {
            request.Result = new RangeDetectFsNote()
            {
                MoneyCellModel = request.MoneyCell,
                Start = nearestHeading,
                End = new MatrixCellModel()
                {
                    Row = row,
                    Col = lastCellNum
                },
            };
            request.SetHandled(true);
            return;
        }
        _nextChain?.Handle(request);
    }

    private static PriorityQueue<HeadingCellModel, double> FindNearestHeading(MoneyCellModel money, List<HeadingCellModel> headings)
    {
        // độ ưu tiên tăng dần
        var results = new PriorityQueue<HeadingCellModel, double>(Comparer<double>.Create((x, y) => x.CompareTo(y)));

        var row = money.Row;
        var col = money.Col;

        foreach (var heading in headings)
        {
            if (row < heading.Row)
            {
                break;
            }
            if (row - heading.Row > DetectRangeChainRequest.MaximumAllowRowRange)
            {
                continue;
            }
            var distance = CoreUtils.EuclideanDistance(row, col, heading.Row, heading.Col);
            results.Enqueue(heading, distance);
        }
        return results;
    }
}

/// <summary>
/// Khoanh vùng dùng chuỗi tương đương
/// -> xử lý khi không tìm thấy heading
/// </summary>
public class DetectUsingSimilartyStringHanlder : HandleChainBase<DetectRangeChainRequest>
{
    public FsNoteParentMappingModel MapParentData { get; init; }
    public DetectUsingSimilartyStringHanlder(FsNoteParentMappingModel mapParentData)
    {
        MapParentData = mapParentData;
    }
    public override void Handle(DetectRangeChainRequest request)
    {
        var col = request.MoneyCell.Col;
        var row = request.MoneyCell.Row;
        var cell = request.UnitOfWork.OcrWorkbook?.GetSheetAt(0).GetRow(row).GetCell(col);

        if (cell == null)
        {
            _nextChain?.Handle(request);
            return;
        }

        var lastCellNum = cell?.Row.PhysicalNumberOfCells ?? 0;

        var queue = FindSimilarityFsNoteParentCell(request.MoneyCell, request.UnitOfWork, MapParentData);

        if (queue.Count == 0)
        {
            _nextChain?.Handle(request);
            return;
        }

        var first = queue.Dequeue();

        request.Result = new RangeDetectFsNote()
        {
            MoneyCellModel = request.MoneyCell,
            Start = first,
            End = new MatrixCellModel()
            {
                Row = row,
                Col = lastCellNum
            }
        };
        request.SetHandled(true);
    }

    private static PriorityQueue<MatrixCellModel, int> FindSimilarityFsNoteParentCell(MoneyCellModel money, UnitOfWorkModel uow, FsNoteParentMappingModel MapParentData)
    {
        // độ ưu tiên giảm dần (chỉ lấy chỉ tiêu xa nhất vì gần có thể là chỉ tiêu con)
        var results = new PriorityQueue<MatrixCellModel, int>(Comparer<int>.Create((x, y) => x.CompareTo(y)));
        var row = money.Row;
        var col = money.Col;

        var start = Math.Max(0, row - DetectRangeChainRequest.MaximumAllowRowRange);
        var end = row;
        const int FIRST_COL = 0;
        var workbook = uow.OcrWorkbook;
        for (int i = end; i >= start; i--)
        {
            var cell = workbook?.GetSheetAt(0)?.GetRow(i)?.GetCell(FIRST_COL);
            if (cell == null)
            {
                continue;
            }
            var cellValue = cell.ToString();

            if (string.IsNullOrEmpty(cellValue))
            {
                continue;
            }
            cellValue = cellValue.ToSystemNomalizeString();
            var maxSimilarity = double.MinValue;
            foreach (var keyword in MapParentData.Keywords)
            {
                var similarity = StringSimilarityUtils.CalculateSimilarity(cellValue, keyword);
                if (similarity > maxSimilarity)
                {
                    maxSimilarity = similarity;
                }
            }

            if (maxSimilarity >= StringSimilarityUtils.AcceptableSimilarity)
            {
                results.Enqueue(new MatrixCellModel()
                {
                    Row = i,
                    Col = FIRST_COL
                }, i);
            }
        }
        return results;
    }
}

/// <summary>
/// Xử lý khi chỉ tiêu cha bị gộp vào 1 ô
/// Ví dụ: 5. Hang ton kho hang hoa dang di tren duong
/// => cần lấy ra hang ton kho
/// </summary>
public class DetectUsingDiffMatchPatchStringHandler : HandleChainBase<DetectRangeChainRequest>
{
    public FsNoteParentMappingModel MapParentData { get; init; }
    public DetectUsingDiffMatchPatchStringHandler(FsNoteParentMappingModel mapParentData)
    {
        MapParentData = mapParentData;
    }

    public override void Handle(DetectRangeChainRequest request)
    {
        var col = request.MoneyCell.Col;
        var row = request.MoneyCell.Row;
        var cell = request.UnitOfWork.OcrWorkbook?.GetSheetAt(0).GetRow(row).GetCell(col);

        if (cell == null)
        {
            _nextChain?.Handle(request);
            return;
        }
        var lastCellNum = cell?.Row.PhysicalNumberOfCells ?? 0;
        var queue = FindMatchPatchStringFsNoteParentCell(request.MoneyCell, request.UnitOfWork, MapParentData);
        if (queue.Count == 0)
        {
            _nextChain?.Handle(request);
            return;
        }
        var first = queue.Dequeue();

        request.Result = new RangeDetectFsNote()
        {
            MoneyCellModel = request.MoneyCell,
            Start = first,
            End = new MatrixCellModel()
            {
                Row = row,
                Col = lastCellNum
            },
            DetectStartRangeStatus = DetectStartRangeStatus.AllowStringSimilarity
        };
        request.SetHandled(true);
    }

    private static PriorityQueue<MatrixCellModel, int> FindMatchPatchStringFsNoteParentCell(MoneyCellModel money, UnitOfWorkModel uow, FsNoteParentMappingModel MapParentData)
    {
        var results = new PriorityQueue<MatrixCellModel, int>(Comparer<int>.Create((x, y) => x.CompareTo(y)));
        var row = money.Row;
        var col = money.Col;
        var start = Math.Max(0, row - DetectRangeChainRequest.MaximumAllowRowRange);
        var end = row;
        const int FIRST_COL = 0;
        var workbook = uow.OcrWorkbook;

        for (int i = end; i >= start; i--)
        {
            var cell = workbook?.GetSheetAt(0)?.GetRow(i)?.GetCell(FIRST_COL);
            if (cell == null)
            {
                continue;
            }
            var cellValue = cell.ToString();

            if (string.IsNullOrEmpty(cellValue))
            {
                continue;
            }
            cellValue = cellValue.ToSystemNomalizeString();
            var maxSimilarity = double.MinValue;
            foreach (var keyword in MapParentData.Keywords)
            {
                var text = StringSimilarityUtils.FindDiffMatchPatchFromPlainText(cellValue, keyword);
                var similarity = StringSimilarityUtils.CalculateSimilarity(text, keyword);
                if (similarity > maxSimilarity)
                {
                    maxSimilarity = similarity;
                }
            }
            if (maxSimilarity >= StringSimilarityUtils.AcceptableSimilarity)
            {
                results.Enqueue(new MatrixCellModel()
                {
                    Row = i,
                    Col = FIRST_COL
                }, i);
            }
        }
        return results;
    }
}