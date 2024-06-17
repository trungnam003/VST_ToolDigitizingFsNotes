using PdfSharp.Pdf;
using PdfSharp.Pdf.IO;
using System.IO;
using VST_ToolDigitizingFsNotes.Libs.Services;

namespace VST_ToolDigitizingFsNotes.AppMain.Services;


public class PdfService : IPdfService
{
    public async Task<bool> SplitPdfAsync(string filePath,  int startPage, int endPage, string outputFolder = ".", string? fileName = null)
    {
        var fileInfo = new FileInfo(filePath);
        if (!fileInfo.Exists)
        {
            throw new FileNotFoundException("File not found", filePath);
        }
        using var document = await Task.Run(() => PdfReader.Open(fileInfo.FullName, PdfDocumentOpenMode.Import));
        if (endPage > document.PageCount)
        {
            throw new ArgumentOutOfRangeException(nameof(endPage), "End page is greater than total page count");
        }
        if (startPage < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(startPage), "Start page is less than 1");
        }
        if (startPage > endPage)
        {
            throw new ArgumentOutOfRangeException(nameof(startPage), "Start page is greater than end page");
        }

        var outputFileName = fileName ?? fileInfo.Name;

        if(outputFolder == ".")
        {
            outputFolder = fileInfo.DirectoryName ?? throw new DirectoryNotFoundException("Directory not found");
        }
        else if (!Directory.Exists(outputFolder))
        {
            Directory.CreateDirectory(outputFolder);
        }

        var outputFilePath = Path.Combine(outputFolder, outputFileName);
        using var outputDocument = new PdfDocument();
        for (int i = startPage - 1; i < endPage; i++)
        {
            outputDocument.AddPage(document.Pages[i]);
        }
        outputDocument.Save(outputFilePath);
        return (true);
    }

    public async Task<int> GetPdfPageCountAsync(string filePath)
    {
        var fileInfo = new FileInfo(filePath);
        if (!fileInfo.Exists)
        {
            throw new FileNotFoundException("File not found", filePath);
        }
        using var document = await Task.Run(() => PdfReader.Open(fileInfo.FullName, PdfDocumentOpenMode.Import));
        return(document.PageCount);
    }
}