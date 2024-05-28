namespace VST_ToolDigitizingFsNotes.Libs.Common;
public class AbbyyCmdString
{
    public string AbbyyPath { get; private set; }
    public string InputPath { get; private set; }
    public string OutputPath { get; private set; }
    public string Language { get; private set; }
    public bool QuitOnDone { get; private set; } = true;

    private AbbyyCmdString(Builder builder)
    {
        AbbyyPath = builder.AbbyyPath;
        InputPath = builder.InputPath;
        OutputPath = builder.OutputPath;
        Language = builder.Language;
        QuitOnDone = builder.QuitOnDone;
    }

    public override string ToString()
    {
        var str = $"\"{AbbyyPath}\" {InputPath} /lang {Language} /out {OutputPath}";
        str += QuitOnDone ? " /quit" : string.Empty;
        return str;
    }

    public class Builder
    {
        public string AbbyyPath { get; private set; } = string.Empty;
        public string InputPath { get; private set; } = string.Empty;
        public string OutputPath { get; private set; } = string.Empty;
        public string Language { get; private set; } = string.Empty;
        public bool QuitOnDone { get; private set; } = true;

        public Builder SetAbbyyPath(string abbyyPath)
        {
            AbbyyPath = abbyyPath;
            return this;
        }

        public Builder SetInputPath(string inputPath)
        {
            InputPath = inputPath;
            return this;
        }

        public Builder SetOutputPath(string outputPath)
        {
            OutputPath = outputPath;
            return this;
        }

        public Builder SetLanguage(string language)
        {
            Language = language;
            return this;
        }

        public Builder SetQuitOnDone(bool quitOnDone)
        {
            QuitOnDone = quitOnDone;
            return this;
        }

        public AbbyyCmdString Build()
        {
            return new AbbyyCmdString(this);
        }

        public Builder UseVietnameseLanguge()
        {
            SetLanguage("Vietnamese");
            return this;
        }
    }
}
