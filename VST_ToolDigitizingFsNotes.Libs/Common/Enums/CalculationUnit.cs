namespace VST_ToolDigitizingFsNotes.Libs.Common.Enums
{
    public enum CalculationUnit
    {
        One = 1,
        Thousand = 1_000,
        Million = 1_000_000,
    }

    public static class CalculationUnitExtension
    {
        public static string ToDisplayString(this CalculationUnit unit)
        {
            return unit switch
            {
                CalculationUnit.One => "Đơn vị",
                CalculationUnit.Thousand => "Nghìn",
                CalculationUnit.Million => "Triệu",
                _ => "Đơn vị",
            };
        }

        public static int ToInt32(this CalculationUnit unit)
        {
            return Convert.ToInt32(unit);
        }
    }
}
