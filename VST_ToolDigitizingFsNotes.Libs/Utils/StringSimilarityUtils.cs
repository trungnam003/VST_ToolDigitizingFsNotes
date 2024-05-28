using F23.StringSimilarity;

namespace VST_ToolDigitizingFsNotes.Libs.Utils
{
    public static class StringSimilarityUtils
    {
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
    }
}
