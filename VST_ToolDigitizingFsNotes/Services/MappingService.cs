using Force.DeepCloner;
using NPOI.XSSF.UserModel;
using System.IO;
using VST_ToolDigitizingFsNotes.Libs.Chains;
using VST_ToolDigitizingFsNotes.Libs.Common;
using VST_ToolDigitizingFsNotes.Libs.Common.Enums;
using VST_ToolDigitizingFsNotes.Libs.Models;
using VST_ToolDigitizingFsNotes.Libs.Services;
using VST_ToolDigitizingFsNotes.Libs.Utils;
using Direction = VST_ToolDigitizingFsNotes.Libs.Utils.CoreUtils.Direction;

namespace VST_ToolDigitizingFsNotes.AppMain.Services;

public class MappingService : IMappingService
{
    #region Config Format Read From File
    private const int StartRow = 1;

    private const int ColumnNoteId = 0;
    private const int ColumnName = 1;
    private const int ColumnIsParent = 2;
    private const int ColumnKeyword = 3;
    private const int ColumnExtensionKeyword = 4;
    #endregion

    private readonly Dictionary<int, FsNoteParentMappingModel> _mapping;
    private readonly UserSettings _userSettings;
    private readonly DataReaderMapSetting _dataReaderMapSetting;
    public MappingService(Dictionary<int, FsNoteParentMappingModel> mapping, UserSettings userSettings, DataReaderMapSetting dataReaderMapSetting)
    {
        _mapping = mapping;
        _userSettings = userSettings;
        _dataReaderMapSetting = dataReaderMapSetting;
    }

    public async Task LoadMapping()
    {
        try
        {
            var fileMapping = ValidateFileMapping();
            _mapping.Clear();
            await LoadMapping(fileMapping);
        }
        catch (Exception)
        {
            throw;
        }
    }

    private FileInfo ValidateFileMapping()
    {
        if (string.IsNullOrEmpty(_userSettings.FileMappingPath))
        {
            throw new ArgumentNullException("FileMappingPath is null or empty");
        }

        var fileMapping = new FileInfo(_userSettings.FileMappingPath);

        if (!fileMapping.Exists)
        {
            throw new FileNotFoundException("FileMappingPath not found");
        }

        return fileMapping;
    }

    private async Task LoadMapping(FileInfo fileInfo)
    {
        await using var fs = new FileStream(fileInfo.FullName, FileMode.Open, FileAccess.Read);

        using var workbook = await Task.Run(() => new XSSFWorkbook(fs));

        var sheet = workbook.GetSheetAt(0) ?? throw new ArgumentNullException("Sheet is null");

        int currentParentId = -1;
        Dictionary<int, int> countGroup = [];
        for (var i = StartRow; i <= sheet.LastRowNum; i++)
        {
            var row = sheet.GetRow(i) ?? throw new ArgumentNullException("Row is null");

            var noteId = row.GetCell(ColumnNoteId).NumericCellValue;
            var name = row.GetCell(ColumnName).StringCellValue;
            var isParent = FsNoteMappingBase.Parent.Equals(row.GetCell(ColumnIsParent).StringCellValue.ToLower().Trim());
            var keyword = row.GetCell(ColumnKeyword)?.StringCellValue ?? string.Empty;
            var extensionKeyword = row.GetCell(ColumnExtensionKeyword)?.StringCellValue ?? string.Empty;

            if (isParent)
            {
                var previousParentId = currentParentId;
                currentParentId = (int)noteId;

                if (_mapping.TryGetValue(currentParentId, out FsNoteParentMappingModel? value) && currentParentId == previousParentId)
                {
                    countGroup[currentParentId]++;
                    value.Children.Add([]);
                    value.TotalGroup = countGroup[currentParentId];
                    continue;
                }
                _mapping.Add((int)noteId, new FsNoteParentMappingModel
                {
                    Id = (int)noteId,
                    Name = name,
                    Keywords = [.. keyword.Split(',').Select(x => x.Trim())],
                    KeywordExtensions = [.. extensionKeyword.Split(',').Select(x => x.Trim())],
                    Children = [[]],
                    IsDisabled = FsNoteMappingBase.Disabled.Equals(keyword),
                    TotalGroup = 0
                });
                countGroup.Add(currentParentId, 0);
            }
            else
            {
                var parentId = currentParentId;
                var isFormula = keyword.Equals(FsNoteMappingBase.Formula);
                var isOther = keyword.Equals(FsNoteMappingBase.Other);

                var parent = _mapping[parentId];
                var child = new FsNoteMappingModel
                {
                    Id = (int)noteId,
                    Name = name,
                    Keywords = [.. keyword.Split(',').Select(x => x.Trim())],
                    KeywordExtensions = [.. extensionKeyword.Split(',').Select(x => x.Trim())],
                    ParentId = parentId,
                    IsFormula = isFormula,
                    IsOther = isOther
                };
                parent.Children[parent.TotalGroup].Add(child);
            }
        }

        fs.Close();
        workbook.Close();
    }

