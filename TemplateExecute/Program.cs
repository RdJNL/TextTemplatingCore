//---------------------------------------------//
// Copyright 2023 RdJNL                        //
// https://github.com/RdJNL/TextTemplatingCore //
//---------------------------------------------//
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using RdJNL.TextTemplatingCore.TextTemplatingCoreLib;

namespace RdJNL.TemplateExecute
{
    internal static class Program
    {
        private const string TEMPLATE_NAMESPACE = "RdJNL.TextTemplatingCore.GeneratedTemplate";
        private const string TEMPLATE_CLASS = "Template";

        private static int Main(string[] args)
        {
            try
            {
                if( args.Length < 3 )
                {
                    throw new ArgumentException($"Need at least 3 arguments, found only {args.Length}.", nameof(args));
                }

                string templateFile = UnescapeArg(args[0]);
                string inputFile = UnescapeArg(args[1]);
                string outputFile = UnescapeArg(args[2]);
                string[] libraries = args.Length > 3 ? UnescapeArgs(args[3..]) : new string[0];

                Directory.SetCurrentDirectory(Path.GetDirectoryName(templateFile)!);

                string inputCode = File.ReadAllText(inputFile, Encoding.UTF8);
                SourceText sourceText = SourceText.From(inputCode);
                CSharpParseOptions options = CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp8);
                SyntaxTree syntaxTree = SyntaxFactory.ParseSyntaxTree(sourceText, options);

                string partialCode = $"namespace {TEMPLATE_NAMESPACE} {{ public partial class {TEMPLATE_CLASS} {{ " +
                    $"public string TemplateFile {{ get; }} = @\"{templateFile.Replace("\"", "\"\"")}\";" +
                    " } }";
                SourceText partialText = SourceText.From(partialCode);
                SyntaxTree partialTree = SyntaxFactory.ParseSyntaxTree(partialText, options);

                // This array is here so the assemblies containing these types are automatically loaded into the AppDomain
                _ = new[] {
                    typeof(System.CodeDom.Compiler.GeneratedCodeAttribute),
                    typeof(System.CodeDom.Compiler.CompilerError),
                    typeof(System.Collections.CollectionBase),
                };

                List<MetadataReference> references = new List<MetadataReference>();

                foreach( Assembly a in AppDomain.CurrentDomain.GetAssemblies() )
                {
                    references.Add(MetadataReference.CreateFromFile(a.Location));
                }

                LibraryLoadContext context = new LibraryLoadContext();

                foreach( string library in libraries )
                {
                    Assembly assembly = context.LoadLibrary(library);
                    references.Add(MetadataReference.CreateFromFile(assembly.Location));
                }

                CSharpCompilation compilation = CSharpCompilation.Create("GeneratedTemplate.dll", new[] { syntaxTree, partialTree }, references,
                    new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

                using( var stream = new MemoryStream() )
                {
                    var result = compilation.Emit(stream);
                    IEnumerable<TemplateError> compileErrors = ProcessErrors(result.Diagnostics);

                    if( !result.Success )
                    {
                        throw new TemplateException(compileErrors);
                    }

                    stream.Seek(0, SeekOrigin.Begin);

                    Assembly assembly = context.LoadFromStream(stream);

                    Type? templateType = assembly.GetType($"{TEMPLATE_NAMESPACE}.{TEMPLATE_CLASS}");

                    if( templateType == null )
                    {
                        throw new Exception("Failed to find template class.");
                    }

                    dynamic? template = Activator.CreateInstance(templateType);

                    if( template == null )
                    {
                        throw new Exception("Failed to create instance of template class.");
                    }

                    string output = template.TransformText();

                    File.WriteAllText(outputFile, output, Encoding.UTF8);

                    WriteTemplateErrors(compileErrors);
                    return 0;
                }
            }
            catch( TemplateException ex )
            {
                WriteTemplateErrors(ex.Errors);
                return 1;
            }
            catch( Exception ex )
            {
                Console.Error.Write(ex.ToString());
                return 2;
            }
        }

        private static string UnescapeArg(string arg)
        {
            return arg.Replace("\\\\", "\\");
        }

        private static string[] UnescapeArgs(string[] args)
        {
            return args.Select(UnescapeArg).ToArray();
        }

        private static IEnumerable<TemplateError> ProcessErrors(IEnumerable<Diagnostic> diagnostics)
        {
            return diagnostics
                .Where(d => d.Severity == DiagnosticSeverity.Error || d.Severity == DiagnosticSeverity.Warning)
                .Select(d => new TemplateError(
                    d.Severity == DiagnosticSeverity.Warning,
                    d.ToString(),
                    d.Location.GetMappedLineSpan().Span.Start.Line + 1,
                    d.Location.GetMappedLineSpan().Span.Start.Character + 1));
        }

        private static void WriteTemplateErrors(IEnumerable<TemplateError> errors)
        {
            foreach( TemplateError error in errors )
            {
                Console.Error.WriteLine(error.Warning ? 1 : 0);
                Console.Error.WriteLine(error.Line);
                Console.Error.WriteLine(error.Column);
                Console.Error.WriteLine(error.Message.Length);
                Console.Error.WriteLine(error.Message);
            }
        }
    }
}
