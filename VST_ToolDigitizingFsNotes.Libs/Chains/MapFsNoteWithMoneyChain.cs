
using Ardalis.SmartEnum;
using VST_ToolDigitizingFsNotes.Libs.Models;

namespace VST_ToolDigitizingFsNotes.Libs.Chains;

public class MapFsNoteWithMoneyChainRequest : ChainBaseRequest<MapEvaluators>
{
    public List<TextCellSuggestModel> ListTextCellSuggests { get; init; }
    public List<MoneyCellModel> ListMoneyCells { get; init; }

    public MapFsNoteWithMoneyChainRequest(List<TextCellSuggestModel> listTextCellSuggests, List<MoneyCellModel> listMoneyCells)
    {
        ListTextCellSuggests = listTextCellSuggests;
        ListMoneyCells = listMoneyCells;
    }
}

public class MapEvaluatorStatus : SmartEnum<MapEvaluatorStatus>
{
    public static readonly MapEvaluatorStatus CantMap = new(nameof(CantMap), -1);
    public static readonly MapEvaluatorStatus NotYetMapped = new(nameof(NotYetMapped), 0);
    public static readonly MapEvaluatorStatus Mapped = new(nameof(Mapped), 1);

    public MapEvaluatorStatus(string name, int value) : base(name, value)
    {
    }
}

public class MapType : SmartEnum<MapType>
{
    public static readonly MapType None = new(nameof(None), 0);
    public static readonly MapType MappedInRow = new(nameof(MappedInRow), 1);

    public MapType(string name, int value) : base(name, value)
    {
    }
}

public class MapEvaluator
{
    public TextCellSuggestModel textCellSuggest { get; init; }
    public MoneyCellModel moneyCell { get; init; }
   

    public double Distance { get; set; }
    public double Angle { get; set; }

    public MapEvaluatorStatus Status { get; set; } = MapEvaluatorStatus.NotYetMapped;
    public MapType MapType { get; set; } = MapType.None;

    public MapEvaluator(TextCellSuggestModel textCellSuggest, MoneyCellModel moneyCell)
    {
        this.textCellSuggest = textCellSuggest;
        this.moneyCell = moneyCell;
    }
}

public class MapEvaluators
{
    public List<MapEvaluator> ListMapEvaluators { get; init; } = [];
    public HashSet<MoneyCellModel> MoneyCellMapped { get; init; } = [];
}


/// <summary>
/// Map trên cùng 1 hàng
/// </summary>
public class MapInRowHandler : HandleChainBase<MapFsNoteWithMoneyChainRequest>
{
    public override void Handle(MapFsNoteWithMoneyChainRequest request)
    {
        if (request.Handled)
        {
            return;
        }
        // là root nên không cần kiểm tra null và khởi tạo mới giá trị
        var evaluators = new MapEvaluators();

        foreach(var money in request.ListMoneyCells)
        {
            var row = money.Row;
            var suggestInRowFirst = request.ListTextCellSuggests.FirstOrDefault(x => x.Row == row);

            if (suggestInRowFirst == null)
            {
                continue;
            }

            var evaluator = new MapEvaluator(suggestInRowFirst, money);
            evaluators.ListMapEvaluators.Add(evaluator);
        }

        if(evaluators.ListMapEvaluators.Count != 0)
        {
            request.Result = evaluators;
            if(evaluators.ListMapEvaluators.Count == request.ListMoneyCells.Count)
            {
                request.SetHandled(true);
                return;
            }
        }
        _nextChain?.Handle(request);
    }
}