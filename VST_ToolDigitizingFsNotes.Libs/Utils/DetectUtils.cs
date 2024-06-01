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
    [GeneratedRegex(@"(?<pos>\b(?:\d{1,3}(?:[.,]\d{3}){1,5})\b)|(?<neg>\(\b(?:\d{1,3}(?:[.,]\d{3}){1,5})\b\))")]
    public static partial Regex MoneyRegex001();

    [Obsolete("Không sử dụng nữa")]
    [GeneratedRegex(@"\b(\d{1,3}(?:[.,]\d{3})*(?:(?:[.,]\d{1,3})|\b))\b|\((\d{1,3}(?:[.,]\d{3})*(?:(?:[.,]\d{1,3})|\b))\)")]
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
    ///     Nhóm h_roman: bắt các heading bắt đầu bằng số La Mã và có dạng I. | II. | III. | IV. | V. | VI. | VII. | VIII. | IX. | X. (tối đa 40)
    ///     Nhóm h_char1: bắt các heading bắt đầu bằng ký tự chữ cái và có dạng A. | B. | C. | a. | b. | c.
    ///     Nhóm h_char2: bắt các heading bắt đầu bằng ký tự chữ cái và số và có dạng (1). | (2). | (3). | (a). | (b). | (c). | (1) | (2) | (3) | (a) | (b) | (c)
    /// Phía sau h_symbol là 1 - n khoảng trắng hoặc tab
    /// Nhóm h_content: Chứa nội dung của heading
    /// Kết thúc 1 heading là ký tự xuống dòng hoặc kết thúc chuỗi
    /// </summary>
    /// <returns></returns>
    //[GeneratedRegex(@"^(?<h_symbol>(?<h_number>\d{1,2}(?:\.\d+){0,3}\.?)|(?<h_roman>(?:X{0,3})(?:IX|IV|V?I{0,3})\.)|(?<h_char1>[a-zA-Z]\.)|(?<h_char2>\([\da-zA-Z]+\)\.?))(?:[ \t]+)(?<h_content>.*?)(?=\n|$)")]
    [GeneratedRegex(@"^(?<h_symbol>(?<h_number>\d{1,2}(?:\.\d{1,2}){0,3}\.?)|(?<h_roman>(?=.)(?:(?:X{0,3})(?:IX|IV|V?I{0,3}))\.?)|(?<h_char1>[a-zA-Z]\.)|(?<h_char2>\([\da-zA-Z]+\)\.?))(?:[ \t]+)(?<h_content>.{1,80}?)(?=\n|$)")]
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

//[GeneratedRegex(@"^((?:\d+(?:\.\d+)?\.\ )|(?:[IVX]+\.\ )|(?:[a-zA-Z]\.\ ))(.*?)(?=\n|$)")]
//[GeneratedRegex(@"^(?<heading>(?<h_number>\d{1,2}(?:\.\d+){0,3}\.)|(?<h_roman>[IVX]+\.)|(?<h_char1>[a-zA-Z]\.)|(?<h_char2>\([\da-zA-Z]+\)\.?))(?:[ \t]+)(?<content>.*?)(?=\n|$)")]