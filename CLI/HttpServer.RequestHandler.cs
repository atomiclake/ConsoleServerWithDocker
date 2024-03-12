using System.Net;
using System.Text;
using Microsoft.Extensions.Logging;

namespace ConsoleServerWithDocker;

public partial class HttpServer
{
    private class RequestHandler(HttpServer server)
    {
        private readonly HttpServer _Server = server;

        private void SendResponse(HttpListenerContext context, int statusCode, string statusDescription, string? infoLogMessage = null, string? errorLogMessage = null)
        {
            if (errorLogMessage is string message)
            {
                _Server._Logger.LogError("{}", message);
            }

            if (infoLogMessage is string infoMessage)
            {
                _Server._Logger.LogInformation("{}", infoMessage);
            }

            context.Response.StatusCode = statusCode;
            context.Response.StatusDescription = statusDescription;
            context.Response.ProtocolVersion = new(1, 1);

            context.Response.Close();
        }

        private async Task ServeFileContent(HttpListenerContext context, string path, string mimeType, Encoding encoding)
        {
            try
            {
                byte[] data = encoding.GetBytes(await File.ReadAllTextAsync(path));

                context.Response.ContentEncoding = encoding;
                context.Response.ContentLength64 = data.LongLength;
                context.Response.ContentType = mimeType;
                context.Response.OutputStream.Write(data);
                
                SendResponse(context, (int)HttpStatusCode.OK, "OK");
            }
            catch (Exception ex)
            {
                _Server._Logger.LogCritical("A fatal error occurred while starting the server. Exception: {}", ex.Message);

                SendResponse(context, (int)HttpStatusCode.InternalServerError, "Internal Server Error", null, $"Exception caugth: {ex.Message}");

                return;
            }

            return;
        }

        public async Task HandleRequest(HttpListenerContext context)
        {
            if (context.Request.HttpMethod.Equals("GET", StringComparison.OrdinalIgnoreCase))
            {
                await HandleGetRequest(context);
            }
        }

        private async Task HandleGetRequest(HttpListenerContext context)
        {
            // Handle a request to the default resource
            if (context.Request.RawUrl! == "/")
            {
                await ServeFileContent(context, Path.Combine(_Server.StaticFilesDirectoryPath, "index.html"), "text/html", Encoding.UTF8);
                return;
            }

            // Substring notation used to ignore the first '/' character of the RawUrl, this character causes problems with the Path.Combine function
            string resourceAbsoluteUri = Path.Combine(_Server.StaticFilesDirectoryPath, context.Request.RawUrl![1..]);

            // File not found
            if (!File.Exists(resourceAbsoluteUri))
            {
                SendResponse(context, (int)HttpStatusCode.NotFound, "Not Found", null, $"Could not find the file '{resourceAbsoluteUri}'");
                return;
            }

            // File was found
            await ServeFileContent(context, resourceAbsoluteUri, "text/html", Encoding.UTF8);
        }
    }
}