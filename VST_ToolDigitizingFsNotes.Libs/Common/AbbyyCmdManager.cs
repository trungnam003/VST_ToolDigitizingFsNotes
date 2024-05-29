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
            var startInfo = new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = $"/c {_abbyyCmdString}",
                UseShellExecute = false,
                CreateNoWindow = true,
            };

            _process = new Process
            {
                StartInfo = startInfo,
                EnableRaisingEvents = false
            };
            _process.Start();
            return _process;
        }
        catch (Exception)
        {
            throw;
        }
    }

}