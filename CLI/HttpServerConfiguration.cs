namespace ConsoleServerWithDocker;

public class HttpServerConfiguration
{
    public required int HttpListenerPort { get; set; }

    public required string ServerStaticFilesDirectory { get; set; }

    public required bool GenServerFiles { get; set; }
}
