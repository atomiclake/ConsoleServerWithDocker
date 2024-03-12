using System.Net;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ConsoleServerWithDocker;

public partial class HttpServer : IHostedService
{
    private static readonly HttpServerConfiguration DefaultServerConfiguration = new()
    {
        HttpListenerPort = 8080,
        ServerStaticFilesDirectory = "wwwroot",
        GenServerFiles = true
    };

    private readonly HttpServerConfiguration _Configuration;

    private readonly ILogger _Logger;

    private readonly IHostEnvironment _HostEnvironment;

    private readonly RequestHandler _RequestHandler;

    private string StaticFilesDirectoryPath => Path.Combine(_HostEnvironment.ContentRootPath, _Configuration.ServerStaticFilesDirectory);

    private string HttpListenerAddress => $"http://localhost:{_Configuration.HttpListenerPort}/";

    private HttpListener? Listener { get; set; }

    private void GenerateServerFiles()
    {
        string indexPageContent = """
        <!DOCTYPE html>

        <html lang="en">
            <head>
                <meta charset="UTF-8">
                <meta name="viewport" content="width=device-width, initial-scale=1.0">
                <title>Document</title>
            </head>

            <body>
                <nav>
                    <a href="/index.html">Home</a> |
                    <a href="/contacts.html">Contacts</a>
                </nav>

                <h1>Hello, world!</h1>
            </body>
        </html>
        """;

        string contactsPageContent = """
        <!DOCTYPE html>

        <html lang="en">
            <head>
                <meta charset="UTF-8">
                <meta name="viewport" content="width=device-width, initial-scale=1.0">
                <title>Document</title>
            </head>

            <body>
                <nav>
                    <a href="/index.html">Home</a> |
                    <a href="/contacts.html">Contacts</a>
                </nav>

                <h1>Contact list:</h1>

                <ul>
                    <li>Contact 1</li>
                    <li>Contact 2</li>
                    <li>Contact 3</li>
                    <li>Contact 4</li>
                    <li>Contact 5</li>
                </ul>
            </body>
        </html>
        """;

        try
        {
            File.WriteAllText(Path.Combine(StaticFilesDirectoryPath, "index.html"), indexPageContent);
            File.WriteAllText(Path.Combine(StaticFilesDirectoryPath, "contacts.html"), contactsPageContent);
        }
        catch (Exception ex)
        {
            _Logger.LogCritical("A fatal error occurred while generating the server files. Exception: {}", ex.Message);
        }
    }

    private bool InitializeServer()
    {
        if (!Directory.Exists(StaticFilesDirectoryPath))
        {
            if (_Configuration.GenServerFiles)
            {
                Directory.CreateDirectory(StaticFilesDirectoryPath);
                GenerateServerFiles();
            }
            else
            {
                _Logger.LogCritical("Could not find server root folder at: {}, stopping...", StaticFilesDirectoryPath);
                return false;
            }
        }

        try
        {
            Listener = new();

            Listener.Prefixes.Add(HttpListenerAddress);

            Listener.Start();
        }
        catch (Exception ex)
        {
            _Logger.LogCritical("A fatal error occurred while starting the server. Exception: {}", ex.Message);
            return false;
        }

        _Logger.LogInformation("Starting listener at {}", HttpListenerAddress);

        return true;
    }

    private async Task HandleConnections(CancellationToken cancellationToken)
    {
        if (Listener is null)
        {
            return;
        }

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                HttpListenerContext context = await Listener.GetContextAsync().WaitAsync(cancellationToken);

                await _RequestHandler.HandleRequest(context);
            }
            catch (TaskCanceledException)
            {
                return;
            }
        }
    }

    public HttpServer(
        IConfiguration configuration,
        ILogger<HttpServer> logger,
        IHostEnvironment hostEnvironment)
    {
        _Configuration = configuration.Get<HttpServerConfiguration>()!;
        _Logger = logger;
        _HostEnvironment = hostEnvironment;
        _RequestHandler = new(this);
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (!InitializeServer())
        {
            await StopAsync(cancellationToken);
        }

        await HandleConnections(cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _Logger.LogInformation("Stopping server...");

        Listener?.Stop();

        return Task.CompletedTask;
    }
}