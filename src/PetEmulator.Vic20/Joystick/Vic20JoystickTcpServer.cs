using System.Net;
using System.Net.Sockets;

namespace PetEmulator.Vic20;

/// <summary>
/// Optional loopback TCP adapter for external joystick clients (for example a WSL helper).
/// The wire protocol is deliberately machine-independent: one command per line in the form
/// <c>UP 1</c>, <c>DOWN 0</c>, <c>LEFT 1</c>, <c>RIGHT 0</c>, <c>FIRE 1</c>, or <c>RESET</c>.
/// </summary>
public sealed class Vic20JoystickTcpServer : IAsyncDisposable
{
    private static readonly (string Name, Vic20JoystickInput Input)[] Inputs =
    [
        ("UP", Vic20JoystickInput.Up),
        ("DOWN", Vic20JoystickInput.Down),
        ("LEFT", Vic20JoystickInput.Left),
        ("RIGHT", Vic20JoystickInput.Right),
        ("FIRE", Vic20JoystickInput.Fire),
    ];

    private readonly IJoystickInputSink _inputSink;
    private readonly TcpListener _listener;
    private CancellationTokenSource? _cancellation;
    private Task? _acceptTask;

    public Vic20JoystickTcpServer(IJoystickInputSink inputSink, int port = 0)
    {
        ArgumentNullException.ThrowIfNull(inputSink);
        if (port is < 0 or > 65535)
            throw new ArgumentOutOfRangeException(nameof(port));

        _inputSink = inputSink;
        _listener = new TcpListener(IPAddress.Loopback, port);
    }

    public int Port { get; private set; }

    public void Start()
    {
        if (_cancellation is not null)
            throw new InvalidOperationException("The joystick TCP server is already started.");

        _cancellation = new CancellationTokenSource();
        _listener.Start();
        Port = ((IPEndPoint)_listener.LocalEndpoint).Port;
        _acceptTask = AcceptClientsAsync(_cancellation.Token);
    }

    public async ValueTask DisposeAsync()
    {
        _cancellation?.Cancel();
        _listener.Stop();

        if (_acceptTask is not null)
        {
            try
            {
                await _acceptTask.ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
            }
            catch (SocketException) when (_cancellation?.IsCancellationRequested == true)
            {
            }
        }

        _cancellation?.Dispose();
        _cancellation = null;
        _acceptTask = null;
        ResetInputs();
    }

    private async Task AcceptClientsAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                using var client = await _listener.AcceptTcpClientAsync(cancellationToken).ConfigureAwait(false);
                await HandleClientAsync(client, cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                return;
            }
            catch (SocketException) when (cancellationToken.IsCancellationRequested)
            {
                return;
            }
        }
    }

    private async Task HandleClientAsync(TcpClient client, CancellationToken cancellationToken)
    {
        client.NoDelay = true;
        try
        {
            using var reader = new StreamReader(client.GetStream());
            while (!cancellationToken.IsCancellationRequested)
            {
                var line = await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false);
                if (line is null)
                    break;

                Apply(line);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (IOException)
        {
        }
        finally
        {
            ResetInputs();
        }
    }

    private void Apply(string command)
    {
        var parts = command.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length == 1 && parts[0].Equals("RESET", StringComparison.OrdinalIgnoreCase))
        {
            ResetInputs();
            return;
        }

        if (parts.Length != 2 || (parts[1] != "0" && parts[1] != "1"))
            return;

        var input = Inputs.FirstOrDefault(item => item.Name.Equals(parts[0], StringComparison.OrdinalIgnoreCase));
        if (input.Name is not null)
            _inputSink.Set(input.Input, parts[1] == "1");
    }

    private void ResetInputs()
    {
        foreach (var (_, input) in Inputs)
            _inputSink.Set(input, false);
    }
}
