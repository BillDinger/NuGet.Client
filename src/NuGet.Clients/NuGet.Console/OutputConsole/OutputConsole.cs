// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Threading;
using NuGet.VisualStudio;
using Task = System.Threading.Tasks.Task;

namespace NuGetConsole
{
    /// <summary>
    /// This class implements the IConsole interface in order to integrate with the PowerShellHost.
    /// It sends PowerShell host outputs to the VS Output tool window.
    /// </summary>
    internal sealed class OutputConsole : BaseOutputConsole, IConsole
    {
        private readonly IVsOutputWindow _vsOutputWindow;
        private readonly IVsUIShell _vsUiShell;
        private readonly AsyncLazy<IVsOutputWindowPane> _outputWindowPane;
        private readonly AsyncLazy<OutputWindowTextWriter> _outputWindowTextWriter;

        private IVsOutputWindowPane VsOutputWindowPane => NuGetUIThreadHelper.JoinableTaskFactory.Run(_outputWindowPane.GetValueAsync);

        public OutputConsole(
            IVsOutputWindow vsOutputWindow,
            IVsUIShell vsUiShell)
        {
            if (vsOutputWindow == null)
            {
                throw new ArgumentNullException(nameof(vsOutputWindow));
            }

            if (vsUiShell == null)
            {
                throw new ArgumentNullException(nameof(vsUiShell));
            }

            _vsOutputWindow = vsOutputWindow;
            _vsUiShell = vsUiShell;

            _outputWindowPane = new AsyncLazy<IVsOutputWindowPane>(async () =>
            {
                await NuGetUIThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

                // create the Package Manager pane within the Output window
                var hr = _vsOutputWindow.CreatePane(
                    ref GuidList.guidNuGetOutputWindowPaneGuid,
                    Resources.OutputConsolePaneName,
                    fInitVisible: 1,
                    fClearWithSolution: 0);
                ErrorHandler.ThrowOnFailure(hr);

                IVsOutputWindowPane pane;
                hr = _vsOutputWindow.GetPane(
                    ref GuidList.guidNuGetOutputWindowPaneGuid,
                    out pane);
                ErrorHandler.ThrowOnFailure(hr);

                return pane;

            }, NuGetUIThreadHelper.JoinableTaskFactory);

            _outputWindowTextWriter = new AsyncLazy<OutputWindowTextWriter>(async () =>
            {
                var outputWindowPane = await _outputWindowPane.GetValueAsync();
                await NuGetUIThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                return new OutputWindowTextWriter(outputWindowPane);
            },
            NuGetUIThreadHelper.JoinableTaskFactory);
        }

        public override async Task WriteAsync(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return;
            }

            Start();

            var outputWindowTextWriter = await _outputWindowTextWriter.GetValueAsync();
            await outputWindowTextWriter.WriteAsync(text);
        }

        public override async Task ActivateAsync()
        {
            var outputWindowTextWriter = await _outputWindowTextWriter.GetValueAsync();
            await outputWindowTextWriter.FlushAsync();

            await NuGetUIThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            _vsUiShell.FindToolWindow(0,
                ref GuidList.guidVsWindowKindOutput,
                out var toolWindow);
            toolWindow?.Show();

            VsOutputWindowPane.Activate();
        }

        public override async Task ClearAsync()
        {
            Start();

            await NuGetUIThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            VsOutputWindowPane.Activate();
            VsOutputWindowPane.Clear();
        }

        public override void StartConsoleDispatcher()
        {
            _ = VsOutputWindowPane;
        }
    }
}
