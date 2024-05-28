using System.Diagnostics;
using VST_ToolDigitizingFsNotes.Libs.Common;

namespace VST_ToolDigitizingFsNotes.AppMain.Services
{
    /// <summary>
    /// Dịch vụ khởi chạy tiến trình ABBYY FineReader
    /// </summary>
    public class AbbyyService
    {
        public Process? StartAbbyy(AbbyyCmdString abbyyCmdString)
        {
            try
            {
                // Khởi tạo tiến trình ABBYY FineReader
                ProcessStartInfo startInfo = new()
                {
                    FileName = "cmd.exe",
                    Arguments = $"/k {abbyyCmdString}",
                };

                var process = Process.Start(startInfo);
                return process;
            }
            catch (Exception)
            {
                throw;
            }
        }

        public void StopAbbyy(Process? process)
        {
            try
            {
                if (process != null)
                {
                    process.Kill();
                    process.Close();
                    process.Dispose();
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        public void ExitAllAbbyy()
        {
            try
            {
                Process[] procs = Process.GetProcesses();
                foreach (Process proc in procs)
                {
                    if ((proc.ProcessName != null
                        && (proc.ProcessName.ToLower().Contains("fine") ||
                        proc.ProcessName.ToLower().Contains("abbyy") ||
                        proc.ProcessName.ToLower().Contains("cmd"))) ||
                        (proc.MainWindowTitle != null &&
                        (proc.MainWindowTitle.ToLower().Contains("fine") ||
                        proc.MainWindowTitle.ToLower().Contains("abbyy") ||
                        proc.MainWindowTitle.ToLower().Contains("cmd"))))
                    {
                        StopAbbyy(proc);
                    }
                }
            }
            catch (Exception)
            {
                throw;
            }
        }
    }
}
