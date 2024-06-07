using MediatR;
using VST_ToolDigitizingFsNotes.Libs.Models;
using VST_ToolDigitizingFsNotes.Libs.Services;

namespace VST_ToolDigitizingFsNotes.Libs.Handlers
{
    public class DetectDataRequest : IRequest<bool>
    {
        public UnitOfWorkModel UnitOfWork { get; }
        public DetectDataRequest(ref UnitOfWorkModel unitOfWork)
        {
            UnitOfWork = unitOfWork;
        }
    }

    public class DetectDataHandler : IRequestHandler<DetectDataRequest, bool>
    {
        private readonly IDetectService _detectService;
        public DetectDataHandler(IDetectService detectService)
        {
            _detectService = detectService;
        }

        public Task<bool> Handle(DetectDataRequest request, CancellationToken cancellationToken)
        {
            try
            {
                var unitOfWork = request.UnitOfWork;
                var moneys = unitOfWork.MoneyCellModels;
                var headings = unitOfWork.HeadingCellModels;
                var workbook = unitOfWork.OcrWorkbook;
                ArgumentNullException.ThrowIfNull(workbook, nameof(workbook));
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

                        var cellValue = cell.ToString()?.Trim() ?? string.Empty;
                        if (string.IsNullOrEmpty(cellValue)) continue;
                        _detectService.DetectMoneys(cellValue, cell, ref moneys);
                        _detectService.DetectHeadings(cellValue, cell, ref headings);
                    }
                }
                return Task.FromResult(true);
            }
            catch (Exception)
            {
                throw;
            }
        }
    }
}
