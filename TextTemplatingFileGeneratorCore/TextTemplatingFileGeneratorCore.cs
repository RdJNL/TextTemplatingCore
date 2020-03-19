//---------------------------------------------//
// Copyright 2020 RdJNL                        //
// https://github.com/RdJNL/TextTemplatingCore //
//---------------------------------------------//
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using EnvDTE;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TextTemplating.VSHost;
using RdJNL.TextTemplatingCore.TextTemplatingCoreLib;

namespace RdJNL.TextTemplatingCore.TextTemplatingFileGeneratorCore
{
    [Guid(GENERATOR_GUID)]
    public sealed class TextTemplatingFileGeneratorCore : BaseTemplatedCodeGenerator, IVsSingleFileGenerator
    {
        public const string GENERATOR_GUID = "85B769DE-38F5-4CBE-91AE-D0DFA431FE30";
        public const string GENERATOR_NAME = nameof(TextTemplatingFileGeneratorCore);
        public const string GENERATOR_DESCRIPTION = "Generate files from T4 templates using the .NET Core 3.1 runtime.";

        private const string ERROR_OUTPUT = "ErrorGeneratingOutput";
        private const string TEMPLATE_NAMESPACE = "RdJNL.TextTemplatingCore.GeneratedTemplate";
        private const string TEMPLATE_CLASS = "Template";

        private string _extension;
        private Encoding _encoding;

        public override string GetDefaultExtension()
        {
            return _extension ?? ".cs";
        }

        protected override string ProcessTemplate(string inputFileName, string inputFileContent, ITextTemplating processor, IVsHierarchy hierarchy)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            try
            {
                TextTemplatingCallback callback = new TextTemplatingCallback(this);

                processor.BeginErrorSession();
                string templateCode = processor.PreprocessTemplate(inputFileName, inputFileContent, callback, TEMPLATE_CLASS, TEMPLATE_NAMESPACE,
                    out string[] references);

                if( processor.EndErrorSession() || callback.ErrorLogged )
                {
                    return ERROR_OUTPUT;
                }

                DetectExtensionDirective(inputFileContent);
                _encoding = callback.OutputEncoding;

                references = ProcessReferences(references, inputFileName).ToArray();

                string output = TextTemplatingHelper.ExecuteTemplate(inputFileName, templateCode, references, out TemplateError[] errors);
                GenerateErrors(errors);

                if( output == null )
                {
                    return ERROR_OUTPUT;
                }
                else
                {
                    return output;
                }
            }
            catch( Exception e )
            {
                GenerateError(false, $"Something went wrong processing the template '{inputFileName}': {e.ToString()}");
                return ERROR_OUTPUT;
            }
        }

        private void DetectExtensionDirective(string inputFileContent)
        {
            Match m = Regex.Match(inputFileContent,
               @"<#@\s*output(?:\s+encoding=""[.a-z0-9- ]*"")?(?:\s+extension=""([.a-z0-9- ]*)"")?(?:\s+encoding=""[.a-z0-9- ]*"")?\s*#>",
               RegexOptions.IgnoreCase);

            if( m.Success && m.Groups[1].Success )
            {
                _extension = m.Groups[1].Value;

                if( _extension != "" && !_extension.StartsWith(".") )
                {
                    _extension = "." + _extension;
                }
            }
        }

        private IEnumerable<string> ProcessReferences(string[] references, string inputFileName)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            DTE dte = (DTE)GetService(typeof(DTE));
            string solutionFile = dte?.Solution.FileName;
            string solutionDir = solutionFile != null ? Path.GetDirectoryName(solutionFile) + "\\" : "";
            string projectFile = dte?.Solution.FindProjectItem(inputFileName)?.ContainingProject.FileName;
            string projectDir = projectFile != null ? Path.GetDirectoryName(projectFile) + "\\" : "";

            IEnumerable<string> refs = references
                .Take(references.Length - 2)
                .Select(r => r.Replace("$(SolutionDir)", solutionDir).Replace("$(ProjectDir)", projectDir));

            return TextTemplatingHelper.ProcessReferences(refs, inputFileName);
        }

        protected override byte[] GenerateCode(string inputFileName, string inputFileContent)
        {
            // Make sure the output file has the correct encoding

            byte[] resultBytes = base.GenerateCode(inputFileName, inputFileContent);

            if( _encoding == null )
            {
                return resultBytes;
            }

            Encoding inputEncoding;
            try
            {
                inputEncoding = DetectFileEncoding(inputFileName);
            }
            catch
            {
                return resultBytes;
            }

            if( _encoding == inputEncoding )
            {
                return resultBytes;
            }

            string result = inputEncoding.GetString(resultBytes);
            byte[] encodedBytes = _encoding.GetBytes(result);
            return encodedBytes;
        }

        private static Encoding DetectFileEncoding(string file)
        {
            using( var reader = new StreamReader(file, Encoding.ASCII, true) )
            {
                reader.ReadToEnd();
                return reader.CurrentEncoding;
            }
        }

        private void GenerateErrors(IEnumerable<TemplateError> errors)
        {
            foreach( TemplateError error in errors )
            {
                GenerateError(error);
            }
        }

        private void GenerateError(TemplateError error)
        {
            GenerateError(error.Warning, error.Message, error.Line, error.Column);
        }

        private void GenerateError(bool warning, string message, int line = 1, int column = 1)
        {
            GeneratorErrorCallback(warning, 0, message, line + 1, column + 1);
        }

        private sealed class TextTemplatingCallback : ITextTemplatingCallback
        {
            private readonly TextTemplatingFileGeneratorCore _generator;
            public bool ErrorLogged { get; private set; } = false;
            public string FileExtension { get; private set; }
            public Encoding OutputEncoding { get; private set; }

            public TextTemplatingCallback(TextTemplatingFileGeneratorCore generator)
            {
                _generator = generator;
            }

            public void ErrorCallback(bool warning, string message, int line, int column)
            {
                _generator.GeneratorErrorCallback(warning, 0, message, line, column);

                if( !warning )
                {
                    ErrorLogged = true;
                }
            }

            public void SetFileExtension(string extension)
            {
                FileExtension = extension;
            }

            public void SetOutputEncoding(Encoding encoding, bool fromOutputDirective)
            {
                OutputEncoding = encoding;
            }
        }
    }
}
