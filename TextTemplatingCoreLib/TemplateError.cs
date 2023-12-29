//---------------------------------------------//
// Copyright 2023 RdJNL                        //
// https://github.com/RdJNL/TextTemplatingCore //
//---------------------------------------------//
namespace RdJNL.TextTemplatingCore.TextTemplatingCoreLib
{
    public sealed class TemplateError
    {
        public bool Warning { get; }
        public string Message { get; }
        public int Line { get; }
        public int Column { get; }

        public TemplateError(bool warning, string message, int line, int column)
        {
            Warning = warning;
            Message = message;
            Line = line;
            Column = column;
        }

        public TemplateError(bool warning, string message)
            : this(warning, message, 1, 1)
        {
        }
    }
}
