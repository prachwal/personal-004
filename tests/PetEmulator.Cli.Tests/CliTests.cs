using NUnit.Framework;

namespace PetEmulator.Cli.Tests;

[TestFixture]
[NonParallelizable]
public sealed class CliTests
{
    [Test]
    public async Task Apps_lists_predefined_applications()
    {
        (int exitCode, string output, string error) result = await RunAsync("apps", "--log-level", "Warning");

        Assert.That(result.exitCode, Is.EqualTo(0));
        Assert.That(result.output, Does.Contain("status"));
        Assert.That(result.output, Does.Contain("config"));
        Assert.That(result.error, Is.Empty);
    }

    [Test]
    public async Task Status_accepts_overrides_after_command()
    {
        (int exitCode, string output, string error) result = await RunAsync(
            "run", "status", "--profile", "pet-4032", "--steps", "25", "--log-level", "Warning");

        Assert.That(result.exitCode, Is.EqualTo(0));
        Assert.That(result.output, Does.Contain("profile: pet-4032"));
        Assert.That(result.output, Does.Contain("steps: 25"));
        Assert.That(result.error, Is.Empty);
    }

    [Test]
    public async Task Status_reads_custom_json_configuration()
    {
        string configPath = Path.GetTempFileName();
        try
        {
            await File.WriteAllTextAsync(configPath, """
                {
                  "PetEmulator": {
                    "Profile": "pet-8032",
                    "RomsPath": "test-roms",
                    "Steps": 42
                  }
                }
                """);

            (int exitCode, string output, string error) result = await RunAsync(
                "run", "status", "--config", configPath, "--log-level", "Warning");

            Assert.That(result.exitCode, Is.EqualTo(0));
            Assert.That(result.output, Does.Contain("profile: pet-8032"));
            Assert.That(result.output, Does.Contain("test-roms"));
            Assert.That(result.output, Does.Contain("steps: 42"));
            Assert.That(result.error, Is.Empty);
        }
        finally
        {
            File.Delete(configPath);
        }
    }

    [Test]
    public async Task Unknown_application_returns_input_error()
    {
        (int exitCode, string output, string error) result = await RunAsync(
            "run", "missing", "--log-level", "Warning");

        Assert.That(result.exitCode, Is.EqualTo(2));
        Assert.That(result.output, Is.Empty);
        Assert.That(result.error, Does.Contain("Unknown application 'missing'"));
    }

    private static async Task<(int exitCode, string output, string error)> RunAsync(params string[] args)
    {
        using var output = new StringWriter();
        using var error = new StringWriter();
        TextWriter originalOutput = Console.Out;
        TextWriter originalError = Console.Error;
        Console.SetOut(output);
        Console.SetError(error);
        try
        {
            int exitCode = await global::PetEmulator.Cli.Program.Main(args);
            return (exitCode, output.ToString(), error.ToString());
        }
        finally
        {
            Console.SetOut(originalOutput);
            Console.SetError(originalError);
        }
    }
}
