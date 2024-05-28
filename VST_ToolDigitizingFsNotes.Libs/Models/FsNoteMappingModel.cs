namespace VST_ToolDigitizingFsNotes.Libs.Models
{
    public abstract class FsNoteMappingBase
    {
        public const string Formula = "#";
        public const string Other = "*";
        public const string Disabled = "x";
        public const string Parent = "x";

        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public List<string> Keywords { get; set; } = [];
        public List<string> KeywordExtensions { get; set; } = [];

    }

    public class FsNoteMappingModel : FsNoteMappingBase
    {
        public int ParentId { get; set; }
        public bool IsFormula { get; set; }
        public bool IsOther { get; set; }
    }

    public class FsNoteParentMappingModel : FsNoteMappingBase
    {
        public List<List<FsNoteMappingModel>> Children { get; set; } = [];
        public bool IsDisabled { get; set; }
        public int TotalGroup { get; set; }

        public override string ToString()
        {
            var str = $"ID: {Id}; Total Group: {Children.Count}; IsDisabled {IsDisabled}";

            foreach (var child in Children)
            {
                var c = $";child: {child.Count}";
                str += c;
            }

            return str;
        }
    }
}
