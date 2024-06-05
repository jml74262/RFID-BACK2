using System;
using System.IO;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

public class PrinterSocketService
{
    private readonly IConfiguration _configuration;
    private WebSocket _webSocket;

    public PrinterSocketService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task HandleWebSocketAsync(WebSocket webSocket)
    {
        _webSocket = webSocket;
        var buffer = new byte[1024 * 4];
        WebSocketReceiveResult result = await _webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

        while (!result.CloseStatus.HasValue)
        {
            var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
            // Handle received message (e.g., send to printer)
            bool printResult = await SendFileToPrinterAsync(message);

            // Send acknowledgment back to the client
            var ackMessage = printResult ? "Print job successful." : "Print job failed.";
            var ackData = Encoding.UTF8.GetBytes(ackMessage);
            await _webSocket.SendAsync(new ArraySegment<byte>(ackData), WebSocketMessageType.Text, true, CancellationToken.None);

            result = await _webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
        }

        await _webSocket.CloseAsync(result.CloseStatus.Value, result.CloseStatusDescription, CancellationToken.None);
    }

    private async Task<bool> SendFileToPrinterAsync(string filePath)
    {
        if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
        {
            Console.WriteLine($"Invalid file path: {filePath}");
            return false;
        }

        string ipAddress = _configuration["PrinterSettings:IPAddress"];
        int port = int.Parse(_configuration["PrinterSettings:Port"]);

        try
        {
            // Read the content of the file
            string printCommand = await File.ReadAllTextAsync(filePath);

            using (TcpClient client = new TcpClient(ipAddress, port))
            using (NetworkStream stream = client.GetStream())
            {
                byte[] data = Encoding.ASCII.GetBytes(printCommand);
                await stream.WriteAsync(data, 0, data.Length);
                await stream.FlushAsync();
                Console.WriteLine($"Sent to printer: {printCommand.Replace("\x1B", "\\x1B").Replace("\x0A", "\\x0A")}");
            }
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Exception: {ex.Message}");
            return false;
        }
    }
}
