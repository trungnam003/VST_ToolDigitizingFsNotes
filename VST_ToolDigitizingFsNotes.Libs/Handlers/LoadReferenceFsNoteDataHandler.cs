using MediatR;
using NPOI.HSSF.UserModel;
using NPOI.SS.UserModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using VST_ToolDigitizingFsNotes.Libs.Models;
using VST_ToolDigitizingFsNotes.Libs.Utils;

namespace VST_ToolDigitizingFsNotes.Libs.Handlers
{

    public class LoadReferenceFsNoteDataRequest : IRequest<bool>
    {
        public HSSFWorkbook InputWorkbook { get; }
        public UnitOfWorkModel UnitOfWork { get; }
        public string SheetName { get; }

        public LoadReferenceFsNoteDataRequest(HSSFWorkbook inputWorkbook, string sheetName, ref UnitOfWorkModel unitOfWork)
        {
            InputWorkbook = inputWorkbook;
            UnitOfWork = unitOfWork;
            SheetName = sheetName;
        }
    }

    public class LoadReferenceFsNoteDataHandler : IRequestHandler<LoadReferenceFsNoteDataRequest, bool>
    {
        public LoadReferenceFsNoteDataHandler()
        {
        }

        public Task<bool> Handle(LoadReferenceFsNoteDataRequest request, CancellationToken cancellationToken)
        {
            try
            {
                var sheetName = request.SheetName;
                var unitOfWork = request.UnitOfWork;
                var workbook = request.InputWorkbook;
                var list = LoadDataFromSheetName(workbook, sheetName);
                foreach (var item in list)
                {
                    unitOfWork.FsNoteParentModels.Add(item);
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
        private static bool IsValidCell(IRow row, int colName, int colNoteId)
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
        private static List<FsNoteParentModel> LoadDataFromSheetName(HSSFWorkbook workbook, string sheetName)
        {
            ArgumentException.ThrowIfNullOrEmpty(sheetName);
            ArgumentNullException.ThrowIfNull(workbook);

            var listResult = new List<FsNoteParentModel>();
            var sheet = workbook.GetSheet(sheetName)
                ?? throw new ArgumentException($"Sheet name '{sheetName}' not found in workbook");

            const int COL_NOTE_ID = 3;
            const int COL_CHECK_PARENT_NOTE = 2;
            const int COL_NOTE_NAME = 4;
            const int COL_NOTE_PARENT_VALUE = 6;
            const int START_ROW_INDEX = 14;
            const int COL_NOTE_VALUE = 5;

            int currentParentId = 0;
            Dictionary<int, int> countGroup = [];
            FsNoteParentModel? currentParent = null;
            for (int i = START_ROW_INDEX; i <= sheet.LastRowNum; i++)
            {
                try
                {
                    var row = sheet.GetRow(i);
                    if (row == null) continue;

                    if (!IsValidCell(row, COL_NOTE_NAME, COL_NOTE_ID))
                    {
                        continue;
                    }

                    int noteId = (int)row.GetCell(COL_NOTE_ID).NumericCellValue;
                    string name = row.GetCell(COL_NOTE_NAME).ToString()!;

                    var cellCheckParent = row.GetCell(COL_CHECK_PARENT_NOTE);

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
                            Cell = new(i, COL_NOTE_VALUE),
                            CellAddress = $"{(char)('A' + COL_NOTE_VALUE)}{i}",
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
                        var cellParentValue = row.GetCell(COL_NOTE_PARENT_VALUE);
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
                        var cellValue = row.GetCell(COL_NOTE_VALUE);
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
                            Cell = new(i, COL_NOTE_VALUE),
                            CellAddress = $"{(char)('A' + COL_NOTE_VALUE)}{i}",
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
