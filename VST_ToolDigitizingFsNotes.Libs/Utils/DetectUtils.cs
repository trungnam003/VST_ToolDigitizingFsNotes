using System.Text.RegularExpressions;

namespace VST_ToolDigitizingFsNotes.Libs.Utils;

/// <summary>
/// Chứa các phuong thức hỗ trợ việc phân tích dữ liệu trong file excel OCR
/// </summary>
public static partial class DetectUtils
{
    #region Regex Money

    //[GeneratedRegex(@"\b(\d{1,3}(?:[.,]\d{3})*[.,]\d{3})\b|\((\d{1,3}(?:[.,]\d{3})*[.,]\d{3})\)")]
    /// <summary>
    /// Regex phát hiện số tiền trong 1 chuỗi
    /// </summary>
    /// <returns></returns>
    //[GeneratedRegex(@"(?<pos>\b(?:\d{1,3}(?:[., ]\d{3}){1,5})\b)|(?<neg>\(\b(?:\d{1,3}(?:[., ]\d{3}){1,5})\b\))")]
    [GeneratedRegex(@"(?<pos>(?:\d{1,3}(?:[., ]\d{3}){1,5}))|(?<neg>\((?:\d{1,3}(?:[., ]\d{3}){1,5})\))")]
    public static partial Regex MoneyRegex001();

    [GeneratedRegex(@"(?<pos>(?:\d{1,6}(?:[., ]\d{3,6}){1,5}))|(?<neg>\((?:\d{1,6}(?:[., ]\d{3,6}){1,5})\))")]
    public static partial Regex MoneySoftRegex001();

    //[GeneratedRegex(@"\b(\d{1,3}(?:[.,]\d{3})*(?:(?:[.,]\d{1,3})|\b))\b|\((\d{1,3}(?:[.,]\d{3})*(?:(?:[.,]\d{1,3})|\b))\)")]
    [GeneratedRegex(@"(?<pos>(?:\d{1,3}(?:[.,]\d{3}){1,5}))|(?<neg>\((?:\d{1,3}(?:[.,]\d{3}){1,5})\))")]
    public static partial Regex MoneyRegex002();

    /// <summary>
    /// Regex phát hiện 1 chuỗi là số tiền
    /// </summary>
    /// <returns></returns>
    [GeneratedRegex(@"^(?<pos>\b(?:\d{1,3}(?:[.,]\d{3}){1,5})\b)$|^(?<neg>\(\b(?:\d{1,3}(?:[.,]\d{3}){1,5})\b\))$")]
    public static partial Regex MoneyRegexHard();

    #endregion

    #region Regex Heading

    [Obsolete("Không sử dụng nữa")]
    [GeneratedRegex(@"^(?:(?:\d+(?:\.\d+)?\.? )|(?:[IVX]+\. ?)).*")]
    public static partial Regex HeadingRegex001();

    [Obsolete("Không sử dụng nữa")]
    [GeneratedRegex(@"^(?:(?:\d+(?:\.\d+)?\.\ )|(?:[IVX]+\.\ )).*")]
    public static partial Regex HeadingRegex002();

    /// <summary>
    /// Chuỗi regex phát hiện heading trong file OCR
    /// Nhóm h_symbol
    ///     Nhóm h_number: bắt các heading bắt đầu bằng số và có dạng 1. | 1.2. | 1.3.1. 
    ///     Nhóm h_number2: bắt các heading bắt đầu bằng số có dạng 12a. 22a. 33a. 44a. 1a.
    ///     Nhóm h_roman: bắt các heading bắt đầu bằng số La Mã và có dạng I. | II. | III. | IV. | V. | VI. | VII. | VIII. | IX. | X. (tối đa 40)
    ///     Nhóm h_char1: bắt các heading bắt đầu bằng ký tự chữ cái và có dạng A. | B. | C. | a. | b. | c.
    ///     Nhóm h_char2: bắt các heading bắt đầu bằng ký tự chữ cái và số và có dạng (1). | (2). | (3). | (a). | (b). | (c). | (1) | (2) | (3) | (a) | (b) | (c)
    /// Phía sau h_symbol là 1 - n khoảng trắng hoặc tab
    /// Nhóm h_content: Chứa nội dung của heading
    /// Kết thúc 1 heading là ký tự xuống dòng hoặc kết thúc chuỗi
    /// </summary>
    /// <returns></returns>
    //[GeneratedRegex(@"^(?<h_symbol>(?<h_number>\d{1,2}(?:\.\d+){0,3}\.?)|(?<h_roman>(?:X{0,3})(?:IX|IV|V?I{0,3})\.)|(?<h_char1>[a-zA-Z]\.)|(?<h_char2>\([\da-zA-Z]+\)\.?))(?:[ \t]+)(?<h_content>.*?)(?=\n|$)")]
    //[GeneratedRegex(@"^(?<h_symbol>(?<h_number>\d{1,2}(?:\.\d{1,2}){0,3}\.?)|(?<h_roman>(?=.)(?:(?:X{0,3})(?:IX|IV|V?I{0,3}))\.?)|(?<h_char1>[a-zA-Z]\.)|(?<h_char2>\([\da-zA-Z]+\)\.?))(?:[ \t]+)(?<h_content>.{1,222}?)(?=\n|$)")]
    [GeneratedRegex(
        @"^(?<h_symbol>(?<h_number>\d{1,2}(?:\.\d{1,2}){0,3}\.?)|(?<h_number2>(?!0)[1-9]{1,2}[a-zA-Z]{1}\.)|(?<h_roman>(?=.)(?:(?:X{0,3})(?:IX|IV|V?I{0,3}))\.?)|(?<h_char1>[a-zA-Z]\.)|(?<h_char2>\([\da-zA-Z]+\)\.?))(?:[ \t]+)(?<h_content>.{1,222}?)(?=\n|$)"
    )]
    public static partial Regex HeadingRegex003();

    /// <summary>
    /// Regex phát hiện các ký tự đặc biệt hoặc khoảng trắng xuất hiện nhiều lần (>= 3)
    /// </summary>
    /// <returns></returns>
    [GeneratedRegex(@"(?>[^\w\s]{3,})|(?>[\s_]{3,})")]
    public static partial Regex ManyDuplicateSpaceRegex();

    [GeneratedRegex(@"^(?<h1>[VvXxIi]+\.|[A-K]+\.)?(?<h2>[0-9]\.|[0-4][0-9]\.)*([0-9]|[0-4][0-9])(\.[a-k])?(?=$)$")]
    public static partial Regex HeadingGroupRegex();

    #endregion
}

public partial class DetectUtils
{
    public static List<List<T>> FindAllSubsetSums<T>(IList<T> arr, double sum, Func<T, double> valueSelector, int maxLength = 23, CancellationToken cancellationToken = default)
    {
        if (arr == null || arr.Count == 0)
        {
            return [];
        }

        if (arr.Count > maxLength)
        {
            throw new ArgumentException("Số lượng phần tử trong mảng quá lớn");
        }

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

//[GeneratedRegex(@"^((?:\d+(?:\.\d+)?\.\ )|(?:[IVX]+\.\ )|(?:[a-zA-Z]\.\ ))(.*?)(?=\n|$)")]
//[GeneratedRegex(@"^(?<heading>(?<h_number>\d{1,2}(?:\.\d+){0,3}\.)|(?<h_roman>[IVX]+\.)|(?<h_char1>[a-zA-Z]\.)|(?<h_char2>\([\da-zA-Z]+\)\.?))(?:[ \t]+)(?<content>.*?)(?=\n|$)")]