    public void MapFsNoteWithMoney(UnitOfWorkModel uow, FsNoteDataMap dataMap)
    {
        var ranges = dataMap.RangeDetectFsNotes?.Where(x => x.DetectRangeStatus == DetectRangeStatus.AllowNextHandle).ToList();
        if (ranges == null || ranges.Count == 0)
        {
            return;
        }
        MapEvaluators? rs = null;
        foreach (var range in ranges)
        {
            var suggests = range.ListTextCellSuggestModels;
            if (suggests == null) continue;
            List<(int, int)>? input = suggests.Select(x => (x.Row, x.Col)).ToList();

            if (input == null || input.Count == 0)
            {
                continue;
            }
            var direction = CoreUtils.DetermineDirection(input);
            if (direction == Direction.Row)
            {
                rs = HandleMappingRowDirection(uow, dataMap, range);

            }
            else if (direction == Direction.Column)
            {
                rs = HandleMappingColumnDirection(uow, dataMap, range);

            }
            else if (direction == Direction.Unknown
                && input.Count == 1)
            {
                var debug = 1;
            }

            if (rs != null)
            {
                foreach (var evaluator in rs.ListMapEvaluators)
                {
                    var id = evaluator.textCellSuggest.NoteId;
                    var value = evaluator.moneyCell.Value;
                    if (dataMap.Result.TryGetValue(id, out var fsNoteModel))
                    {
                        fsNoteModel.Value += value;
                        fsNoteModel.Values.Add(value);
                    }
                    else
                    {
                        if (dataMap.HasOtherFsNoteId)
                        {
                            dataMap.Result[dataMap.OtherFsNoteId].Value += value;
                            dataMap.Result[dataMap.OtherFsNoteId].Values.Add(value);
                        }
                        else if (value < 0)
                        {
                            dataMap.Result[dataMap.NegOtherFsNoteId].Value += value;
                            dataMap.Result[dataMap.NegOtherFsNoteId].Values.Add(value);
                        }
                        else if (value > 0)
                        {
                            dataMap.Result[dataMap.PosOtherFsNoteId].Value += value;
                            dataMap.Result[dataMap.PosOtherFsNoteId].Values.Add(value);
                        }
                    }
                    
                }

                if (rs.RemainMoneys != null && rs.RemainMoneys.Count != 0)
                {
                    foreach (var remainMoney in rs.RemainMoneys)
                    {
                        var remainValue = remainMoney.Value;

                        if (dataMap.HasOtherFsNoteId)
                        {
                            dataMap.Result[dataMap.OtherFsNoteId].Value += remainValue;
                            dataMap.Result[dataMap.OtherFsNoteId].Values.Add(remainValue);
                        }
                        else if (remainValue < 0)
                        {
                            dataMap.Result[dataMap.NegOtherFsNoteId].Value += remainValue;
                            dataMap.Result[dataMap.NegOtherFsNoteId].Values.Add(remainValue);
                        }
                        else if (remainValue > 0)
                        {
                            dataMap.Result[dataMap.PosOtherFsNoteId].Value += remainValue;
                            dataMap.Result[dataMap.PosOtherFsNoteId].Values.Add(remainValue);
                        }

                    }
                }

                if (!dataMap.HasOtherFsNoteId)
                {
                    // pos 
                    dataMap.Result.TryGetValue(dataMap.PosOtherFsNoteId, out var posModel);
                    // neg
                    dataMap.Result.TryGetValue(dataMap.NegOtherFsNoteId, out var negModel);

                    if (posModel != null && negModel != null)
                    {
                        TransferNumbers( posModel.Values, negModel.Values);
                    }

                }

                break;
            }
        }
    }
    static void TransferNumbers( List<double> positiveNumbers,  List<double> negativeNumbers)
    {
        // Chuyển tất cả các số dương từ negativeNumbers sang positiveNumbers
        var positiveFromNegative = negativeNumbers.Where(x => x > 0).ToList();
        positiveNumbers.AddRange(positiveFromNegative);

        // Chuyển tất cả các số âm từ positiveNumbers sang negativeNumbers
        var negativeFromPositive = positiveNumbers.Where(x => x < 0).ToList();
        negativeNumbers.AddRange(negativeFromPositive);

        // Xóa các số đã chuyển từ danh sách gốc
        positiveNumbers.RemoveAll(x => x < 0);
        negativeNumbers.RemoveAll(x => x > 0);
    }
    /// <summary>
    /// Map các chỉ tiêu theo hàng
    /// </summary>
    /// <param name="uow"></param>
    /// <param name="dataMap"></param>
    /// <param name="range"></param>
    private static MapEvaluators? HandleMappingRowDirection(UnitOfWorkModel uow, FsNoteDataMap dataMap, RangeDetectFsNote range)
    {
        if (range.MoneyResults == null)
        {
            return null;
        }
        MapEvaluators? mapEvaluators = null;
        var moneyCols = range.MoneyResults.DataRows;
        foreach (var moneys in moneyCols)
        {
            var moneyClones = moneys.Select(x => x.DeepClone()).ToList();
            // nên chuyển qua nơi khởi tạo
            moneyClones.Sort(MoneyCellModel.MoneyCellModelComparer);

            var request = new MapFsNoteWithMoneyChainRequest(range.ListTextCellSuggestModels!, moneyClones);
            var handler1 = new MapInColHandler();

            handler1.Handle(request);

            if (request.Handled && request.Result != null)
            {
                if (mapEvaluators == null || mapEvaluators.ListMapEvaluators.Count < request.Result.ListMapEvaluators.Count)
                {
                    mapEvaluators = request.Result;
                }
            }
        }
        return mapEvaluators;
    }
    /// <summary>
    /// Map các chỉ tiêu theo cột
    /// </summary>
    /// <param name="uow"></param>
    /// <param name="dataMap"></param>
    /// <param name="range"></param>
    private static MapEvaluators? HandleMappingColumnDirection(UnitOfWorkModel uow, FsNoteDataMap dataMap, RangeDetectFsNote range)
    {
        if (range.MoneyResults == null)
        {
            return null;
        }
        var moneyCols = range.MoneyResults.DataCols;
        MapEvaluators? mapEvaluators = null;
        foreach (var moneys in moneyCols)
        {
            var moneyClones = moneys.Select(x => x.DeepClone()).ToList();
            // nên chuyển qua nơi khởi tạo
            moneyClones.Sort(MoneyCellModel.MoneyCellModelComparer);

            var request = new MapFsNoteWithMoneyChainRequest(range.ListTextCellSuggestModels!, moneyClones);
            var handler1 = new MapInRowHandler();
            var handler2 = new MapWhenOcrLineBreakErrorHandler(uow, range, dataMap);
            handler1.SetNext(handler2);
            handler1.Handle(request);

            if (request.Handled && request.Result != null)
            {
                if (mapEvaluators == null || mapEvaluators.ListMapEvaluators.Count < request.Result.ListMapEvaluators.Count)
                {
                    mapEvaluators = request.Result;
                }
            }
        }
        return mapEvaluators;
    }

