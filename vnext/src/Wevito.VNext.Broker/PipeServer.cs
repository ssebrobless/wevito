using System.IO.Pipes;
using System.Text;
using Wevito.VNext.Contracts;

namespace Wevito.VNext.Broker;

internal sealed class PipeServer : IAsyncDisposable
{
    private readonly string _pipeName;
    private readonly CancellationTokenSource _cts = new();
    private readonly SemaphoreSlim _writeLock = new(1, 1);
    private NamedPipeServerStream? _stream;
    private StreamWriter? _writer;

    public PipeServer(string pipeName)
    {
        _pipeName = pipeName;
    }

    public event Func<ShellCommandEnvelope, Task>? CommandReceived;

    public event Action? ClientDisconnected;

    public async Task RunAsync()
    {
        while (!_cts.IsCancellationRequested)
        {
            _stream = new NamedPipeServerStream(
                _pipeName,
                PipeDirection.InOut,
                1,
                PipeTransmissionMode.Byte,
                PipeOptions.Asynchronous);

            try
            {
                await _stream.WaitForConnectionAsync(_cts.Token);
                using var reader = new StreamReader(_stream, Encoding.UTF8, leaveOpen: true);
                _writer = new StreamWriter(_stream, new UTF8Encoding(false), leaveOpen: true)
                {
                    AutoFlush = true,
                    NewLine = "\n"
                };

                while (!_cts.IsCancellationRequested && _stream.IsConnected)
                {
                    var line = await reader.ReadLineAsync(_cts.Token);
                    if (string.IsNullOrWhiteSpace(line))
                    {
                        break;
                    }

                    var envelope = PipeMessage.DeserializeCommand(line);
                    if (CommandReceived is not null)
                    {
                        await CommandReceived.Invoke(envelope);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch
            {
                ClientDisconnected?.Invoke();
                break;
            }
            finally
            {
                _writer = null;
                if (_stream is not null)
                {
                    await _stream.DisposeAsync();
                    _stream = null;
                }
            }

            ClientDisconnected?.Invoke();
            break;
        }
    }

    public async Task SendEventAsync<TPayload>(string eventType, TPayload payload)
    {
        var writer = _writer;
        if (writer is null)
        {
            return;
        }

        var line = PipeMessage.SerializeEvent(eventType, payload);
        await _writeLock.WaitAsync(_cts.Token);
        try
        {
            await writer.WriteLineAsync(line);
        }
        catch
        {
            ClientDisconnected?.Invoke();
        }
        finally
        {
            _writeLock.Release();
        }
    }

    public async ValueTask DisposeAsync()
    {
        await _cts.CancelAsync();
        _writeLock.Dispose();
        if (_stream is not null)
        {
            await _stream.DisposeAsync();
        }
        _cts.Dispose();
    }
}
