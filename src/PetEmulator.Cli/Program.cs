using System.CommandLine;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace PetEmulator.Cli;

internal static class Program
{
    public static async Task<int> Main(string[] args)
    {
        RootCommand root = CliCommandFactory.Create();
        return await root.Parse(args).InvokeAsync();
    }
}

internal static class CliCommandFactory
{
    public static RootCommand Create()
    {
        var root = new RootCommand("PET emulator command-line host.");
        CliOptionSet rootOptions = AddOptions(root);

        var apps = new Command("apps", "List predefined applications.");
        CliOptionSet appsOptions = AddOptions(apps);
        apps.SetAction(async (parseResult, cancellationToken) =>
            await ExecuteAsync(parseResult, appsOptions, rootOptions,
                static (registry, request, token) => registry.ListAsync(request, token), cancellationToken));

        var application = new Argument<string>("application")
        {
            Description = "Predefined application name."
        };
        var run = new Command("run", "Run a predefined application.");
        CliOptionSet runOptions = AddOptions(run);
        run.Arguments.Add(application);
        run.SetAction(async (parseResult, cancellationToken) =>
            await ExecuteAsync(parseResult, runOptions, rootOptions,
                (registry, request, token) => registry.RunAsync(
                    parseResult.GetValue(application)!, request, token), cancellationToken));

        var script = new Argument<string?>("script")
        {
            Description = "Path to a debugger script (one command per line, '#' starts a comment) - omit to read stdin.",
            Arity = ArgumentArity.ZeroOrOne
        };
        var debug = new Command("debug",
            "Run a scripted PetDebuggerSession: profile/roms/tape/disk/key/type/devices/status, " +
            "plus superpet-diagnose/superpet-boot-checkpoints/via-irq-check, " +
            "trace/watch/watch-range/unwatch/break-cycle/break-instruction-count/dump.");
        debug.Arguments.Add(script);
        debug.SetAction((parseResult, _) => Task.FromResult(RunDebugScript(parseResult.GetValue(script))));

        var vic20Script = new Argument<string?>("script")
        {
            Description = "Path to a Vic20DebuggerSession script (one command per line, '#' starts a comment) - omit to read stdin.",
            Arity = ArgumentArity.ZeroOrOne
        };
        var vic20Debug = new Command("vic20-debug",
            "Run a scripted Vic20DebuggerSession: roms/cartridge/cartridge-plugin/disk/key/type/status, plus trace/watch/" +
            "watch-range/unwatch/break-cycle/break-instruction-count/break-pc/dump.");
        vic20Debug.Arguments.Add(vic20Script);
        vic20Debug.SetAction((parseResult, _) => Task.FromResult(RunVic20DebugScript(parseResult.GetValue(vic20Script))));

        root.Subcommands.Add(apps);
        root.Subcommands.Add(run);
        root.Subcommands.Add(debug);
        root.Subcommands.Add(vic20Debug);
        root.SetAction(async (parseResult, cancellationToken) =>
            await ExecuteAsync(parseResult, rootOptions, null,
                static (registry, request, token) => registry.RunAsync(
                    request.Settings.DefaultApplication, request, token), cancellationToken));
        return root;
    }

    /// <summary>Runs a <see cref="PetDebuggerSession"/> script line by line, printing whatever
    /// each command returns. Mirrors <see cref="MachineDebugger"/>/<see cref="PetDebuggerSession"/>'s
    /// own "never throw, report as an 'error: ...' string" convention - one bad line doesn't stop
    /// the rest of the script; the exit code just reflects whether any line failed.</summary>
    private static int RunDebugScript(string? scriptPath)
    {
        using var reader = scriptPath is not null
            ? new StreamReader(scriptPath)
            : new StreamReader(Console.OpenStandardInput());

        var session = new PetDebuggerSession();
        var hadError = false;
        string? line;
        while ((line = reader.ReadLine()) is not null)
        {
            var trimmed = line.Trim();
            if (trimmed.Length == 0 || trimmed.StartsWith('#'))
                continue;

            var output = session.Execute(trimmed);
            if (output.Length > 0)
                Console.Write(output.EndsWith('\n') ? output : output + Environment.NewLine);
            if (output.StartsWith("error:", StringComparison.Ordinal))
                hadError = true;
        }

        return hadError ? 1 : 0;
    }

