using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Windows.Threading;
using Wevito.VNext.Contracts;

namespace Wevito.VNext.Shell;

internal sealed class DevControlServer : IAsyncDisposable
{
    public const string DefaultPipeName = "wevito-vnext-dev-control";
    public const string PipeNameEnvironmentVariable = "WEVITO_DEV_CONTROL_PIPE";

    private readonly string _pipeName;
    private readonly Dispatcher _dispatcher;
    private readonly Func<DevControlCommandEnvelope, Task<DevControlResponseEnvelope>> _handleCommandAsync;
    private readonly CancellationTokenSource _shutdown = new();
    private Task? _loopTask;

    public DevControlServer(
        string pipeName,
        Dispatcher dispatcher,
        Func<DevControlCommandEnvelope, Task<DevControlResponseEnvelope>> handleCommandAsync)
    {
        _pipeName = pipeName;
        _dispatcher = dispatcher;
        _handleCommandAsync = handleCommandAsync;
    }

    public void Start()
    {
        _loopTask ??= Task.Run(RunAsync);
    }

    public static string ResolvePipeName()
    {
        var configured = Environment.GetEnvironmentVariable(PipeNameEnvironmentVariable);
        return string.IsNullOrWhiteSpace(configured) ? DefaultPipeName : configured.Trim();
    }

    private async Task RunAsync()
    {
        while (!_shutdown.IsCancellationRequested)
        {
            try
            {
                await using var pipe = new NamedPipeServerStream(
                    _pipeName,
                    PipeDirection.InOut,
                    1,
                    PipeTransmissionMode.Byte,
                    PipeOptions.Asynchronous);

                await pipe.WaitForConnectionAsync(_shutdown.Token);
                using var reader = new StreamReader(pipe, Encoding.UTF8, false, leaveOpen: true);
                await using var writer = new StreamWriter(pipe, new UTF8Encoding(false), leaveOpen: true)
                {
                    AutoFlush = true
                };

                var line = await reader.ReadLineAsync(_shutdown.Token);
                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                var response = await DispatchAsync(line);
                await writer.WriteLineAsync(DevControlPipeMessage.SerializeResponse(response));
            }
            catch (OperationCanceledException) when (_shutdown.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                TraceLog.Write("dev-control", $"server-error {ex.GetType().Name}: {ex.Message}");
            }
        }
    }

    private async Task<DevControlResponseEnvelope> DispatchAsync(string line)
    {
        try
        {
            var envelope = DevControlPipeMessage.DeserializeCommand(line);
            return await _dispatcher.InvokeAsync(() => _handleCommandAsync(envelope)).Task.Unwrap();
        }
        catch (Exception ex)
        {
            TraceLog.Write("dev-control", $"command-error {ex.GetType().Name}: {ex.Message}");
            return new DevControlResponseEnvelope(
                false,
                $"Dev-control command failed: {ex.Message}",
                DevControlSnapshot.Empty(DateTimeOffset.UtcNow));
        }
    }

    public async ValueTask DisposeAsync()
    {
        _shutdown.Cancel();
        if (_loopTask is not null)
        {
            try
            {
                await _loopTask.WaitAsync(TimeSpan.FromSeconds(1));
            }
            catch (TimeoutException)
            {
            }
            catch (OperationCanceledException)
            {
            }
        }

        _shutdown.Dispose();
    }
}
