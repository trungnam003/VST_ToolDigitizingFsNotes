﻿using System.Text;
using System.Text.RegularExpressions;

namespace VST_ToolDigitizingFsNotes.Libs.Utils
{
    public static partial class StringUtils
    {
        /// <summary>
        /// Xóa dấu tiếng Việt
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        public static string RemoveSign4VietnameseString(this string s)
        {
            Regex regex = IsCombiningDiacriticalMarksRegex();
            string temp = s.Normalize(NormalizationForm.FormD);
            return regex.Replace(temp, string.Empty).Replace('\u0111', 'd').Replace('\u0110', 'D');
        }
        /// <summary>
        /// Xóa ký tự đặc biệt
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        public static string RemoveSpecialCharacters(this string s)
        {
            return NormalCharacterRegex().Replace(s, "");
        }

        [GeneratedRegex("\\p{IsCombiningDiacriticalMarks}+")]
        private static partial Regex IsCombiningDiacriticalMarksRegex();

        [GeneratedRegex("[^a-zA-Z0-9_.\\s]+", RegexOptions.Compiled)]
        private static partial Regex NormalCharacterRegex();

        public static List<List<string>> ConvertToMatrix(string data,
            StringSplitOptions rowOption = StringSplitOptions.RemoveEmptyEntries, StringSplitOptions colOption = StringSplitOptions.None)
        {
            string[] lines = data.Split(new[] { Environment.NewLine }, rowOption);
            List<List<string>> matrix = [];

            foreach (var line in lines)
            {
                List<string> row = new(line.Split(new[] { '\t' }, colOption));
                matrix.Add(row);
            }
            return matrix;
        }
    }

    public static partial class StringUtils
    {
        /// <summary>
        /// Chứa các ký tự thay thế khi OCR nhận diện sai
        /// </summary>
        private static readonly Dictionary<char, List<char>> OcrErrors = new()
        {
            { '0', new List<char> { 'O', 'o' } },
            { '1', new List<char> { 'l', 'I', '!', '|', 'i' } },
            { '2', new List<char> { 'Z', 'z' } },
            { '3', new List<char> { 'B' } },
            { '4', new List<char> { 'A', 'e' } },
            { '5', new List<char> { 'S', 's' } },
            { '6', new List<char> { 'G', 'b' } },
            { '7', new List<char> { '/', '\\' } },
            { '8', new List<char> { 'X', 'B' } },
            { '9', new List<char> { 'g', 'q' } },
            { 'I', new List<char> { '1' } }
        };
    }
}


///// <summary>
///// Regex tiền tệ trong bctc chuẩn
///// </summary>
//public const string MoneyStringPattern001 = @"\b(\d{1,3}(?:[.,]\d{3})*[.,]\d{3})\b|\((\d{1,3}(?:[.,]\d{3})*[.,]\d{3})\)";

///// <summary>
///// Regex tiền tệ để fix lỗi OCR đọc sai
///// </summary>
//public const string MoneyStringPattern002 = @"\b(\d{1,3}(?:[.,]\d{3})*(?:(?:[.,]\d{1,3})|\b))\b|\((\d{1,3}(?:[.,]\d{3})*(?:(?:[.,]\d{1,3})|\b))\)";

///// <summary>
///// Regex tiền tệ bắt buộc cao
///// chỉ chấp nhận các số có dấu phẩy hoặc dấu chấm sau 3 số và không quá 6 lần lặp
///// </summary>
//public const string MoneyStringHardPattern = @"^(?<pos>\b(?:\d{1,3}(?:[.,]\d{3}){1,5})\b)$|^(?<neg>\(\b(?:\d{1,3}(?:[.,]\d{3}){1,5})\b\))$";

///// <summary>
///// Heading Regex Pattern 001
///// </summary>
//public const string HeadingPattern001 = @"^(?:(?:\d+(?:\.\d+)?\.? )|(?:[IVX]+\. ?)).*";

///// <summary>
///// Heading Regex Pattern 002
///// </summary>
//public const string HeadingPattern002 = @"^(?:(?:\d+(?:\.\d+)?\.\ )|(?:[IVX]+\.\ )).*";

///// <summary>
///// Heading Regex Pattern 003
///// </summary>
////public const string HeadingPattern003 = @"^(?:(?:\d+(?:\.\d+)?\.\ )|(?:[IVX]+\.\ )|(?:[a-zA-Z]\.\ )).*";
//public const string HeadingPattern003 = @"^((?:\d+(?:\.\d+)?\.\ )|(?:[IVX]+\.\ )|(?:[a-zA-Z]\.\ ))(.*?)(?=\n|$)";

///// <summary>
///// Regex xóa các khoảng trắng và ký tự đặc biệt liên tiếp từ 3 ký tự trở lên
///// </summary>
//public const string ManyDuplicateSpacePattern = @"(?>[^\w\s]{3,})|(?>[\s_]{3,})";

///// <summary>
///// Regex phát hiện các heading được nhóm lại
///// Ví dụ: I.1.1.a, V.01, A.1, B.2.1
///// Heading thường là I, V, X, A -> G, 1 -> 19, a -> g
///// </summary>
//public const string HeadingGroupPattern = @"^(?<h1>[VvXxIi]+\.|[A-K]+\.)?(?<h2>[0-9]\.|[0-4][0-9]\.)*([0-9]|[0-4][0-9])(\.[a-k])?(?=$)$";

//[GeneratedRegex(MoneyStringPattern001)]
//public static partial Regex MoneyRegex001();

//[GeneratedRegex(MoneyStringPattern002)]
//public static partial Regex MoneyRegex002();

//[GeneratedRegex(MoneyStringHardPattern)]
//public static partial Regex MoneyRegexHard();

//[GeneratedRegex(HeadingPattern001)]
//public static partial Regex HeadingRegex001();

//[GeneratedRegex(HeadingPattern002)]
//public static partial Regex HeadingRegex002();

//[GeneratedRegex(HeadingPattern003)]
//public static partial Regex HeadingRegex003();

//[GeneratedRegex(ManyDuplicateSpacePattern)]
//public static partial Regex ManyDuplicateSpaceRegex();

//[GeneratedRegex(HeadingGroupPattern)]
//public static partial Regex HeadingGroupRegex();