    /// <summary>Mirrors <see cref="RunDebugScript"/> exactly, for a <see cref="Vic20DebuggerSession"/>
    /// instead of a <see cref="PetDebuggerSession"/>.</summary>
    private static int RunVic20DebugScript(string? scriptPath)
    {
        using var reader = scriptPath is not null
            ? new StreamReader(scriptPath)
            : new StreamReader(Console.OpenStandardInput());

        var session = new Vic20DebuggerSession();
        var hadError = false;
        string? line;
        while ((line = reader.ReadLine()) is not null)
        {
            var trimmed = line.Trim();
            if (trimmed.Length == 0 || trimmed.StartsWith('#'))
                continue;

            var output = session.Execute(trimmed);
            if (output.Length > 0)
                Console.Write(output.EndsWith('\n') ? output : output + Environment.NewLine);
            if (output.StartsWith("error:", StringComparison.Ordinal))
                hadError = true;
        }

        return hadError ? 1 : 0;
    }

    private static CliOptionSet AddOptions(Command command)
    {
        var options = new CliOptionSet(
            new Option<string?>("--config")
            {
                Description = "Path to the JSON configuration file."
            },
            new Option<string?>("--log-level")
            {
                Description = "Logging level: Trace, Debug, Information, Warning, or Critical."
            },
            new Option<string?>("--profile")
            {
                Description = "PET profile override."
            },
            new Option<string?>("--roms")
            {
                Description = "ROM directory override."
            },
            new Option<int?>("--steps")
            {
                Description = "Instruction budget for an application."
            },
            new Option<bool>("--verbose")
            {
                Description = "Include exception details in error output."
            });
        options.Steps.Validators.Add(result =>
        {
            if (result.GetValueOrDefault<int?>() is < 0)
            {
                result.AddError("--steps must be zero or greater.");
            }
        });
        command.Options.Add(options.Config);
        command.Options.Add(options.LogLevel);
        command.Options.Add(options.Profile);
        command.Options.Add(options.Roms);
        command.Options.Add(options.Steps);
        command.Options.Add(options.Verbose);
        return options;
    }

    private static async Task<int> ExecuteAsync(
        ParseResult parseResult,
        CliOptionSet options,
        CliOptionSet? fallbackOptions,
        Func<ApplicationRegistry, ApplicationRequest, CancellationToken, Task<int>> action,
        CancellationToken cancellationToken)
    {
        var cli = new CliOverrides(
            parseResult.GetValue(options.Config) ?? GetFallback(parseResult, fallbackOptions?.Config),
            parseResult.GetValue(options.LogLevel) ?? GetFallback(parseResult, fallbackOptions?.LogLevel),
            parseResult.GetValue(options.Profile) ?? GetFallback(parseResult, fallbackOptions?.Profile),
            parseResult.GetValue(options.Roms) ?? GetFallback(parseResult, fallbackOptions?.Roms),
            parseResult.GetValue(options.Steps) ?? GetFallback(parseResult, fallbackOptions?.Steps),
            parseResult.GetValue(options.Verbose) || GetFallback(parseResult, fallbackOptions?.Verbose));

        try
        {
            using IHost host = CliHost.Create(cli);
            ApplicationRegistry registry = host.Services.GetRequiredService<ApplicationRegistry>();
            ApplicationRequest request = host.Services.GetRequiredService<ApplicationRequestFactory>().Create(cli);
            return await action(registry, request, cancellationToken);
        }
        catch (CliInputException exception)
        {
            Console.Error.WriteLine($"Error: {exception.Message}");
            return 2;
        }
        catch (OperationCanceledException)
        {
            Console.Error.WriteLine("Operation cancelled.");
            return 130;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine($"Error: {exception.Message}");
            if (cli.Verbose)
            {
                Console.Error.WriteLine(exception);
            }

            return 1;
        }
    }

    private static T? GetFallback<T>(ParseResult parseResult, Option<T>? option) =>
        option is null ? default : parseResult.GetValue(option);

    private sealed record CliOptionSet(
        Option<string?> Config,
        Option<string?> LogLevel,
        Option<string?> Profile,
        Option<string?> Roms,
        Option<int?> Steps,
        Option<bool> Verbose);
}

internal static class CliHost
{
    public static IHost Create(CliOverrides cli)
    {
        var settings = new HostApplicationBuilderSettings
        {
            ApplicationName = "PetEmulator.Cli",
            ContentRootPath = AppContext.BaseDirectory,
            Args = []
        };
        HostApplicationBuilder builder = Host.CreateApplicationBuilder(settings);

        if (!string.IsNullOrWhiteSpace(cli.ConfigPath))
        {
            string configPath = Path.GetFullPath(cli.ConfigPath);
            if (!File.Exists(configPath))
            {
                throw new CliInputException($"Configuration file not found: {configPath}");
            }

            builder.Configuration.AddJsonFile(configPath, optional: false, reloadOnChange: false);
        }

        if (!string.IsNullOrWhiteSpace(cli.LogLevel))
        {
            LogLevel minimum = ParseLogLevel(cli.LogLevel);
            builder.Logging.AddFilter((_, level) => level >= minimum);
        }

        builder.Services.AddSingleton(cli);
        builder.Services.AddSingleton<ApplicationRequestFactory>();
        builder.Services.AddSingleton<ApplicationRegistry>();
        builder.Services.AddSingleton<IPredefinedApplication, StatusApplication>();
        builder.Services.AddSingleton<IPredefinedApplication, ConfigurationApplication>();
        return builder.Build();
    }

