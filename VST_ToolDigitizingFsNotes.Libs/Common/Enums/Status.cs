using Ardalis.SmartEnum;

namespace VST_ToolDigitizingFsNotes.Libs.Common.Enums;

public class MapFsNoteStatus(string name, int value)
    : SmartEnum<MapFsNoteStatus>(name, value)
{
    /// <summary>
    /// Không map được dữ liệu
    /// </summary>
    public static readonly MapFsNoteStatus NotYetMapped = new(nameof(NotYetMapped), 0);
    /// <summary>
    /// Đã map dữ liệu
    /// </summary>
    public static readonly MapFsNoteStatus Mapped = new(nameof(Mapped), 1);
    /// <summary>
    /// Yêu cầu map lại
    /// </summary>
    public static readonly MapFsNoteStatus RequireMapAgain = new(nameof(RequireMapAgain), 2);
    /// <summary>
    /// Bỏ qua map dữ liệu
    /// </summary>
    //public static readonly MapFsNoteStatus IgnoreMap = new(nameof(IgnoreMap), 3);

    public static readonly MapFsNoteStatus CanMap = new(nameof(CanMap), 4);

    //public static readonly MapFsNoteStatus ReadyToMap = new(nameof(ReadyToMap), 5);
}

public class DetectRangeStatus(string name, int value)
    : SmartEnum<DetectRangeStatus>(name, value)
{
    /// <summary>
    /// Không phát hiện được và có thể phát hiện lại
    /// </summary>
    public static readonly DetectRangeStatus NotYetDetected = new(nameof(NotYetDetected), 0);
    /// <summary>
    /// Đã phát hiện và không cần phát hiện lại
    /// </summary>
    public static readonly DetectRangeStatus Detected = new(nameof(Detected), 1);
    /// <summary>
    /// Yêu cầu phát hiện lại
    /// </summary>
    public static readonly DetectRangeStatus RequireDetectAgain = new(nameof(RequireDetectAgain), 2);
    /// <summary>
    /// Bỏ qua phát hiện
    /// </summary>
    //public static readonly DetectRangeStatus IgnoreRange = new(nameof(IgnoreRange), 3);
    public static readonly DetectRangeStatus AllowNextHandle = new(nameof(AllowNextHandle), 4);

}

public class DetectStartRangeStatus(string name, int value)
    : SmartEnum<DetectStartRangeStatus>(name, value)
{
    public static readonly DetectStartRangeStatus SkipStringSimilarity = new(nameof(SkipStringSimilarity), 0);
    public static readonly DetectStartRangeStatus AllowStringSimilarity = new(nameof(AllowStringSimilarity), 1);
}