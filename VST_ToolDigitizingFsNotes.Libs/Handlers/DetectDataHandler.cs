using MediatR;
using NPOI.XSSF.UserModel;
using System.IO;
using VST_ToolDigitizingFsNotes.Libs.Models;
using VST_ToolDigitizingFsNotes.Libs.Services;

namespace VST_ToolDigitizingFsNotes.Libs.Handlers
{
    public class DetectDataRequest : IRequest<bool>
    {
        public UnitOfWorkModel UnitOfWork { get; }
        public string FilePath { get; }
        public DetectDataRequest(string filePath, ref UnitOfWorkModel unitOfWork)
        {
            UnitOfWork = unitOfWork;
            FilePath = filePath;
        }
    }

    public class DetectDataHandler : IRequestHandler<DetectDataRequest, bool>
    {
        private readonly IDetectService _detectService;
        public DetectDataHandler(IDetectService detectService)
        {
            _detectService = detectService;
        }

        public async Task<bool> Handle(DetectDataRequest request, CancellationToken cancellationToken)
        {
            try
            {
                var fullPath = request.FilePath;
                var unitOfWork = request.UnitOfWork;
                var moneys = unitOfWork.MoneyCellModels;
                var headings = unitOfWork.HeadingCellModels;
                // read xlsx file using NPOI
                await using var fs = new FileStream(fullPath, FileMode.Open, FileAccess.Read);
                var workbook = await Task.Run(() => new XSSFWorkbook(fs));

                var sheet = workbook.GetSheetAt(0);
                for (int i = 0; i <= sheet.LastRowNum; i++)
                {
                    var row = sheet.GetRow(i);
                    if (row == null) continue;

                    // loop all row cells
                    for (int j = 0; j < row.LastCellNum; j++)
                    {
                        var cell = row.GetCell(j);
                        if (cell == null) continue;

                        var cellValue = cell.ToString();
                        if (string.IsNullOrEmpty(cellValue)) continue;
                        _detectService.DetectMoneys(cellValue, cell, ref moneys);
                        _detectService.DetectHeadings(cellValue, cell, ref headings);
                    }
                }
                return true;
            }
            catch (Exception)
            {
                throw;
            }
        }
    }
}
