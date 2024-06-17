using PdfSharp.Pdf;
namespace VST_ToolDigitizingFsNotes.Libs.Services;

public interface IPdfService
{
    Task<int> GetPdfPageCountAsync(string filePath);
    Task<bool> SplitPdfAsync(string filePath, int startPage, int endPage, string outputFolder = ".", string? fileName = null);
}