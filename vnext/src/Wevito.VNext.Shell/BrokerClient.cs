using System.IO;
using System.IO.Pipes;
using System.Text;
using Wevito.VNext.Contracts;

namespace Wevito.VNext.Shell;

internal sealed class BrokerClient : IAsyncDisposable
{
    private readonly string _pipeName;
    private readonly CancellationTokenSource _cts = new();
    private readonly SemaphoreSlim _writeLock = new(1, 1);
    private NamedPipeClientStream? _stream;
    private StreamWriter? _writer;
    private Task? _readerTask;

    public BrokerClient(string pipeName)
    {
        _pipeName = pipeName;
    }

    public event Action<ShellEventEnvelope>? EventReceived;

    public async Task ConnectAsync(CancellationToken cancellationToken = default)
    {
        _stream = new NamedPipeClientStream(".", _pipeName, PipeDirection.InOut, PipeOptions.Asynchronous);
        await _stream.ConnectAsync(cancellationToken);
        _writer = new StreamWriter(_stream, new UTF8Encoding(false), leaveOpen: true)
        {
            AutoFlush = true,
            NewLine = "\n"
        };
        _readerTask = Task.Run(ReadLoopAsync, cancellationToken);
    }

    public async Task SendCommandAsync<TPayload>(string commandType, TPayload payload)
    {
        var writer = _writer ?? throw new InvalidOperationException("Broker pipe is not connected.");
        var line = PipeMessage.SerializeCommand(commandType, payload);
        await _writeLock.WaitAsync(_cts.Token);
        try
        {
            await writer.WriteLineAsync(line);
        }
        finally
        {
            _writeLock.Release();
        }
    }

    private async Task ReadLoopAsync()
    {
        if (_stream is null)
        {
            return;
        }

        using var reader = new StreamReader(_stream, Encoding.UTF8, leaveOpen: true);
        while (!_cts.IsCancellationRequested && _stream.IsConnected)
        {
            var line = await reader.ReadLineAsync(_cts.Token);
            if (string.IsNullOrWhiteSpace(line))
            {
                break;
            }

            EventReceived?.Invoke(PipeMessage.DeserializeEvent(line));
        }
    }

    public async ValueTask DisposeAsync()
    {
        await _cts.CancelAsync();
        if (_readerTask is not null)
        {
            try
            {
                await _readerTask;
            }
            catch
            {
                // Ignore pipe shutdown exceptions.
            }
        }

        _writeLock.Dispose();
        if (_stream is not null)
        {
            await _stream.DisposeAsync();
        }
        _cts.Dispose();
    }
}
