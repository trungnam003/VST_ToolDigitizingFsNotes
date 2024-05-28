using System.Diagnostics;
using System.IO;

namespace VST_ToolDigitizingFsNotes.Libs.Common;

public class AbbyyCmdManager
{
    private Process _process;
    private AbbyyCmdString _abbyyCmdString;

    public event EventHandler<AbbyyCmdString> OutputFileCreated;

    public AbbyyCmdManager(AbbyyCmdString abbyyCmdString)
    {
        _abbyyCmdString = abbyyCmdString;
    }

    public Process StartAbbyyProcess()
    {
        try
        {
            _process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = $"/c {_abbyyCmdString}",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    
                }
            };
            _process.EnableRaisingEvents = true;
            _process.OutputDataReceived += (sender, e) =>
            {
                Console.WriteLine("OutputDataReceived");
                if (e.Data != null)
                {
                    if (e.Data.Contains("Output file:"))
                    {
                        OutputFileCreated?.Invoke(this, _abbyyCmdString);
                    }
                }
            };

            // on process exit
            _process.Exited += (sender, e) =>
            {
                Console.WriteLine("Process exited");
                // check output file created
                var fileInfo = _abbyyCmdString.OutputPath;
                if (File.Exists(fileInfo))
                {
                    Console.WriteLine($"Output file created: {fileInfo}");
                }
            };

            _process.Disposed += (sender, e) =>
            {
                Console.WriteLine("Process disposed");
            };
            _process.Start();
            //_process.WaitForExit();
            return _process;

        }
        catch (Exception)
        {
            throw;
        }
    }

}