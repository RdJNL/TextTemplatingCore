//---------------------------------------------//
// Copyright 2020 RdJNL                        //
// https://github.com/RdJNL/TextTemplatingCore //
//---------------------------------------------//
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace RdJNL.TextTemplatingCore.TextTemplatingCoreLib
{
    public static class TextTemplatingHelper
    {
        public static IEnumerable<string> ProcessReferences(IEnumerable<string> references, string inputFileName)
        {
            string[] refs = references.ToArray();

            return refs
                .Where(r => r != "System")
                .Select(r =>
                {
                    if( r.EndsWith(".dll") )
                    {
                        r = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(inputFileName), r));
                    }

                    return r;
                });
        }

        public static string ExecuteTemplate(string inputFileName, string templateCode, string[] references, out TemplateError[] errors)
        {
            string coreInputFile = null;
            string coreOutputFile = null;

            try
            {
                coreInputFile = Path.GetTempFileName();
                File.WriteAllText(coreInputFile, templateCode, Encoding.UTF8);
                coreOutputFile = Path.GetTempFileName();

                bool executeSuccess = RunExecute(inputFileName, coreInputFile, coreOutputFile, references, out errors);

                if( !executeSuccess )
                {
                    return null;
                }
                else
                {
                    return File.ReadAllText(coreOutputFile, Encoding.UTF8);
                }
            }
            finally
            {
                try
                {
                    if( coreInputFile != null && File.Exists(coreInputFile) )
                    {
                        File.Delete(coreInputFile);
                    }
                }
                catch { }

                try
                {
                    if( coreOutputFile != null && File.Exists(coreOutputFile) )
                    {
                        File.Delete(coreOutputFile);
                    }
                }
                catch { }
            }
        }

        private static bool RunExecute(string inputFileName, string coreInputFile, string coreOutputFile, string[] references, out TemplateError[] errors)
        {
            string executePath = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location),
                @"TemplateExecute\TemplateExecute.exe"));

            ProcessStartInfo info = new ProcessStartInfo
            {
                FileName = executePath,
                Arguments = EscapeArguments(new[] { inputFileName, coreInputFile, coreOutputFile }.Concat(references)),
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardError = true,
                WindowStyle = ProcessWindowStyle.Hidden,
            };

            var p = Process.Start(info);
            p.WaitForExit(60000);

            if( !p.HasExited )
            {
                p.Kill();
                throw new TimeoutException("The TemplateExecute process did not respond within 60 seconds. Aborting operation.");
            }

            if( p.ExitCode == 0 )
            {
                errors = ProcessTemplateErrors(p).ToArray();
                return true;
            }
            else if( p.ExitCode == 1 )
            {
                errors = ProcessTemplateErrors(p).ToArray();
                return false;
            }
            else
            {
                string error = p.StandardError.ReadToEnd();
                errors = new[] { new TemplateError(false, $"Something went wrong executing the template in .NET Core: {error}") };
                return false;
            }
        }

        private static IEnumerable<TemplateError> ProcessTemplateErrors(Process process)
        {
            var stdError = process.StandardError;

            while( !stdError.EndOfStream )
            {
                bool warning = stdError.ReadLine() == "1";
                int line = int.Parse(stdError.ReadLine());
                int column = int.Parse(stdError.ReadLine());
                int messageLength = int.Parse(stdError.ReadLine());
                char[] messageBuffer = new char[messageLength];
                int readLength = stdError.ReadBlock(messageBuffer, 0, messageLength);
                string message = new string(messageBuffer, 0, readLength);
                stdError.ReadLine();

                yield return new TemplateError(warning, message, line, column);
            }
        }

        public static string EscapeArguments(IEnumerable<string> args)
        {
            StringBuilder arguments = new StringBuilder();

            foreach( string arg in args )
            {
                if( arguments.Length > 0 )
                {
                    arguments.Append(" ");
                }

                arguments.Append($"\"{arg.Replace("\\", "\\\\").Replace("\"", "\\\"")}\"");
            }

            return arguments.ToString();
        }

        public static string UnescapeArg(string arg)
        {
            return arg.Replace("\\\\", "\\");
        }

        public static string[] UnescapeArgs(string[] args)
        {
            return args.Select(UnescapeArg).ToArray();
        }
    }
}
