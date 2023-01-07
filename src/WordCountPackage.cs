using System;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.VisualStudio.Shell;
using Task = System.Threading.Tasks.Task;

namespace WordCount
{
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [Guid(WordCountPackage.PackageGuidString)]
    public sealed class WordCountPackage : AsyncPackage
    {
        public const string PackageGuidString = "d4a6d946-7a92-4d7f-8961-608ae6137c3e";

        protected override Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            // When initialized asynchronously, the current thread may be a background thread at this point.
            // Do any initialization that requires the UI thread after switching to the UI thread.
            // await this.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            // Nothing to do.
            return Task.CompletedTask;
        }
    }
}
