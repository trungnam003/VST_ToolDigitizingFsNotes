using Force.DeepCloner;
using NPOI.XSSF.UserModel;
using System.Diagnostics;
using System.IO;
using VST_ToolDigitizingFsNotes.Libs.Chains;
using VST_ToolDigitizingFsNotes.Libs.Common;
using VST_ToolDigitizingFsNotes.Libs.Common.Enums;
using VST_ToolDigitizingFsNotes.Libs.Models;
using VST_ToolDigitizingFsNotes.Libs.Services;
using VST_ToolDigitizingFsNotes.Libs.Utils;

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
    public MappingService(Dictionary<int, FsNoteParentMappingModel> mapping, UserSettings userSettings)
    {
        _mapping = mapping;
        _userSettings = userSettings;
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
        const byte rowDirection = 1;
        const byte colDirection = 2;
        const byte unknownDirection = 0;
        var ranges = dataMap?.RangeDetectFsNotes?.Where(x => x.DetectRangeStatus == DetectRangeStatus.AllowNextHandle).ToList();
        if (ranges == null || ranges.Count == 0)
        {
            return;
        }
        Debug.WriteLine(dataMap!.FsNoteParentModel.Name);
        foreach (var range in ranges)
        {
            var suggests = range.ListTextCellSuggestModels;
            List<(int, int)>? input = suggests?.Select(x => (x.Row, x.Col)).ToList();

            if (input == null || input.Count == 0)
            {
                continue;
            }

            var direction = CoreUtils.DetermineDirection(input);
            if (direction == rowDirection)
            {

            }
            else if (direction == colDirection)
            {
                HandleMappingColumnDirection(uow, dataMap, range);
            }
            else if (direction == unknownDirection && input.Count == 1)
            {

            }
        }
        Debug.WriteLine("---------------------------------");
    }

    private void HandleMappingColumnDirection(UnitOfWorkModel uow, FsNoteDataMap dataMap, RangeDetectFsNote range)
    {
        if (range.MoneyResults == null)
        {
            return;
        }
        var moneyCols = range.MoneyResults.DataCols;
        foreach (var moneys in moneyCols)
        {
            var moneyClones = moneys.Select(x => x.DeepClone())
                //.OrderBy(cell => cell.Row)
                //.ThenBy(cell => cell.Col)
                //.ThenBy(cell => cell.IndexInCell)
                .ToList();
            // nên chuyển qua nơi khởi tạo
            static int comparison(MoneyCellModel x, MoneyCellModel y)
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
            moneyClones.Sort(comparison);

            var request = new MapFsNoteWithMoneyChainRequest(range.ListTextCellSuggestModels!, moneyClones);

        }
    }
}
