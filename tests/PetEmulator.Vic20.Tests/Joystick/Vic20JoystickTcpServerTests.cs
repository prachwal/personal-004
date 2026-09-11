using System.Net.Sockets;
using System.Text;
using FluentAssertions;
using NUnit.Framework;

namespace PetEmulator.Vic20.Tests;

public sealed class Vic20JoystickTcpServerTests
{
    [Test]
    [CancelAfter(5000)]
    public async Task AppliesCommandsAndResetsInputsWhenClientDisconnects()
    {
        var joystick = new Vic20Joystick();
        await using var server = new Vic20JoystickTcpServer(joystick.Set);
        server.Start();

        using (var client = new TcpClient())
        {
            await client.ConnectAsync("127.0.0.1", server.Port);
            await SendAsync(client, "UP 1\nRIGHT 1\nFIRE 1\n");
            await WaitUntilAsync(() => joystick.Up && joystick.Right && joystick.Fire);
        }

        await WaitUntilAsync(() => !joystick.Up && !joystick.Right && !joystick.Fire);
    }

    [Test]
    [CancelAfter(5000)]
    public async Task IgnoresMalformedCommandsAndSupportsReset()
    {
        var joystick = new Vic20Joystick();
        await using var server = new Vic20JoystickTcpServer(joystick.Set);
        server.Start();

        using var client = new TcpClient();
        await client.ConnectAsync("127.0.0.1", server.Port);
        await SendAsync(client, "UP yes\nUNKNOWN 1\nUP 1\nRESET\n");
        await WaitUntilAsync(() => !joystick.Up);

        joystick.Up.Should().BeFalse();
        joystick.Down.Should().BeFalse();
    }

    private static async Task SendAsync(TcpClient client, string text)
    {
        var bytes = Encoding.ASCII.GetBytes(text);
        await client.GetStream().WriteAsync(bytes);
    }

    private static async Task WaitUntilAsync(Func<bool> condition)
    {
        for (var attempt = 0; attempt < 100 && !condition(); attempt++)
            await Task.Delay(10);

        condition().Should().BeTrue();
    }
}