    private static LogLevel ParseLogLevel(string value)
    {
        if (Enum.TryParse(value, ignoreCase: true, out LogLevel level))
        {
            return level;
        }

        throw new CliInputException($"Unknown log level '{value}'.");
    }
}

internal sealed record CliOverrides(
    string? ConfigPath,
    string? LogLevel,
    string? Profile,
    string? RomsPath,
    int? Steps,
    bool Verbose);

internal sealed class PetCliSettings
{
    public string Profile { get; set; } = "pet-2001-8";
    public string RomsPath { get; set; } = "roms";
    public string DefaultApplication { get; set; } = "status";
    public int Steps { get; set; } = 10_000;
}

internal sealed class ApplicationRequestFactory(IConfiguration configuration)
{
    public ApplicationRequest Create(CliOverrides cli)
    {
        PetCliSettings settings = configuration.GetSection("PetEmulator").Get<PetCliSettings>() ?? new();
        settings.Profile = cli.Profile ?? settings.Profile;
        settings.RomsPath = cli.RomsPath ?? settings.RomsPath;
        settings.Steps = cli.Steps ?? settings.Steps;
        if (settings.Steps < 0)
        {
            throw new CliInputException("PetEmulator:Steps must be zero or greater.");
        }

        return new ApplicationRequest(settings, configuration, cli);
    }
}

internal sealed record ApplicationRequest(
    PetCliSettings Settings,
    IConfiguration Configuration,
    CliOverrides Overrides);

internal interface IPredefinedApplication
{
    string Name { get; }
    string Description { get; }
    Task<int> RunAsync(ApplicationRequest request, CancellationToken cancellationToken);
}

internal sealed class ApplicationRegistry(
    IEnumerable<IPredefinedApplication> applications,
    ILogger<ApplicationRegistry> logger)
{
    private readonly IReadOnlyDictionary<string, IPredefinedApplication> applications =
        applications.ToDictionary(application => application.Name, StringComparer.OrdinalIgnoreCase);

    public Task<int> ListAsync(ApplicationRequest request, CancellationToken cancellationToken)
    {
        foreach (IPredefinedApplication application in applications.Values.OrderBy(application => application.Name))
        {
            Console.WriteLine($"{application.Name,-12} {application.Description}");
        }

        return Task.FromResult(0);
    }

    public async Task<int> RunAsync(string name, ApplicationRequest request, CancellationToken cancellationToken)
    {
        if (!applications.TryGetValue(name, out IPredefinedApplication? application))
        {
            throw new CliInputException($"Unknown application '{name}'. Run 'apps' to list available applications.");
        }

        logger.LogInformation("Running predefined application {Application} with profile {Profile}",
            application.Name, request.Settings.Profile);
        return await application.RunAsync(request, cancellationToken);
    }
}

internal sealed class StatusApplication(ILogger<StatusApplication> logger) : IPredefinedApplication
{
    public string Name => "status";
    public string Description => "Show the resolved PET configuration.";

    public Task<int> RunAsync(ApplicationRequest request, CancellationToken cancellationToken)
    {
        logger.LogDebug("Reporting resolved CLI configuration.");
        Console.WriteLine($"profile: {request.Settings.Profile}");
        Console.WriteLine($"roms: {Path.GetFullPath(request.Settings.RomsPath)}");
        Console.WriteLine($"steps: {request.Settings.Steps}");
        Console.WriteLine("emulator: not implemented");
        return Task.FromResult(0);
    }
}

internal sealed class ConfigurationApplication : IPredefinedApplication
{
    public string Name => "config";
    public string Description => "Validate the resolved PET configuration.";

    public Task<int> RunAsync(ApplicationRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Settings.Profile))
        {
            throw new CliInputException("PetEmulator:Profile cannot be empty.");
        }

        if (string.IsNullOrWhiteSpace(request.Settings.RomsPath))
        {
            throw new CliInputException("PetEmulator:RomsPath cannot be empty.");
        }

        Console.WriteLine("configuration: valid");
        return Task.FromResult(0);
    }
}

internal sealed class CliInputException(string message) : Exception(message);
