using System.Diagnostics;

namespace VST_ToolDigitizingFsNotes.ConsoleApp
{
    internal class Program
    {
        static void Main(string[] args)
        {
            //int a = 0;
            //while (true)
            //{
            //    var abbyy15Path = "D:\\Abbyy\\Abbyy15\\ABBYY FineReader 15\\FineCmd.exe";
            //    var input = "C:\\Users\\trungnamth\\Downloads\\test\\nam.pdf";
            //    var output = $"C:\\Users\\trungnamth\\Downloads\\test\\nam-{a++}.xlsx";

            //    var abbyyCmdString = new AbbyyCmdString.Builder()
            //                        .SetAbbyyPath(abbyy15Path)
            //                        .SetInputPath(input)
            //                        .UseVietnameseLanguge()
            //                        .SetOutputPath(output)
            //                        .SetQuitOnDone(true)
            //                        .Build();

            //    var abbyyManager = new AbbyyCmdManager(abbyyCmdString);

            //    //abbyyManager.OutputFileCreated += (sender, e) =>
            //    //{
            //    //    Console.WriteLine($"Output file created: {e}");
            //    //};

            //    using var p = abbyyManager.StartAbbyyProcess();
            //    p.WaitForExit();

            //    var cts = new CancellationTokenSource();
            //    cts.CancelAfter(TimeSpan.FromSeconds(10));

            //    p.WaitForExitAsync(CancellationToken.None);
            //    Console.WriteLine("Continue___");
            //    Thread.Sleep(5000);
            //}

            // calulate time
            var watch = new Stopwatch();
            watch.Start();
            double[] arr = {
                19802696762, 65684029692, 331828286952, 23992425272, 62183211463, 330672898968
            };
            Console.WriteLine(arr.Length);
            TimeSpan timeout = TimeSpan.FromSeconds(5);
            var cancellationTokenSource = new CancellationTokenSource(timeout);
            double sum = 417315013406;
            List<List<double>> results = Solution.FindAllSubsetSums(arr, sum, x => x, cancellationTokenSource.Token);

            watch.Stop();
            Console.WriteLine($"Execution Time: {watch.ElapsedMilliseconds} ms");

            if (results.Count > 0)
            {
                Console.WriteLine("Các tập hợp con có tổng bằng " + sum + " là:");
                foreach (var subset in results)
                {
                    Console.WriteLine(string.Join(", ", subset));
                }
            }
            else
            {
                Console.WriteLine("Không có tập hợp con nào có tổng bằng " + sum);
            }

        }
    }
    public class Solution
    {
        public static List<List<T>> FindAllSubsetSums<T>(IList<T> arr, double sum, Func<T, double> valueSelector, CancellationToken cancellationToken = default)
        {
            List<List<T>> result = [];
            List<T> current = [];
            FindSubsets(arr, sum, 0, current, result, valueSelector, cancellationToken);
            return result;
        }

        static void FindSubsets<T>(IList<T> arr, double sum, int index, List<T> current, List<List<T>> result, Func<T, double> valueSelector, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (sum == 0)
            {
                result.Add(new List<T>(current));
                return;
            }

            if (index == arr.Count)
            {
                return;
            }

            // Bao gồm phần tử hiện tại
            current.Add(arr[index]);
            FindSubsets(arr, sum - valueSelector(arr[index]), index + 1, current, result, valueSelector, cancellationToken);
            current.RemoveAt(current.Count - 1);

            // Bỏ qua phần tử hiện tại
            FindSubsets(arr, sum, index + 1, current, result, valueSelector, cancellationToken);
        }
    }
}
