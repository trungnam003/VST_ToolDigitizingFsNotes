using MediatR;
using NPOI.HSSF.UserModel;
using NPOI.SS.UserModel;
using System.Drawing;
using VST_ToolDigitizingFsNotes.Libs.Common;
using VST_ToolDigitizingFsNotes.Libs.Models;
using VST_ToolDigitizingFsNotes.Libs.Utils;

namespace VST_ToolDigitizingFsNotes.Libs.Handlers
{

    public class LoadReferenceFsNoteDataRequest : IRequest<bool>
    {
        public HSSFWorkbook InputWorkbook { get; }
        public string SheetName { get; }
        public List<FsNoteParentModel> FsNoteParentModels { get; }

        public LoadReferenceFsNoteDataRequest(HSSFWorkbook inputWorkbook, string sheetName,  ref List<FsNoteParentModel> fsNoteParentModels)
        {
            InputWorkbook = inputWorkbook;
            SheetName = sheetName;
            FsNoteParentModels = fsNoteParentModels;
        }
    }

    public class LoadReferenceFsNoteDataHandler : IRequestHandler<LoadReferenceFsNoteDataRequest, bool>
    {
        private readonly DataReaderSheetSetting _dataReaderSheetSetting;
        public LoadReferenceFsNoteDataHandler(DataReaderSheetSetting dataReaderSheetSetting)
        {
            _dataReaderSheetSetting = dataReaderSheetSetting;
        }

        public Task<bool> Handle(LoadReferenceFsNoteDataRequest request, CancellationToken cancellationToken)
        {
            try
            {
                var sheetName = request.SheetName;
                var fsNoteParentModels = request.FsNoteParentModels;
                var workbook = request.InputWorkbook;
                var list = LoadDataFromSheetName(workbook, sheetName);
                foreach (var item in list)
                {
                    fsNoteParentModels.Add(item);
                    //Debug.WriteLine($"{item.FsNoteId} - {item.Name} - {item.Value} - {item.ParentId} - {item.IsParent} - {item.Group}");
                    //foreach (var child in item.Children)
                    //{
                    //    Debug.WriteLine($"\t{child.FsNoteId} - {child.Name} - {child.Value} - {child.Group}");
                    //}
                }
                return Task.FromResult(true);
            }
            catch (Exception)
            {
                throw;
            }
        }
        public static bool IsValidCell(IRow row, int colName, int colNoteId)
        {

            if (row == null)
            {
                return false;
            }

            string? name = row.GetCell(colName).ToString();
            // kiểm tra nếu ô màu đỏ thì bỏ qua
            var cellColor = row.GetCell(colName).CellStyle.FillForegroundColorColor;
            Color colorTarget = Color.FromArgb(cellColor.RGB[0], cellColor.RGB[1], cellColor.RGB[2]);

            bool validName = !string.IsNullOrEmpty(name);
            bool validNoteId = int.TryParse(row.GetCell(colNoteId).ToString(), out int noteId) && noteId != 0;
            bool validColor = !CoreUtils.IsColorInRangeRed(colorTarget);

            return validName && validNoteId && validColor;
        }
        private List<FsNoteParentModel> LoadDataFromSheetName(HSSFWorkbook workbook, string sheetName)
        {
            ArgumentException.ThrowIfNullOrEmpty(sheetName);
            ArgumentNullException.ThrowIfNull(workbook);

            var listResult = new List<FsNoteParentModel>();
            var sheet = workbook.GetSheet(sheetName)
                ?? throw new ArgumentException($"Sheet name '{sheetName}' not found in workbook");


            int currentParentId = 0;
            Dictionary<int, int> countGroup = [];
            FsNoteParentModel? currentParent = null;
            for (int i = _dataReaderSheetSetting.CheckParentAddress.Row; i <= sheet.LastRowNum; i++)
            {
                try
                {
                    var row = sheet.GetRow(i);
                    if (row == null) continue;

                    if (!IsValidCell(row, _dataReaderSheetSetting.NameAddress.Col, _dataReaderSheetSetting.NoteIdAddress.Col))
                    {
                        continue;
                    }

                    int noteId = (int)row.GetCell(_dataReaderSheetSetting.NoteIdAddress.Col).NumericCellValue;
                    string name = row.GetCell(_dataReaderSheetSetting.NameAddress.Col).ToString()!;

                    var cellCheckParent = row.GetCell(_dataReaderSheetSetting.CheckParentAddress.Col);

                    if (cellCheckParent == null)
                    {
                        continue;
                    }

                    if (!string.IsNullOrEmpty(cellCheckParent.ToString()))
                    {
                        //if (!Mapping.ContainsKey(noteId))
                        //{
                        //    continue;
                        //}
                        currentParent = new FsNoteParentModel
                        {
                            FsNoteId = noteId,
                            Name = name,
                            IsParent = true,
                            ParentId = 0,
                            Group = 0,
                            Cell = new(i, _dataReaderSheetSetting.ValueAddress.Col),
                            CellAddress = $"{(char)('A' + _dataReaderSheetSetting.ValueAddress.Col)}{i}",
                            Children = []
                        };
                        currentParentId = noteId;
                        if (countGroup.TryGetValue(currentParentId, out int value))
                        {
                            countGroup[currentParentId] = ++value;
                        }
                        else
                        {
                            countGroup[currentParentId] = 1;
                        }
                        var cellParentValue = row.GetCell(_dataReaderSheetSetting.ParentValueAddress.Col);
                        if (cellParentValue == null)
                        {
                            currentParent.Value = 0;
                        }
                        else
                        {
                            currentParent.Value = cellParentValue.NumericCellValue;
                        }
                        currentParent.Group = countGroup.TryGetValue(currentParentId, out int group) ? group : 0;
                        listResult.Add(currentParent);
                    }
                    else
                    {
                        var cellValue = row.GetCell(_dataReaderSheetSetting.ValueAddress.Col);
                        if (cellValue.CellType == CellType.Formula)
                        {
                            continue;
                        }
                        var model = new FsNoteModel
                        {
                            FsNoteId = noteId,
                            Name = name,
                            IsParent = false,
                            ParentId = currentParentId,
                            Cell = new(i, _dataReaderSheetSetting.ValueAddress.Col),
                            CellAddress = $"{(char)('A' + _dataReaderSheetSetting.ValueAddress.Col)}{i}",
                            Group = countGroup.TryGetValue(currentParentId, out int group) ? group : 0
                        };
                        currentParent?.Children.Add(model);
                    }
                }
                catch (Exception)
                {
                    throw;
                }
            }

            return listResult;
        }
    }
}
