using System.Diagnostics;
using VST_ToolDigitizingFsNotes.Libs.Common;

namespace VST_ToolDigitizingFsNotes.AppMain.Services
{
    /// <summary>
    /// Dịch vụ khởi chạy tiến trình ABBYY FineReader
    /// </summary>
    public class AbbyyService
    {
        public static Process? StartAbbyy(AbbyyCmdString abbyyCmdString)
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

        public static void StopAbbyy(Process? process)
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

        public static void ExitAllAbbyy()
        {
            try
            {
                Process[] procs = Process.GetProcesses();
                foreach (Process proc in procs)
                {
                    if (IsAbbyyProcess(proc.ProcessName) || IsAbbyyProcess(proc.MainWindowTitle))
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

        public static bool IsAbbyyProcess(string? processName)
        {
            var isAbbyy = false;
            if (processName != null)
            {
                processName = processName.ToLower();
                if (processName.Contains("fine", StringComparison.CurrentCultureIgnoreCase) ||
                                       processName.Contains("abbyy", StringComparison.CurrentCultureIgnoreCase) ||
                                                          processName.Contains("cmd", StringComparison.CurrentCultureIgnoreCase))
                {
                    isAbbyy = true;
                }
            }
            return isAbbyy;
        }
    }
}
