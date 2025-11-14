using System;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Threading;
using System.Threading.Tasks;
using CommandLine;
using Microsoft.Extensions.Logging;
using Spectre.Console;
using Sandbox;
using Sandbox.Scenes;

static class Program
{
    private static ILoggerFactory? _loggerFactory;
    public enum GraphicsApi
    {
        OpenGL,
        Vulkan,
        Metal
    }

    public enum SceneType
    {
        MMark,
        Paragraph,
        CirclingSquares
    }

    public class Options
    {
        [Option('a', "api", Required = false, Default = GraphicsApi.OpenGL, HelpText = "Graphics API to use (OpenGL, Vulkan, Metal)")]
        public GraphicsApi Api { get; set; }

        [Option('s', "scene", Required = false, Default = SceneType.MMark, HelpText = "Scene to render (MMark, Paragraph, CirclingSquares)")]
        public SceneType Scene { get; set; }

        [Option('w', "width", Required = false, Default = 800, HelpText = "Window width")]
        public int Width { get; set; }

        [Option('h', "height", Required = false, Default = 600, HelpText = "Window height")]
        public int Height { get; set; }
    }

    static void Main(string[] args)
    {
        // Configure logging
        _loggerFactory = LoggerFactory.Create(builder =>
        {
            builder
                .AddDebug()
                .SetMinimumLevel(LogLevel.Debug);
        });

        var parser = new Parser(with => with.CaseInsensitiveEnumValues = true);
        parser.ParseArguments<Options>(args)
            .WithParsed(RunApplication)
            .WithNotParsed(errors => Environment.Exit(1));
    }

    static void RunApplication(Options options)
    {
        var logger = _loggerFactory!.CreateLogger("Sandbox");

        // Create the scene based on the selected type
        IScene scene = options.Scene switch
        {
            SceneType.MMark => new MMarkScene(),
            SceneType.Paragraph => new ParagraphScene(),
            SceneType.CirclingSquares => new CirclingSquares(),
            _ => new MMarkScene()
        };

        // Check if Metal is requested and we're on macOS
        if (options.Api == GraphicsApi.Metal)
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                logger.LogError("Metal is only supported on macOS");
                return;
            }

            var metalLogger = _loggerFactory!.CreateLogger<MetalApplication>();
            var metalApp = new MetalApplication(options.Width, options.Height, "NImpeller on Metal", metalLogger);
            metalApp.SetScene(scene);
            RunWithConsoleDisplay(metalApp);
            return;
        }

        // For OpenGL and Vulkan, use SDL
        var sdlApi = options.Api == GraphicsApi.OpenGL ? SdlApplication.GraphicsApi.OpenGL : SdlApplication.GraphicsApi.Vulkan;
        var sdlLogger = _loggerFactory!.CreateLogger<SdlApplication>();
        var sdlApp = new SdlApplication(sdlApi, sdlLogger);
        if (sdlApp.Initialize(options.Width, options.Height))
        {
            sdlApp.SetScene(scene);
            RunWithConsoleDisplay(sdlApp);
        }
    }

    static void RunWithConsoleDisplay(IApplication app)
    {
        var currentStatus = app.GetStatus();
        var isRunning = true;

        // Subscribe to status updates
        app.StatusUpdated += (sender, e) =>
        {
            currentStatus = e.Status;
        };

        // Start console display in background thread
        var consoleTask = Task.Run(() =>
        {
            AnsiConsole.Live(CreateStatusTable(currentStatus))
                .Start(ctx =>
                {
                    while (isRunning)
                    {
                        ctx.UpdateTarget(CreateStatusTable(currentStatus));
                        Thread.Sleep(100); // Update display every 100ms
                    }
                });
        });

        app.Run();

        isRunning = false;

        consoleTask.Wait(TimeSpan.FromSeconds(1));
    }

    static Table CreateStatusTable(ApplicationStatus status)
    {
        var table = new Table()
            .Border(TableBorder.Rounded)
            .BorderColor(Color.Blue)
            .AddColumn(new TableColumn("[bold]Metric[/]").Centered())
            .AddColumn(new TableColumn("[bold]Value[/]").Centered());

        table.AddRow("[cyan]FPS[/]", $"[green]{status.CurrentFps}[/]");
        table.AddRow("[cyan]Total Frames[/]", $"[yellow]{status.TotalFrames:N0}[/]");
        table.AddRow("[cyan]Runtime[/]", $"[magenta]{status.RunTime:hh\\:mm\\:ss}[/]");

        return table;
    }
}

