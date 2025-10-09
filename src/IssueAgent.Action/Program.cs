using System;
using System.Threading;
using IssueAgent.Agent.Runtime;

try
{
    using var cancellationSource = new CancellationTokenSource();

    Console.CancelKeyPress += (_, args) =>
    {
        args.Cancel = true;
        cancellationSource.Cancel();
    };

    var exitCode = await AgentBootstrap.RunAsync(cancellationSource.Token).ConfigureAwait(false);
    Environment.ExitCode = exitCode;
}
catch (Exception ex)
{
    Console.Error.WriteLine($"Fatal error: {ex.Message}");
    Environment.ExitCode = 1;
}