    public void CombineUnitOfWorks(UnitOfWorkModel uow)
    {
        throw new NotImplementedException();
    }

    public async Task LoadMapping2()
    {
        var fileMapping = ValidateFileMapping();
        _mapping.Clear();
        await LoadMapping2(fileMapping);
    }

    private async Task LoadMapping2(FileInfo fileInfo)
    {
        await using var fs = new FileStream(fileInfo.FullName, FileMode.Open, FileAccess.Read);
        using var workbook = await Task.Run(() => new XSSFWorkbook(fs));
        var sheet = workbook.GetSheetAt(0) ?? throw new ArgumentNullException("Sheet is null");
        int currentParentId = -1;
        Dictionary<int, int> countGroup = [];

        int startRow = _dataReaderMapSetting.NoteIdAddress.Row;
        for (var i = startRow; i <= sheet.LastRowNum; i++)
        {
            var row = sheet.GetRow(i) ?? throw new ArgumentNullException("Row is null");

            var noteId = row.GetCell(_dataReaderMapSetting.NoteIdAddress.Col).NumericCellValue;
            var name = row.GetCell(_dataReaderMapSetting.NameAddress.Col).StringCellValue;
            var isParent = FsNoteMappingBase.Parent.Equals(row.GetCell(_dataReaderMapSetting.CheckParentAddress.Col).StringCellValue.ToLower().Trim());
            var keyword = row.GetCell(_dataReaderMapSetting.KeywordsAddress.Col)?.StringCellValue ?? string.Empty;
            var extensionKeyword = row.GetCell(_dataReaderMapSetting.KeywordExtensionAddress.Col)?.StringCellValue ?? string.Empty;
            var other = row.GetCell(_dataReaderMapSetting.OtherAddress.Col)?.StringCellValue ?? string.Empty;

            if (isParent)
            {
                var previousParentId = currentParentId;
                currentParentId = (int)noteId;

                if (_mapping.TryGetValue(currentParentId, out FsNoteParentMappingModel? value) && currentParentId == previousParentId)
                {
                    countGroup[currentParentId]++;
                    value.Children.Add([]);
                    value.TotalGroup = countGroup[currentParentId];
                    continue;
                }
                _mapping.Add((int)noteId, new FsNoteParentMappingModel
                {
                    Id = (int)noteId,
                    Name = name,
                    Keywords = [.. keyword.Split(',').Select(x => x.Trim())],
                    KeywordExtensions = [.. extensionKeyword.Split(',').Select(x => x.Trim())],
                    Children = [[]],
                    IsDisabled = FsNoteMappingBase.Disabled.Equals(keyword),
                    TotalGroup = 0
                });
                countGroup.Add(currentParentId, 0);
            }
            else
            {
                var parentId = currentParentId;
                var isFormula = keyword.Equals(FsNoteMappingBase.Formula);
                var otherType = FsNoteMappingBase.ToMappingOtherType(other != string.Empty ? other[0] : ' ');
                var isOther = otherType != MappingOtherType.None;
                var parent = _mapping[parentId];
                var child = new FsNoteMappingModel
                {
                    Id = (int)noteId,
                    Name = name,
                    Keywords = [.. keyword.Split(',').Select(x => x.Trim())],
                    KeywordExtensions = [.. extensionKeyword.Split(',').Select(x => x.Trim())],
                    ParentId = parentId,
                    IsFormula = isFormula,
                    IsOther = isOther,
                    OtherType = otherType
                };
                parent.Children[parent.TotalGroup].Add(child);
            }
        }
    }
}
