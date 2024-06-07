using DiffMatchPatch;
using F23.StringSimilarity;

namespace VST_ToolDigitizingFsNotes.Libs.Utils
{
    public static class StringSimilarityUtils
    {
        public const double AcceptableSimilarity = 0.6868;
        private static double CombineSimilarity(double similarity1, double similarity2)
        {
            return (similarity1 + similarity2) / 2;
        }

        public static double CalculateSimilarity(string str1, string str2)
        {
            var similarity1 = new Cosine().Similarity(str1, str2);
            var similarity2 = new RatcliffObershelp().Similarity(str1, str2);
            return CombineSimilarity(similarity1, similarity2);
        }

        /// <summary>
        /// Tìm kiếm chuỗi tương đồng từ plain text
        /// Ví dụ: plainText "hang mua dang di du0ng nguyen lieu vat lieu c0ng cu dung cu chi phi san xuat k!nh doanh do dang" => nên xử lý chuỗi trước khi tìm kiếm
        ///        target: "san xuat kinh doanh"
        ///        => trả về "san xuat knh doanh"
        ///        => Kết hợp với CalculateSimilarity để xác định độ tương đồng
        /// </summary>
        /// <param name="plainText"></param>
        /// <param name="target"></param>
        /// <returns></returns>
        public static bool TryFindStringSimilarityFromPlainText(string plainText, string target, out string output, double defaultThreshold = AcceptableSimilarity)
        {
            output = FindDiffMatchPatchFromPlainText(plainText, target);
            var similarity = CalculateSimilarity(output, target);

            var result = similarity >= defaultThreshold;
            output = result ? output : "";
            return result;
        }

        public static string FindDiffMatchPatchFromPlainText(string plainText, string target)
        {
            diff_match_patch dmp = new();
            var diffs = dmp.diff_main(plainText, target);
            dmp.diff_cleanupSemantic(diffs);
            for (int i = 0; i < diffs.Count; i++)
            {
                if (diffs[i].operation == Operation.INSERT || diffs[i].operation == Operation.DELETE)
                {
                    diffs.RemoveAt(i);
                    i--;
                }
            }
            var output = dmp.diff_text2(diffs);
            return output;
        }
    }
}
