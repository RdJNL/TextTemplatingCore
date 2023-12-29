//---------------------------------------------//
// Copyright 2023 RdJNL                        //
// https://github.com/RdJNL/TextTemplatingCore //
//---------------------------------------------//
using System;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.TextTemplating.VSHost;
using Task = System.Threading.Tasks.Task;

namespace RdJNL.TextTemplatingCore.TextTemplatingFileGeneratorCore
{
    [Guid(PACKAGE_GUID)]
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [InstalledProductRegistration(PACKAGE_NAME, PACKAGE_DESCRIPTION, PACKAGE_VERSION)]
    [ProvideCodeGenerator(typeof(TextTemplatingFileGeneratorCore), TextTemplatingFileGeneratorCore.GENERATOR_NAME, TextTemplatingFileGeneratorCore.GENERATOR_DESCRIPTION, true)]
    public sealed class VSPackage : AsyncPackage
    {
        public const string PACKAGE_GUID = "68C949A0-7E31-4336-82A1-DBAEFCD2AE62";
        public const string PACKAGE_NAME = "Text Templating File Generator .NET 8";
        public const string PACKAGE_DESCRIPTION = TextTemplatingFileGeneratorCore.GENERATOR_DESCRIPTION;
        public const string PACKAGE_VERSION = "1.3.0";

        protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
        }
    }
}
