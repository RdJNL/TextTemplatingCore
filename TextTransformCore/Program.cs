//---------------------------------------------//
// Copyright 2023 RdJNL                        //
// https://github.com/RdJNL/TextTemplatingCore //
//---------------------------------------------//
using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TextTemplating;
using RdJNL.TextTemplatingCore.TextTemplatingCoreLib;

namespace RdJNL.TextTemplatingCore.TextTransformCore
{
    internal static class Program
    {
        private const string ERROR_OUTPUT = "ErrorGeneratingOutput";
        private const string TEMPLATE_NAMESPACE = "RdJNL.TextTemplatingCore.GeneratedTemplate";
        private const string TEMPLATE_CLASS = "Template";

        private static int Main(string[] args)
        {
            string outputFileName = null;
            Encoding encoding = Encoding.UTF8;

            try
            {
                if( args.Length != 1 )
                {
                    throw new ArgumentException($"Need 1 argument, found {args.Length}.", nameof(args));
                }

                string inputFileName = Path.GetFullPath(TextTemplatingHelper.UnescapeArg(args[0]));
                Directory.SetCurrentDirectory(Path.GetDirectoryName(inputFileName));

                string inputText;
                using( var reader = new StreamReader(inputFileName, Encoding.ASCII, true) )
                {
                    inputText = reader.ReadToEnd();
                    encoding = reader.CurrentEncoding;
                }

                Engine engine = new Engine();
                TTHost host = new TTHost(inputFileName);

                string templateCode = engine.PreprocessTemplate(inputText, host, TEMPLATE_CLASS, TEMPLATE_NAMESPACE, out _, out string[] references);

                ThrowOrWriteErrors(host.HasErrors || templateCode == ERROR_OUTPUT, host.Errors);

                outputFileName = Path.Combine(Path.GetDirectoryName(inputFileName), Path.GetFileNameWithoutExtension(inputFileName) + (host.FileExtension ?? ".cs"));
                encoding = host.OutputEncoding ?? encoding;

                if( outputFileName == inputFileName )
                {
                    throw new ExtensionException(
                        "Cannot overwrite input file. This error probably means the output extension is equal to the template file's extension.");
                }

                references = TextTemplatingHelper.ProcessReferences(references, inputFileName).ToArray();

                string output = TextTemplatingHelper.ExecuteTemplate(inputFileName, templateCode, references, out TemplateError[] errors);
                ThrowOrWriteErrors(output == null, errors);

                File.WriteAllText(outputFileName, output, encoding);
                return 0;
            }
            catch( TemplateException ex )
            {
                WriteExceptionHeader();
                WriteErrors(ex.Errors);
                OutputException(false, FormatErrors(ex.Errors), outputFileName, encoding);
                return 1;
            }
            catch( ExtensionException ex )
            {
                OutputException(true, ex.ToString() + Environment.NewLine + Environment.NewLine, null, encoding);
                return 2;
            }
            catch( Exception ex )
            {
                OutputException(true, ex.ToString() + Environment.NewLine + Environment.NewLine, outputFileName, encoding);
                return 2;
            }
        }

        private static void ThrowOrWriteErrors(bool throwErrors, IEnumerable<TemplateError> errors)
        {
            if( throwErrors )
            {
                throw new TemplateException(errors);
            }
            else
            {
                WriteErrors(errors);
            }
        }

        private static void WriteExceptionHeader()
        {
            ConsoleColor color = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Red;

            Console.Error.WriteLine(ERROR_OUTPUT);
            Console.Error.WriteLine();

            Console.ForegroundColor = color;
        }

        private static void WriteErrors(IEnumerable<TemplateError> errors)
        {
            foreach( TemplateError error in errors )
            {
                WriteError(error);
            }
        }

        private static void WriteError(TemplateError error)
        {
            ConsoleColor color = Console.ForegroundColor;
            Console.ForegroundColor = error.Warning ? ConsoleColor.Yellow : ConsoleColor.Red;

            Console.Error.Write(FormatError(error));

            Console.ForegroundColor = color;
        }

        private static string FormatErrors(IEnumerable<TemplateError> errors)
        {
            return string.Concat(errors.Select(e => FormatError(e)));
        }

        private static string FormatError(TemplateError error)
        {
            return $"{(error.Warning ? "Warning" : "Error")} on line {error.Line}, column {error.Column}:" +
                $"{Environment.NewLine}{error.Message}{Environment.NewLine}{Environment.NewLine}";
        }

        private static void OutputException(bool writeToConsole, string exceptionText, string outputFileName, Encoding encoding)
        {
            exceptionText = ERROR_OUTPUT + Environment.NewLine + Environment.NewLine + exceptionText;

            if( writeToConsole )
            {
                ConsoleColor color = Console.ForegroundColor;
                Console.ForegroundColor = ConsoleColor.Red;

                Console.Error.Write(exceptionText);

                Console.ForegroundColor = color;
            }

            if( outputFileName != null )
            {
                File.WriteAllText(outputFileName, exceptionText, encoding);
            }
        }

        private sealed class TTHost : ITextTemplatingEngineHost
        {
            public string FileExtension { get; private set; }
            public Encoding OutputEncoding { get; private set; }
            public bool HasErrors { get; private set; }
            public ImmutableArray<TemplateError> Errors { get; private set; }

            public string TemplateFile { get; }
            public IList<string> StandardImports => new[] { "System" };
            public IList<string> StandardAssemblyReferences => new string[0];

            public TTHost(string templateFile)
            {
                TemplateFile = templateFile;
            }

            public object GetHostOption(string optionName)
            {
                return null;
            }

            public bool LoadIncludeText(string requestFileName, out string content, out string location)
            {
                if( !Path.IsPathRooted(requestFileName) )
                {
                    content = "";
                    location = "";
                    return false;
                }

                string file = Path.GetFullPath(requestFileName);

                if( File.Exists(file) )
                {
                    content = File.ReadAllText(file);
                    location = file;
                    return true;
                }
                else
                {
                    content = "";
                    location = "";
                    return false;
                }
            }

            public void LogErrors(CompilerErrorCollection errors)
            {
                HasErrors = errors.HasErrors;

                Errors = errors
                    .Cast<CompilerError>()
                    .Select(e => new TemplateError(e.IsWarning, $"Compile {(e.IsWarning ? "warning" : "error")} in {e.FileName}({e.Line},{e.Column}): {e.ErrorText}",
                        e.Line, e.Column))
                    .ToImmutableArray();
            }

            public AppDomain ProvideTemplatingAppDomain(string content)
            {
                throw new NotSupportedException();
            }

            public string ResolveAssemblyReference(string assemblyReference)
            {
                throw new NotSupportedException();
            }

            public Type ResolveDirectiveProcessor(string processorName)
            {
                throw new InvalidOperationException($"Directive processor '{processorName}' not found.");
            }

            public string ResolveParameterValue(string directiveId, string processorName, string parameterName)
            {
                throw new NotSupportedException();
            }

            public string ResolvePath(string path)
            {
                throw new NotSupportedException();
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
