using System.Diagnostics;
using VST_ToolDigitizingFsNotes.Libs.Utils;
using F23.StringSimilarity;
using DiffMatchPatch;
namespace VST_ToolDigitizingFsNotes.ConsoleApp
{
    internal class Program
    {
        static void Main(string[] args)
        {
            string faulty = "262056S34167x";
            List<string> possibleCorrections = GeneratePossibleCorrections(faulty);

            Console.WriteLine("Possible corrections:");
            foreach (var correction in possibleCorrections)
            {
                Console.WriteLine(correction);
            }
        }

        static List<string> GeneratePossibleCorrections(string faulty)
        {
            var corrections = new List<string>();
            GenerateCorrections(faulty.ToCharArray(), 0, corrections);
            return corrections;
        }

        static void GenerateCorrections(char[] faulty, int index, List<string> corrections)
        {
            if (index == faulty.Length)
            {
                corrections.Add(new string(faulty));
                return;
            }

            if (char.IsDigit(faulty[index]))
            {
                GenerateCorrections(faulty, index + 1, corrections);
            }
            else
            {
                for (char replacement = '0'; replacement <= '9'; replacement++)
                {
                    char originalChar = faulty[index];
                    faulty[index] = replacement;
                    GenerateCorrections(faulty, index + 1, corrections);
                    faulty[index] = originalChar; // Khôi phục lại ký tự gốc
                }
            }
        }
        //static void Main(string[] args)
        //{
        //    //int a = 0;
        //    //while (true)
        //    //{
        //    //    var abbyy15Path = "D:\\Abbyy\\Abbyy15\\ABBYY FineReader 15\\FineCmd.exe";
        //    //    var input = "C:\\Users\\trungnamth\\Downloads\\test\\nam.pdf";
        //    //    var output = $"C:\\Users\\trungnamth\\Downloads\\test\\nam-{a++}.xlsx";

        //    //    var abbyyCmdString = new AbbyyCmdString.Builder()
        //    //                        .SetAbbyyPath(abbyy15Path)
        //    //                        .SetInputPath(input)
        //    //                        .UseVietnameseLanguge()
        //    //                        .SetOutputPath(output)
        //    //                        .SetQuitOnDone(true)
        //    //                        .Build();

        //    //    var abbyyManager = new AbbyyCmdManager(abbyyCmdString);

        //    //    //abbyyManager.OutputFileCreated += (sender, e) =>
        //    //    //{
        //    //    //    Console.WriteLine($"Output file created: {e}");
        //    //    //};

        //    //    using var p = abbyyManager.StartAbbyyProcess();
        //    //    p.WaitForExit();

        //    //    var cts = new CancellationTokenSource();
        //    //    cts.CancelAfter(TimeSpan.FromSeconds(10));

        //    //    p.WaitForExitAsync(CancellationToken.None);
        //    //    Console.WriteLine("Continue___");
        //    //    Thread.Sleep(5000);
        //    //}

        //    // calulate time
        //    //var watch = new Stopwatch();
        //    //watch.Start();
        //    //double[] arr = {
        //    //    1_789_473_698, 1_789_473_698, 1_789_473_698, 2_526_315_786, 2_526_315_786
        //    //};
        //    //Console.WriteLine(arr.Length);
        //    //TimeSpan timeout = TimeSpan.FromSeconds(5);
        //    //var cancellationTokenSource = new CancellationTokenSource(timeout);
        //    //double sum = 4_315_789_484;
        //    //List<List<double>> results = Solution.FindAllSubsetSums(arr, sum, x => x, cancellationTokenSource.Token);

        //    //watch.Stop();
        //    //Console.WriteLine($"Execution Time: {watch.ElapsedMilliseconds} ms");

        //    //if (results.Count > 0)
        //    //{
        //    //    Console.WriteLine("Các tập hợp con có tổng bằng " + sum + " là:");
        //    //    foreach (var subset in results)
        //    //    {
        //    //        Console.WriteLine(string.Join(", ", subset));
        //    //    }
        //    //}
        //    //else
        //    //{
        //    //    Console.WriteLine("Không có tập hợp con nào có tổng bằng " + sum);
        //    //}

        //    var s =  "262056S34167x".ToSystemNomalizeString();
        //    var s2 = "262056S3xxx7x".ToSystemNomalizeString();

        //   // calculate distance between two strings
        //    var jw = new Levenshtein();
        //    var similarity = jw.Distance(s, s2);
        //    Console.WriteLine(similarity);

        //    // correct string
        //    var reference = "2620565341678";
        //    var faulty =    "262056S3xxx7x";
        //    var corrected = CorrectString(reference, faulty);
        //    Console.WriteLine(corrected);

        //    string originalString = "262056S34167x";
        //    string pattern = @"\d+"; // Chuỗi mẫu chỉ chứa số

        //    var dmp = new diff_match_patch();

        //    // Tìm chuỗi khớp tốt nhất với chuỗi mẫu
        //    var matches = dmp.diff_main(pattern, originalString);

        //    // Tạo bản vá từ danh sách các thay đổi
        //    var patches = dmp.patch_make(matches);

        //    // Áp dụng bản vá lên chuỗi mẫu để tạo ra chuỗi số phù hợp
        //    var r = dmp.patch_apply(patches, pattern);


        //}

        //static string CorrectString(string reference, string faulty)
        //{

        //    var dmp = new diff_match_patch();
        //    var diffs = dmp.diff_main(reference, faulty);
        //    dmp.diff_cleanupSemantic(diffs);

        //    // Tạo một chuỗi sửa đổi từ chuỗi lỗi
        //    var corrected = faulty.ToCharArray();
        //    int referenceIndex = 0, faultyIndex = 0;

        //    foreach (var diff in diffs)
        //    {
        //        if (diff.operation == Operation.DELETE)
        //        {
        //            for (int i = 0; i < diff.text.Length; i++)
        //            {
        //                if (faultyIndex < corrected.Length)
        //                {
        //                    corrected[faultyIndex] = reference[referenceIndex];
        //                }
        //                referenceIndex++;
        //                faultyIndex++;
        //            }
        //        }
        //        else if (diff.operation == Operation.INSERT)
        //        {
        //            referenceIndex += diff.text.Length - 1;
        //        }
        //        else if (diff.operation == Operation.EQUAL)
        //        {
        //            referenceIndex += diff.text.Length;
        //            faultyIndex += diff.text.Length;
        //        }
        //    }
        //    return new string(corrected);
        //}
    }
    
}
